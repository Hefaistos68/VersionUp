namespace VersionUp;

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;
using static Microsoft.VisualStudio.VSConstants;

/// <summary>
/// Command handler for the VersionUp increment command.
/// </summary>
public sealed class VersionUpCommand
{
    /// <summary>
    /// Command set GUID string (should match vsct file).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("c3f962f7-a630-4c98-9b21-7b1a9908f87c");

    /// <summary>
    /// Command ID for Increment Major.
    /// </summary>
    public const int cmdidIncrementMajor = 0x0100;

    /// <summary>
    /// Command ID for Increment Minor.
    /// </summary>
    public const int cmdidIncrementMinor = 0x0200;

    /// <summary>
    /// Command ID for Increment Build.
    /// </summary>
    public const int cmdidIncrementBuild = 0x0300;

    /// <summary>
    /// Command ID for Increment Patch.
    /// </summary>
    public const int cmdidIncrementPatch = 0x0400;

    /// <summary>The owner package that this command was registered with.</summary>
    private readonly AsyncPackage _package;
 
    /// <summary>
    /// The ordered set of file-type handlers consulted when resolving which handler
    /// can process a given version file. The first handler that reports
    /// <see cref="IVersionFileHandler.CanHandle"/> <see langword="true"/> is used.
    /// </summary>
    private static readonly IVersionFileHandler[] Handlers = new IVersionFileHandler[]
    {
        new CsprojVersionHandler(),
        new AssemblyInfoVersionHandler(),
        new NuspecVersionHandler(),
        new PackageJsonVersionHandler(),
        new VsixManifestVersionHandler(),
        new AppxManifestVersionHandler(),
        new WxsVersionHandler(),
        new RcVersionHandler()
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionUpCommand"/> class.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    private VersionUpCommand(AsyncPackage package)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static VersionUpCommand? Instance { get; private set; }

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        IMenuCommandService? commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;

        if (commandService != null)
        {
            AddMenuCommand(commandService, cmdidIncrementMajor, VersionSegment.Major);
            AddMenuCommand(commandService, cmdidIncrementMinor, VersionSegment.Minor);
            AddMenuCommand(commandService, cmdidIncrementBuild, VersionSegment.Build);
            AddMenuCommand(commandService, cmdidIncrementPatch, VersionSegment.Revision);
        }

        Instance = new VersionUpCommand(package);
    }

    /// <summary>
    /// Registers an <see cref="OleMenuCommand"/> for the given <paramref name="commandId"/> and
    /// wires it up to execute <see cref="Execute"/> for the specified <paramref name="segment"/>.
    /// </summary>
    /// <param name="commandService">The menu command service to register the command with.</param>
    /// <param name="commandId">The numeric command ID defined in the .vsct file.</param>
    /// <param name="segment">The version segment the command will increment.</param>
    private static void AddMenuCommand(IMenuCommandService commandService, int commandId, VersionSegment segment)
    {
        CommandID menuCommandID = new CommandID(CommandSet, commandId);
        OleMenuCommand menuItem = new OleMenuCommand((s, e) => 
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Execute(segment);
        }, menuCommandID);

        menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(menuItem);
    }

    /// <summary>
    /// Handles the <see cref="OleMenuCommand.BeforeQueryStatus"/> event to show or hide the
    /// menu item based on whether the currently resolved path is a supported version file.
    /// </summary>
    private static void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (sender is OleMenuCommand menuItem)
        {
            string? selectedPath = GetSelectedPath();

            if (string.IsNullOrEmpty(selectedPath))
            {
                menuItem.Visible = false;
                menuItem.Enabled = false;

                return;
            }

            IVersionFileHandler? handler = GetHandlerForFile(selectedPath!);
            bool canHandle = handler != null;

            menuItem.Visible = canHandle;
            menuItem.Enabled = canHandle;
        }
    }

    /// <summary>
    /// Callback used to execute the command when a menu item is clicked.
    /// </summary>
    /// <param name="segment">The version segment to increment.</param>
    private static void Execute(VersionSegment segment)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (Instance == null)
        {
            return;
        }

        string? selectedPath = GetSelectedPath();

        if (string.IsNullOrEmpty(selectedPath))
        {
            VsShellUtilities.ShowMessageBox(
                (Instance._package as IServiceProvider) ?? ServiceProvider.GlobalProvider,
                "No active project or file selected in Solution Explorer.",
                "VersionUp",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return;
        }

        IVersionFileHandler? handler = GetHandlerForFile(selectedPath!);

        if (handler == null)
        {
            VsShellUtilities.ShowMessageBox(
                (Instance._package as IServiceProvider) ?? ServiceProvider.GlobalProvider,
                "Unsupported file type for version increment.",
                "VersionUp",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return;
        }

        try
        {
            IVsTextLines? buffer = GetTextBufferForFile(selectedPath!);

            if (buffer == null)
            {
                VsShellUtilities.ShowMessageBox(
                    (Instance._package as IServiceProvider) ?? ServiceProvider.GlobalProvider,
                    $"Failed to open '{Path.GetFileName(selectedPath)}' in Visual Studio.",
                    "VersionUp",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return;
            }

            IVersionLogger logger = (Instance._package as IVersionLogger) ?? new FallbackLogger();
            VersionIncrementer incrementer = new VersionIncrementer(logger);

            string fileContent = GetTextFromBuffer(buffer);
            string? currentVersion = handler.GetVersion(fileContent);

            if (string.IsNullOrEmpty(currentVersion))
            {
                currentVersion = "1.0.0";
            }

            string newVersion = incrementer.Increment(currentVersion!, segment);
            string updatedContent = handler.UpdateVersion(fileContent, newVersion);
            // LinkedTransactionFlags2.mdtGlobal (value 2) marks the transaction as
            // "closed-file-capable": VS keeps the undo entry in its global undo stack
            // even when the edited document is not the currently active editor.
            IVsLinkedUndoTransactionManager? undoManager =
                ServiceProvider.GlobalProvider.GetService(typeof(SVsLinkedUndoTransactionManager)) as IVsLinkedUndoTransactionManager;
            bool undoOpened = false;

            if (undoManager != null)
            {
                int hr = undoManager.OpenLinkedUndo(
                    (uint)LinkedTransactionFlags2.mdtGlobal,
                    "Increment Project Version");

                undoOpened = hr == VSConstants.S_OK;
            }

            try
            {
                ReplaceTextInBuffer(buffer, updatedContent);
            }
            finally
            {
                if (undoOpened && undoManager != null)
                {
                    undoManager.CloseLinkedUndo();
                }
            }

            string successMessage = $"Successfully incremented {segment} version in {Path.GetFileName(selectedPath)}! New version: {newVersion}";
            OutputToWindow(successMessage);
            SetStatusBarText(successMessage);
        }
        catch (Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                (Instance._package as IServiceProvider) ?? ServiceProvider.GlobalProvider,
                $"Failed to increment version: {ex.Message}",
                "VersionUp",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    /// <summary>
    /// Returns the first registered <see cref="IVersionFileHandler"/> that can handle
    /// <paramref name="filePath"/>, or <see langword="null"/> if none matches.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to look up.</param>
    internal static IVersionFileHandler? GetHandlerForFile(string filePath)
    {
        foreach (IVersionFileHandler handler in Handlers)
        {
            if (handler.CanHandle(filePath))
            {
                return handler;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the file path that the version increment command should operate on.
    /// </summary>
    /// <returns>
    /// The resolved absolute file path, evaluated in priority order:
    /// <list type="number">
    ///   <item>The active editor document, if it is a supported version file type.</item>
    ///   <item>The first selected item in Solution Explorer (project or project item).</item>
    ///   <item>The active project's file path (e.g. <c>.csproj</c>) as a last resort.</item>
    /// </list>
    /// Returns <see langword="null"/> when no suitable path can be determined.
    /// </returns>
    private static string? GetSelectedPath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        EnvDTE80.DTE2? dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

        if (dte == null)
        {
            return null;
        }

        // 1. Active editor document — preferred when invoked via keyboard shortcut.
        if (dte.ActiveDocument != null && !string.IsNullOrEmpty(dte.ActiveDocument.FullName))
        {
            string activeDocPath = dte.ActiveDocument.FullName;

            if (GetHandlerForFile(activeDocPath) != null)
            {
                return activeDocPath;
            }
        }

        // 2. Solution Explorer selection.
        if (dte.SelectedItems != null && dte.SelectedItems.Count > 0)
        {

            EnvDTE.SelectedItem? selectedItem = dte.SelectedItems.Item(1);

            if (selectedItem != null)
            {
                if (selectedItem.Project != null && !string.IsNullOrEmpty(selectedItem.Project.FullName))
                {
                    return selectedItem.Project.FullName;
                }

                if (selectedItem.ProjectItem != null)
                {
                    try
                    {
                        string path = selectedItem.ProjectItem.FileNames[1];

                        return path;
                    }
                    catch
                    {
                        // FileNames throws or is empty for some items
                    }
                }
            }
        }

        // 3. Fall back to the active project's file (e.g. .csproj).
        if (dte.ActiveSolutionProjects is Array activeProjects && activeProjects.Length > 0)
        {
            if (activeProjects.GetValue(0) is EnvDTE.Project activeProject &&
                !string.IsNullOrEmpty(activeProject.FullName))
            {
                return activeProject.FullName;
            }
        }

        return null;
    }

    /// <summary>
    /// Opens <paramref name="filePath"/> in a Visual Studio editor window and returns the
    /// underlying <see cref="IVsTextLines"/> buffer so that edits can be made through the
    /// VS text model (which participates in the undo stack).
    /// </summary>
    /// <param name="filePath">Absolute path to the file to open.</param>
    /// <returns>
    /// The text buffer for the file, or <see langword="null"/> if the file could not be opened.
    /// </returns>
    private static IVsTextLines? GetTextBufferForFile(string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        IVsUIShellOpenDocument? openDoc = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

        if (openDoc == null)
        {
            return null;
        }

        Guid logicalView = VSConstants.LOGVIEWID_TextView;
        int hr = openDoc.OpenDocumentViaProject(
            filePath,
            ref logicalView,
            out Microsoft.VisualStudio.OLE.Interop.IServiceProvider _,
            out IVsUIHierarchy _,
            out uint _,
            out IVsWindowFrame frame);

        if (hr == VSConstants.S_OK && frame != null)
        {
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object docData);

            if (docData is IVsTextLines buffer)
            {
                return buffer;
            }

            if (docData is IVsTextBufferProvider bufferProvider)
            {
                bufferProvider.GetTextBuffer(out IVsTextLines textLines);

                return textLines;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the full text content from <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">The VS text buffer to read from.</param>
    /// <returns>The entire buffer content as a single string.</returns>
    private static string GetTextFromBuffer(IVsTextLines buffer)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        buffer.GetSize(out int size);
        buffer.GetLastLineIndex(out int lastLine, out int lastIndex);
        buffer.GetLineText(0, 0, lastLine, lastIndex, out string text);

        return text;
    }

    /// <summary>
    /// Replaces the entire content of <paramref name="buffer"/> with <paramref name="newText"/>
    /// using a single atomic <c>ReplaceLines</c> call, which registers the change on the
    /// document's undo stack.
    /// </summary>
    /// <param name="buffer">The VS text buffer to update.</param>
    /// <param name="newText">The new full-file text to write into the buffer.</param>
    private static void ReplaceTextInBuffer(IVsTextLines buffer, string newText)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        buffer.GetLastLineIndex(out int lastLine, out int lastIndex);

        IntPtr pText = Marshal.StringToCoTaskMemUni(newText);

        try
        {
            buffer.ReplaceLines(0, 0, lastLine, lastIndex, pText, newText.Length, null);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pText);
        }
    }

    /// <summary>
    /// Outputs a message to the default Visual Studio output window.
    /// </summary>
    /// <param name="message">The message to output.</param>
    private static void OutputToWindow(string message)
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsOutputWindow? outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            if (outputWindow == null)
            {
                return;
            }

            Guid generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;
            outputWindow.GetPane(ref generalPaneGuid, out IVsOutputWindowPane? pane);

            if (pane != null)
            {
                pane.OutputString($"[VersionUp] {message}\n");
                pane.Activate();
            }
        }
        catch
        {
            // Silently fail if output window is not available
        }
    }

    /// <summary>
    /// Sets the status bar text to display a message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private static void SetStatusBarText(string message)
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsStatusbar? statusBar = ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            if (statusBar != null)
            {
                statusBar.SetText(message);
            }
        }
        catch
        {
            // Silently fail if status bar is not available
        }
    }

    /// <summary>
    /// Aligns all versioned files inside the project to a specified target version.
    /// Runs atomically inside a linked undo transaction.
    /// </summary>
    /// <param name="project">The Visual Studio project.</param>
    /// <param name="targetVersion">The version to set across all versioned files.</param>
    internal static void AlignProjectVersions(Project project, string targetVersion)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (project == null || string.IsNullOrEmpty(project.FullName))
        {
            return;
        }

        IVsLinkedUndoTransactionManager? undoManager =
            ServiceProvider.GlobalProvider.GetService(typeof(SVsLinkedUndoTransactionManager)) as IVsLinkedUndoTransactionManager;

        bool undoOpened = false;

        if (undoManager != null)
        {
            int hr = undoManager.OpenLinkedUndo(
                (uint)LinkedTransactionFlags2.mdtGlobal,
                $"Align Project Versions to {targetVersion}");

            undoOpened = hr == VSConstants.S_OK;
        }

        try
        {
            string projectPath = project.FullName;

            if (File.Exists(projectPath))
            {
                AlignFileVersion(projectPath, targetVersion);
            }

            AlignProjectItemsVersions(project.ProjectItems, targetVersion);
        }
        finally
        {
            if (undoOpened && undoManager != null)
            {
                undoManager.CloseLinkedUndo();
            }
        }
    }

    /// <summary>
    /// Recursively aligns version files in the project items.
    /// </summary>
    /// <param name="items">The project items collection.</param>
    /// <param name="targetVersion">The target version.</param>
    private static void AlignProjectItemsVersions(ProjectItems items, string targetVersion)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (items == null)
        {
            return;
        }

        foreach (ProjectItem item in items)
        {
            if (item.ProjectItems != null && item.ProjectItems.Count > 0)
            {
                AlignProjectItemsVersions(item.ProjectItems, targetVersion);
            }

            try
            {
                for (short i = 1; i <= item.FileCount; i++)
                {
                    string filePath = item.FileNames[i];

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        AlignFileVersion(filePath, targetVersion);
                    }
                }
            }
            catch
            {
                // Ignore virtual nodes or failures
            }
        }
    }

    /// <summary>
    /// Aligns a single file's version if it is supported and currently has a different version.
    /// </summary>
    /// <param name="filePath">The absolute path to the versioned file.</param>
    /// <param name="targetVersion">The target version.</param>
    private static void AlignFileVersion(string filePath, string targetVersion)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        IVersionFileHandler? handler = GetHandlerForFile(filePath);

        if (handler == null)
        {
            return;
        }

        try
        {
            IVsTextLines? buffer = GetTextBufferForFile(filePath);

            if (buffer == null)
            {
                return;
            }

            string fileContent = GetTextFromBuffer(buffer);

            string? currentVersion = handler.GetVersion(fileContent);

            if (currentVersion != targetVersion)
            {
                string updatedContent = handler.UpdateVersion(fileContent, targetVersion);

                ReplaceTextInBuffer(buffer, updatedContent);
            }
        }
        catch (Exception ex)
        {
            OutputToWindow($"Failed to update version in {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Fallback <see cref="IVersionLogger"/> implementation used when the package itself
    /// does not implement <see cref="IVersionLogger"/>. Writes messages to the debug output.
    /// </summary>
    private class FallbackLogger : IVersionLogger
    {
        /// <summary>Writes <paramref name="message"/> to the debug output channel.</summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[VersionUp] {message}");
        }
    }
}
