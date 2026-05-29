using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using VersionUp.VersionHandlers;

namespace VersionUp.UiElements
{
	/// <summary>
	/// Displays the version of the active project in the Visual Studio status bar.
	/// Clicking it displays a fly-out list of all project versions.
	/// </summary>
	public sealed class VersionStatusBarControl : Border, IDisposable
	{
		/// <summary>Max number of projects to display in the fly-out list.</summary>
		private const int MaxProjectsInList = 20;
		
		private readonly CrispImage _icon;
	    private readonly CrispImage _warningIcon;
	    private readonly TextBlock _versionText;
	    private readonly EnvDTE80.DTE2 _dte;

	    private readonly SelectionEvents _selectionEvents;
	    private readonly DocumentEvents _documentEvents;
	    private readonly WindowEvents _windowEvents;
	    private readonly SolutionEvents _solutionEvents;

	    private Popup? _popup;
	    private bool _isDisposed;

	    /// <summary>The collection of active version buttons in the popup.</summary>
	    private readonly List<Button> _versionButtons = new();

	    /// <summary>
	    /// Initializes a new instance of the <see cref="VersionStatusBarControl"/> class.
	    /// </summary>
	    public VersionStatusBarControl()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        this.Margin = new Thickness(6, 0, 6, 0);
	        this.Padding = new Thickness(6, 3, 6, 3);
	        this.CornerRadius = new CornerRadius(3);
	        this.VerticalAlignment = VerticalAlignment.Center;
	        this.HorizontalAlignment = HorizontalAlignment.Right;
	        this.Background = Brushes.Transparent;
	        this.Cursor = Cursors.Hand;
	        this.ToolTip = "Active Project Version (Click to see all project versions)";

	        _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as EnvDTE80.DTE2
	            ?? throw new InvalidOperationException("Failed to acquire DTE2 service.");

	        _selectionEvents = _dte.Events.SelectionEvents;
	        _selectionEvents.OnChange += OnSelectionChanged;

	        _documentEvents = _dte.Events.DocumentEvents;
	        _documentEvents.DocumentSaved += OnDocumentSaved;

	        _windowEvents = _dte.Events.WindowEvents;
	        _windowEvents.WindowActivated += OnWindowActivated;

	        _solutionEvents = _dte.Events.SolutionEvents;
	        _solutionEvents.Opened += OnSolutionOpened;
	        _solutionEvents.AfterClosing += OnSolutionClosed;

	        StackPanel panel = new()
	        {
	            Orientation = Orientation.Horizontal,
	            VerticalAlignment = VerticalAlignment.Center
	        };

	        _icon = new CrispImage
	        {
	            Width = 14,
	            Height = 14,
	            Margin = new Thickness(0, 0, 4, 0),
	            VerticalAlignment = VerticalAlignment.Center,
	            Moniker = KnownMonikers.CSProjectNode
	        };

	        _versionText = new TextBlock
	        {
	            VerticalAlignment = VerticalAlignment.Center,
	            FontSize = 11.5
	        };

	        _versionText.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.ToolWindowTextKey);

	        _warningIcon = new CrispImage
	        {
	            Width             = 14,
	            Height            = 14,
	            Margin            = new Thickness(4, 0, 0, 0),
	            VerticalAlignment = VerticalAlignment.Center,
	            Moniker           = KnownMonikers.StatusWarning,
	            Visibility        = Visibility.Collapsed
	        };

	        panel.Children.Add(_icon);
	        panel.Children.Add(_versionText);
	        panel.Children.Add(_warningIcon);
			this.Child = panel;

	        this.MouseEnter += OnMouseEnter;
	        this.MouseLeave += OnMouseLeave;
	        this.MouseLeftButtonUp += OnMouseLeftButtonUp;

	        UpdateState();
	    }

	    /// <summary>
	    /// Updates the text and icon of the status bar button according to the active project.
	    /// </summary>
	    public void UpdateState()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        if (_isDisposed)
	        {
	            return;
	        }

	        try
	        {
	            Project? activeProject = GetActiveProject();

	            if (activeProject == null)
	            {
	                _versionText.Text = "No version";
	                _icon.Moniker = KnownMonikers.CSProjectNode;
	                _icon.Opacity = 0.5;
	                this.ToolTip = "Active Project Version (Click to see all project versions)";
	                _warningIcon.Visibility = Visibility.Collapsed;

	                return;
	            }

	            ProjectVersionDiagnostics diagnostics = ProjectVersionHelper.GetProjectVersionDiagnostics(activeProject);

	            _icon.Moniker = GetMonikerForProject(activeProject);
	            _icon.Opacity = 1.0;

	            if (string.IsNullOrEmpty(diagnostics.PrimaryVersion))
	            {
	                _versionText.Text = "No version";
	                _warningIcon.Visibility = Visibility.Collapsed;
	                this.ToolTip = "Active Project Version (Click to see all project versions)";
	            }
	            else
	            {
	                _versionText.Text = diagnostics.PrimaryVersion;
	                _warningIcon.Visibility = diagnostics.IsOutOfSync ? Visibility.Visible : Visibility.Collapsed;

	                if (diagnostics.IsOutOfSync)
	                {
	                    this.ToolTip = $"Active Project Version: {diagnostics.PrimaryVersion} (Out of Sync!) (Click to see details)";
	                }
	                else
	                {
	                    this.ToolTip = $"Active Project Version: {diagnostics.PrimaryVersion} (Click to see all project versions)";
	                }
	            }
	        }
	        catch (Exception ex)
	        {
	            _versionText.Text = "No version";
	            _icon.Moniker = KnownMonikers.CSProjectNode;
	            _icon.Opacity = 0.5;
	            _warningIcon.Visibility = Visibility.Collapsed;
	            this.ToolTip = "Active Project Version (Click to see all project versions)";
	            System.Diagnostics.Debug.WriteLine($"[VersionUp] Error updating status bar button state: {ex.Message}");
	        }
	    }

	    /// <inheritdoc />
	    public void Dispose()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        if (_isDisposed)
	        {
	            return;
	        }

	        _isDisposed = true;

	        _selectionEvents?.OnChange -= OnSelectionChanged;

	        _documentEvents?.DocumentSaved -= OnDocumentSaved;

	        _windowEvents?.WindowActivated -= OnWindowActivated;

	        if (_solutionEvents != null)
	        {
	            _solutionEvents.Opened -= OnSolutionOpened;
	            _solutionEvents.AfterClosing -= OnSolutionClosed;
	        }

	        this.MouseEnter -= OnMouseEnter;
	        this.MouseLeave -= OnMouseLeave;
	        this.MouseLeftButtonUp -= OnMouseLeftButtonUp;
	    }

	    /// <summary>
	    /// Handles the MouseEnter event of the control, applying a subtle hover background color.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnMouseEnter(object sender, MouseEventArgs e)
	    {
	        this.Background = new SolidColorBrush(Color.FromArgb(24, 128, 128, 128));
	    }

	    /// <summary>
	    /// Handles the MouseLeave event of the control, restoring the background to transparent.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnMouseLeave(object sender, MouseEventArgs e)
	    {
	        this.Background = Brushes.Transparent;
	    }

	    /// <summary>
	    /// Handles the MouseLeftButtonUp event of the control, initializing and opening the popup displaying project versions.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        if (_popup == null)
	        {
	            InitializePopup();
	        }

	        RebuildPopupContent();

	        _popup?.IsOpen = true;
	    }

	    /// <summary>
	    /// Initializes the popup control and its layout settings.
	    /// </summary>
	    private void InitializePopup()
	    {
	        _popup = new Popup
	        {
	            Placement = PlacementMode.Top,
	            PlacementTarget = this,
	            StaysOpen = false,
	            AllowsTransparency = true,
	            PopupAnimation = PopupAnimation.Fade
	        };

	        _popup.Opened += OnPopupOpened;
	        _popup.Closed += OnPopupClosed;
	    }

    /// <summary>
    /// Rebuilds the content of the popup, listing all solution projects with their current versions, sync statuses, and options to align or add versions.
    /// </summary>
    private void RebuildPopupContent()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_popup == null)
        {
            return;
        }

        Border border = new()
        {
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(4),
            Padding         = new Thickness(12)
        };

        border.SetResourceReference(Border.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
        border.SetResourceReference(Border.BorderBrushProperty, VsBrushes.ToolWindowBorderKey);

        StackPanel mainStack = new() { Orientation = Orientation.Vertical };

        TextBlock header = new()
        {
            Text       = "Solution Project Versions",
            FontWeight = FontWeights.Bold,
            FontSize   = 12.5,
            Margin     = new Thickness(0, 0, 0, 6)
        };

        header.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.ToolWindowTextKey);
        mainStack.Children.Add(header);

        Border separator = new()
        {
            Height              = 1,
            Margin              = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        separator.SetResourceReference(Border.BackgroundProperty, VsBrushes.ToolWindowBorderKey);
        mainStack.Children.Add(separator);

        List<Project> projects = ProjectVersionHelper.GetAllProjects();

        if (projects.Count == 0)
        {
            TextBlock noProjectsText = new()
            {
                Text      = "No projects found in solution.",
                FontStyle = FontStyles.Italic,
                Margin    = new Thickness(0, 4, 0, 4)
            };

            noProjectsText.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.ToolWindowTextKey);
            mainStack.Children.Add(noProjectsText);
        }
        else
        {
            _versionButtons.Clear();

            Grid grid = new() { Margin = new Thickness(0, 2, 0, 2) };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            int rowIndex = 0;
            int totalProjects = projects.Count;
            bool isExcessive = totalProjects > MaxProjectsInList;

            foreach (Project proj in projects)
            {
                ProjectVersionDiagnostics diagnostics = ProjectVersionHelper.GetProjectVersionDiagnostics(proj);

                AddProjectRow(grid, ref rowIndex, proj, diagnostics);

                if (diagnostics.IsOutOfSync)
                {
                    AddOutOfSyncDetailRows(grid, ref rowIndex, proj, diagnostics);
                }
            }

            ScrollViewer scroll = new()
            {
                MaxHeight                     = isExcessive ? MaxProjectsInList * 22 : 350,
                MaxWidth                      = 500,
                VerticalScrollBarVisibility   = isExcessive ? ScrollBarVisibility.Visible : ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content                       = grid
            };

            if (isExcessive)
            {
                scroll.MinWidth = 320 + SystemParameters.VerticalScrollBarWidth;
            }

            mainStack.Children.Add(scroll);
        }

        border.Child = mainStack;
        _popup.Child = border;
    }

    /// <summary>
    /// Adds a row representing a single project's main version information to the grid.
    /// </summary>
    /// <param name="grid">The grid container.</param>
    /// <param name="rowIndex">The current row index in the grid.</param>
    /// <param name="proj">The project being added.</param>
    /// <param name="diagnostics">The version diagnostics of the project.</param>
    private void AddProjectRow(Grid grid, ref int rowIndex, Project proj, ProjectVersionDiagnostics diagnostics)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        CrispImage icon = new()
        {
            Width             = 16,
            Height            = 16,
            Margin            = new Thickness(0, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Moniker           = GetMonikerForProject(proj)
        };

        TextBlock nameBlock = new()
        {
            Text              = proj.Name,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 2, 24, 2)
        };

        nameBlock.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.ToolWindowTextKey);

        TextBlock versionBlock = new()
        {
            Text                = string.IsNullOrEmpty(diagnostics.PrimaryVersion) ? "No version" : diagnostics.PrimaryVersion!,
            FontWeight          = FontWeights.SemiBold,
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        versionBlock.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.ToolWindowTextKey);

        Grid.SetRow(icon, rowIndex);
        Grid.SetColumn(icon, 0);

        Grid.SetRow(nameBlock, rowIndex);
        Grid.SetColumn(nameBlock, 1);

        Grid.SetRow(versionBlock, rowIndex);
        Grid.SetColumn(versionBlock, 3);

        grid.Children.Add(icon);
        grid.Children.Add(nameBlock);
        grid.Children.Add(versionBlock);

        if (string.IsNullOrEmpty(diagnostics.PrimaryVersion))
        {
            AddVersionInitializationButton(grid, rowIndex, proj);
        }
        else
        {
            StackPanel buttonsPanel = CreateVersionButtons(diagnostics.PrimaryVersion!, (segment, decrease) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                string newVersion = ModifyVersionSegment(diagnostics.PrimaryVersion!, segment, decrease);

                VersionUpCommand.AlignProjectVersions(proj, newVersion);

                if (_popup != null)
                {
                    _popup.IsOpen = false;
                }

                UpdateState();
            });

            Grid.SetRow(buttonsPanel, rowIndex);
            Grid.SetColumn(buttonsPanel, 4);
            grid.Children.Add(buttonsPanel);
        }

        if (diagnostics.IsOutOfSync)
        {
            CrispImage warningIcon = new()
            {
                Width             = 14,
                Height            = 14,
                Margin            = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Moniker           = KnownMonikers.StatusWarning,
                ToolTip           = "Project versions are out of sync!"
            };

            Grid.SetRow(warningIcon, rowIndex);
            Grid.SetColumn(warningIcon, 2);
            grid.Children.Add(warningIcon);
        }

        rowIndex++;
    }

    /// <summary>
    /// Adds a button to initialize the project version if no version is defined.
    /// </summary>
    /// <param name="grid">The grid container.</param>
    /// <param name="rowIndex">The row index in the grid.</param>
    /// <param name="proj">The project reference.</param>
    private void AddVersionInitializationButton(Grid grid, int rowIndex, Project proj)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string projectPath = proj.FullName;

        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return;
        }

        IVersionFileHandler? handler = VersionUpCommand.GetHandlerForFile(projectPath);

        if (handler == null)
        {
            return;
        }

        Button addVersionButton = new()
        {
            Content                    = "Add",
            Margin                     = new Thickness(8, 1, 0, 1),
            Padding                    = new Thickness(6, 0, 6, 0),
            Cursor                     = Cursors.Hand,
            Height                     = 18,
            FontSize                   = 10,
            VerticalAlignment          = VerticalAlignment.Center,
            VerticalContentAlignment    = VerticalAlignment.Center,
            HorizontalContentAlignment  = HorizontalAlignment.Center
        };

        addVersionButton.SetResourceReference(Button.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
        addVersionButton.SetResourceReference(Button.ForegroundProperty, VsBrushes.ToolWindowTextKey);
        addVersionButton.SetResourceReference(Button.BorderBrushProperty, VsBrushes.ToolWindowBorderKey);

        string initialVersion = GetInitialVersion(projectPath);

        addVersionButton.Click += (s, ev) =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VersionUpCommand.AlignProjectVersions(proj, initialVersion);

            _popup?.IsOpen = false;

            UpdateState();
        };

        Grid.SetRow(addVersionButton, rowIndex);
        Grid.SetColumn(addVersionButton, 5);
        grid.Children.Add(addVersionButton);
    }

    /// <summary>
    /// Adds rows representing the out-of-sync details of a project's individual versioned files.
    /// </summary>
    /// <param name="grid">The grid container.</param>
    /// <param name="rowIndex">The current row index in the grid.</param>
    /// <param name="proj">The project reference.</param>
    /// <param name="diagnostics">The version diagnostics of the project.</param>
    private void AddOutOfSyncDetailRows(Grid grid, ref int rowIndex, Project proj, ProjectVersionDiagnostics diagnostics)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (VersionDetails verDetail in diagnostics.Versions)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock fileBlock = new()
            {
                Text              = verDetail.SourceName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(16, 2, 8, 2),
                FontSize          = 11
            };

            fileBlock.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.GrayTextKey);

            TextBlock detailVersionBlock = new()
            {
                Text                = verDetail.Version,
                FontWeight          = FontWeights.Normal,
                FontStyle           = FontStyles.Italic,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                FontSize            = 11
            };

            detailVersionBlock.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.GrayTextKey);

            Button useButton = new()
            {
                Content                    = "Use",
                Margin                     = new Thickness(8, 1, 0, 1),
                Padding                    = new Thickness(6, 0, 6, 0),
                Cursor                     = Cursors.Hand,
                Height                     = 18,
                FontSize                   = 10,
                VerticalAlignment          = VerticalAlignment.Center,
                VerticalContentAlignment    = VerticalAlignment.Center,
                HorizontalContentAlignment  = HorizontalAlignment.Center
            };

            useButton.SetResourceReference(Button.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
            useButton.SetResourceReference(Button.ForegroundProperty, VsBrushes.ToolWindowTextKey);
            useButton.SetResourceReference(Button.BorderBrushProperty, VsBrushes.ToolWindowBorderKey);

            useButton.Click += (s, ev) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                VersionUpCommand.AlignProjectVersions(proj, verDetail.Version);

                _popup?.IsOpen = false;

                UpdateState();
            };

            Grid.SetRow(fileBlock, rowIndex);
            Grid.SetColumn(fileBlock, 1);

            Grid.SetRow(detailVersionBlock, rowIndex);
            Grid.SetColumn(detailVersionBlock, 3);

            Grid.SetRow(useButton, rowIndex);
            Grid.SetColumn(useButton, 5);

            StackPanel fileButtonsPanel = CreateVersionButtons(verDetail.Version, (segment, decrease) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                string newVersion = ModifyVersionSegment(verDetail.Version, segment, decrease);

                VersionUpCommand.AlignFileVersion(verDetail.FilePath, newVersion);

                if (_popup != null)
                {
                    _popup.IsOpen = false;
                }

                UpdateState();
            });

            Grid.SetRow(fileButtonsPanel, rowIndex);
            Grid.SetColumn(fileButtonsPanel, 4);

            grid.Children.Add(fileBlock);
            grid.Children.Add(detailVersionBlock);
            grid.Children.Add(fileButtonsPanel);
            grid.Children.Add(useButton);

            rowIndex++;
        }
    }

	    /// <summary>
	    /// Determines the appropriate initial version string for a project file based on its file extension or name.
	    /// </summary>
	    /// <param name="filePath">The absolute path to the project file.</param>
	    /// <returns>The initial version string, typically "1.0.0" or "1.0.0.0".</returns>
	    private static string GetInitialVersion(string filePath)
	    {
	        if (string.IsNullOrEmpty(filePath))
	        {
	            return "1.0.0";
	        }

	        string ext = Path.GetExtension(filePath).ToLowerInvariant();
	        string fileName = Path.GetFileName(filePath).ToLowerInvariant();

	        if (fileName == "package.appxmanifest" || ext == ".rc")
	        {
	            return "1.0.0.0";
	        }

	        return "1.0.0";
	    }

	    /// <summary>
	    /// Retrieves the appropriate Visual Studio image moniker for the given project based on its file extension.
	    /// </summary>
	    /// <param name="proj">The project for which to retrieve the moniker.</param>
	    /// <returns>An <see cref="ImageMoniker"/> representing the project type.</returns>
	    private static ImageMoniker GetMonikerForProject(Project proj)
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        try
	        {
	            string ext = Path.GetExtension(proj.FullName).ToLowerInvariant();

	            return ext switch
	            {
	                ".csproj" => KnownMonikers.CSProjectNode,
	                ".fsproj" => KnownMonikers.FSProjectNode,
	                ".vbproj" => KnownMonikers.VBProjectNode,
	                _ => KnownMonikers.CSProjectNode
	            };
	        }
	        catch
	        {
	            return KnownMonikers.CSProjectNode;
	        }
	    }

	    /// <summary>
	    /// Retrieves the currently active project from the Visual Studio environment.
	    /// </summary>
	    /// <returns>The active <see cref="Project"/>, or <c>null</c> if no active project can be determined.</returns>
	    private Project? GetActiveProject()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();

	        try
	        {
	            if (_dte.ActiveDocument != null && _dte.ActiveDocument.ProjectItem != null && _dte.ActiveDocument.ProjectItem.ContainingProject != null)
	            {
	                return _dte.ActiveDocument.ProjectItem.ContainingProject;
	            }
	        }
	        catch
	        {
	            // Ignore if active document is not a project item (e.g. misc file)
	        }

	        try
	        {
	            if (_dte.SelectedItems != null && _dte.SelectedItems.Count > 0)
	            {
	                SelectedItem? selectedItem = _dte.SelectedItems.Item(1);

	                if (selectedItem != null)
	                {
	                    if (selectedItem.Project != null)
	                    {
	                        return selectedItem.Project;
	                    }

	                    if (selectedItem.ProjectItem != null && selectedItem.ProjectItem.ContainingProject != null)
	                    {
	                        return selectedItem.ProjectItem.ContainingProject;
	                    }
	                }
	            }
	        }
	        catch
	        {
	            // Ignore exceptions from SelectedItems
	        }

	        try
	        {
	            if (_dte.ActiveSolutionProjects is Array activeProjects && activeProjects.Length > 0)
	            {
	                return activeProjects.GetValue(0) as Project;
	            }
	        }
	        catch
	        {
	            // Ignore
	        }

	        return null;
	    }

	    /// <summary>
	    /// Handles the selection change event in the Visual Studio environment, updating the control's state.
	    /// </summary>
	    private void OnSelectionChanged()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();
	        UpdateState();
	    }

	    /// <summary>
	    /// Handles the document saved event in the Visual Studio environment, updating the control's state.
	    /// </summary>
	    /// <param name="document">The document that was saved.</param>
	    private void OnDocumentSaved(Document document)
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();
	        UpdateState();
	    }

	    /// <summary>
	    /// Handles the window activated event in the Visual Studio environment, updating the control's state.
	    /// </summary>
	    /// <param name="gotFocus">The window receiving focus.</param>
	    /// <param name="lostFocus">The window losing focus.</param>
	    private void OnWindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();
	        UpdateState();
	    }

	    /// <summary>
	    /// Handles the solution opened event in the Visual Studio environment, updating the control's state.
	    /// </summary>
	    private void OnSolutionOpened()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();
	        UpdateState();
	    }

	    /// <summary>
	    /// Handles the solution closed event in the Visual Studio environment, updating the control's state.
	    /// </summary>
	    private void OnSolutionClosed()
	    {
	        ThreadHelper.ThrowIfNotOnUIThread();
	        UpdateState();
	    }

	    /// <summary>
	    /// Modifies a specific segment of the version string (either increment or decrement).
	    /// </summary>
	    /// <param name="currentVersion">The current version string.</param>
	    /// <param name="segment">The version segment to modify.</param>
	    /// <param name="decrease">True to decrement, false to increment.</param>
	    /// <returns>The modified version string.</returns>
	    private static string ModifyVersionSegment(string currentVersion, VersionSegment segment, bool decrease)
	    {
	        if (string.IsNullOrWhiteSpace(currentVersion))
	        {
	            return "1.0.0";
	        }

	        if (!Version.TryParse(currentVersion, out Version parsedVersion))
	        {
	            return "1.0.0";
	        }

	        int major = parsedVersion.Major;
	        int minor = parsedVersion.Minor;
	        int build = parsedVersion.Build < 0 ? 0 : parsedVersion.Build;
	        int revision = parsedVersion.Revision < 0 ? 0 : parsedVersion.Revision;

	        if (decrease)
	        {
	            switch (segment)
	            {
	                case VersionSegment.Major:
	                    major = Math.Max(0, major - 1);
	                    break;

	                case VersionSegment.Minor:
	                    minor = Math.Max(0, minor - 1);
	                    break;

	                case VersionSegment.Build:
	                    build = Math.Max(0, build - 1);
	                    break;

	                case VersionSegment.Revision:
	                    revision = Math.Max(0, revision - 1);
	                    break;
	            }
	        }
	        else
	        {
	            switch (segment)
	            {
	                case VersionSegment.Major:
	                    major++;
	                    minor = 0;
	                    build = 0;
	                    revision = 0;
	                    break;

	                case VersionSegment.Minor:
	                    minor++;
	                    build = 0;
	                    revision = 0;
	                    break;

	                case VersionSegment.Build:
	                    build++;
	                    revision = 0;
	                    break;

	                case VersionSegment.Revision:
	                    revision++;
	                    break;
	            }
	        }

	        if (parsedVersion.Revision >= 0)
	        {
	            return $"{major}.{minor}.{build}.{revision}";
	        }

	        if (parsedVersion.Build >= 0)
	        {
	            return $"{major}.{minor}.{build}";
	        }

	        return $"{major}.{minor}";
	    }

	    /// <summary>
	    /// Creates a StackPanel containing the Major, Minor, and Build increment/decrement buttons.
	    /// </summary>
	    /// <param name="currentVersion">The current version string.</param>
	    /// <param name="onSegmentClicked">The callback invoked when a version segment button is clicked.</param>
	    /// <returns>A StackPanel containing the three buttons.</returns>
	    private StackPanel CreateVersionButtons(string currentVersion, Action<VersionSegment, bool> onSegmentClicked)
	    {
	        StackPanel stack = new()
	        {
	            Orientation = Orientation.Horizontal,
	            Margin      = new Thickness(8, 0, 0, 0)
	        };

	        bool isShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
	        string sign = isShift ? "-" : "+";

	        Button btnMajor = CreateSingleVersionButton(sign, "Major version");
	        Button btnMinor = CreateSingleVersionButton(sign, "Minor version");
	        Button btnBuild = CreateSingleVersionButton(sign, "Build version");

	        btnMajor.Click += (s, e) => onSegmentClicked(VersionSegment.Major, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
	        btnMinor.Click += (s, e) => onSegmentClicked(VersionSegment.Minor, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
	        btnBuild.Click += (s, e) => onSegmentClicked(VersionSegment.Build, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));

	        stack.Children.Add(btnMajor);
	        stack.Children.Add(btnMinor);
	        stack.Children.Add(btnBuild);

	        _versionButtons.Add(btnMajor);
	        _versionButtons.Add(btnMinor);
	        _versionButtons.Add(btnBuild);

	        return stack;
	    }

	    /// <summary>
	    /// Creates a single increment/decrement button with the specified content and tooltip.
	    /// </summary>
	    /// <param name="content">The button label content.</param>
	    /// <param name="segmentName">The name of the version segment.</param>
	    /// <returns>A configured Button control.</returns>
	    private Button CreateSingleVersionButton(string content, string segmentName)
	    {
	        Button btn = new()
	        {
	            Content                    = content,
	            Margin                     = new Thickness(2, 1, 2, 1),
	            Padding                    = new Thickness(0),
	            Cursor                     = Cursors.Hand,
	            Width                      = 18,
	            Height                     = 18,
	            FontSize                   = 10,
	            FontWeight                 = FontWeights.Bold,
	            VerticalAlignment          = VerticalAlignment.Center,
	            VerticalContentAlignment    = VerticalAlignment.Center,
	            HorizontalContentAlignment  = HorizontalAlignment.Center,
	            ToolTip                    = $"{content} {segmentName}"
	        };

	        btn.SetResourceReference(Button.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
	        btn.SetResourceReference(Button.ForegroundProperty, VsBrushes.ToolWindowTextKey);
	        btn.SetResourceReference(Button.BorderBrushProperty, VsBrushes.ToolWindowBorderKey);

	        return btn;
	    }

	    /// <summary>
	    /// Updates all version button labels (+ or -) depending on whether the Shift key is pressed.
	    /// </summary>
	    private void UpdateButtonLabels()
	    {
	        bool isShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
	        string content = isShift ? "-" : "+";

	        foreach (Button btn in _versionButtons)
	        {
	            btn.Content = content;

	            if (btn.ToolTip is string tooltipText)
	            {
	                if (tooltipText.StartsWith("+") || tooltipText.StartsWith("-"))
	                {
	                    btn.ToolTip = $"{content}{tooltipText.Substring(1)}";
	                }
	            }
	        }
	    }

	    /// <summary>
	    /// Handles the Opened event of the popup, subscribing to parent window key events.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnPopupOpened(object sender, EventArgs e)
	    {
	        System.Windows.Window parentWindow = System.Windows.Window.GetWindow(this) ?? Application.Current.MainWindow;

	        if (parentWindow != null)
	        {
	            parentWindow.PreviewKeyDown += OnWindowPreviewKeyDown;
	            parentWindow.PreviewKeyUp += OnWindowPreviewKeyUp;
	        }

	        UpdateButtonLabels();
	    }

	    /// <summary>
	    /// Handles the Closed event of the popup, unsubscribing from parent window key events.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnPopupClosed(object sender, EventArgs e)
	    {
	        System.Windows.Window parentWindow = System.Windows.Window.GetWindow(this) ?? Application.Current.MainWindow;

	        if (parentWindow != null)
	        {
	            parentWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
	            parentWindow.PreviewKeyUp -= OnWindowPreviewKeyUp;
	        }
	    }

	    /// <summary>
	    /// Handles the PreviewKeyDown event of the parent window to detect Shift key press.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
	    {
	        if (e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.System)
	        {
	            UpdateButtonLabels();
	        }
	    }

	    /// <summary>
	    /// Handles the PreviewKeyUp event of the parent window to detect Shift key release.
	    /// </summary>
	    /// <param name="sender">The source of the event.</param>
	    /// <param name="e">The event data.</param>
	    private void OnWindowPreviewKeyUp(object sender, KeyEventArgs e)
	    {
	        if (e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.System)
	        {
	            UpdateButtonLabels();
	        }
	    }
	}
}
