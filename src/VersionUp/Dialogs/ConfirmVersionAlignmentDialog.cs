namespace VersionUp.Dialogs;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

/// <summary>
/// A dialog that asks the user if they want to align all versioned files in a project.
/// </summary>
public partial class ConfirmVersionAlignmentDialog : DialogWindow
{
	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	[DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, uint cbAttribute);

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfirmVersionAlignmentDialog"/> class.
	/// </summary>
	/// <param name="projectName">The name of the project.</param>
	/// <param name="newVersion">The new version string.</param>
	public ConfirmVersionAlignmentDialog(string projectName, string newVersion)
	{
		InitializeComponent();

		_messageTextBlock.Text = $"Incrementing version in {projectName} to {newVersion}.\nDo you want to update all other versioned files in this project to the same version?";

		this.KeyDown += (s, e) =>
		{
			if (e.Key == Key.Y)
			{
				this.DialogResult = true;
				this.Close();
			}
			else if (e.Key == Key.N)
			{
				this.DialogResult = false;
				this.Close();
			}
		};
	}

	/// <summary>
	/// Gets a value indicating whether the "Don't ask again" checkbox is checked.
	/// </summary>
	public bool DontAskAgain => _dontAskCheckBox.IsChecked == true;

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
	/// Handles the Click event of the Yes button, setting the dialog result to true and closing.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments.</param>
	private void YesButton_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = true;
		this.Close();
	}

	/// <summary>
	/// Handles the Click event of the No button, setting the dialog result to false and closing.
	/// </summary>
	/// <param name="sender">The event source.</param>
	/// <param name="e">The event arguments.</param>
	private void NoButton_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = false;
		this.Close();
	}
}
