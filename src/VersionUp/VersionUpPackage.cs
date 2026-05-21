namespace VersionUp;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("VersionUp", "Increment project versions through a context menu in the Solution Explorer.", "1.0")]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
public sealed class VersionUpPackage : AsyncPackage, IVersionLogger
{
    /// <summary>
    /// VersionUpPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "d3f962f7-a630-4c98-9b21-7b1a9908f87c";

    private VersionStatusBarControl? _statusBarControl;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionUpPackage"/> class.
    /// </summary>
    public VersionUpPackage()
    {
    }

    /// <summary>
    /// Logs a version increment activity message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[VersionUp] {message}");
    }

    /// <inheritdoc />
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await VersionUpCommand.InitializeAsync(this);

        _statusBarControl = new VersionStatusBarControl();
        _ = StatusBarInjector.InjectControlAsync(_statusBarControl);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_statusBarControl != null)
            {
                JoinableTaskFactory.Run(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();
                    _statusBarControl.Dispose();
                });

                _statusBarControl = null;
            }
        }

        base.Dispose(disposing);
    }
}
