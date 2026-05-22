namespace VersionUp.Dialogs;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

/// <summary>
/// A dialog that allows the user to manually enter or modify the version segments of a project.
/// </summary>
public partial class SetVersionDialog : DialogWindow
{
	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private readonly string _originalVersion;

	[DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, uint cbAttribute);

	/// <summary>
	/// Initializes a new instance of the <see cref="SetVersionDialog"/> class.
	/// </summary>
	/// <param name="currentVersion">The current version string of the project.</param>
	public SetVersionDialog(string currentVersion)
	{
		InitializeComponent();

		_originalVersion = currentVersion ?? string.Empty;
		_versionTextBox.Text = _originalVersion;

		// Focus the text box and select the text
		this.Loaded += (s, e) =>
		{
			_versionTextBox.Focus();
			_versionTextBox.SelectAll();
		};
	}

	/// <summary>
	/// Gets the modified version string entered by the user.
	/// </summary>
	public string VersionResult => _versionTextBox.Text;

	/// <inheritdoc />
	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);

		try
		{
			var windowColor = (System.Windows.Media.Color)this.FindResource(VsColors.WindowKey);
			bool isDark     = (windowColor.R + windowColor.G + windowColor.B) / 3 < 128;

			if (isDark)
			{
				IntPtr hwnd     = new WindowInteropHelper(this).Handle;
				int useDarkMode = 1;

				DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
			}
		}
		catch
		{
			// Fail safe: absorb any exception on older OS versions or test environments.
		}
	}

	/// <summary>
	/// Handles the Click event of the OK button, validating the version input before closing.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments.</param>
	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		string input = _versionTextBox.Text.Trim();

		if (string.IsNullOrEmpty(input))
		{
			MessageBox.Show("Version cannot be empty.", "Invalid Version", MessageBoxButton.OK, MessageBoxImage.Error);

			return;
		}

		if (!Version.TryParse(input, out _))
		{
			MessageBox.Show("Please enter a valid version format (e.g. 1.0.0 or 1.0.0.0).", "Invalid Version Format", MessageBoxButton.OK, MessageBoxImage.Error);

			return;
		}

		this.DialogResult = true;
		this.Close();
	}

	/// <summary>
	/// Handles the Click event of the Cancel button, closing the dialog.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments.</param>
	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = false;
		this.Close();
	}
}
