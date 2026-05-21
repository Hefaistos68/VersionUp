namespace VersionUp;

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

/// <summary>
/// A dialog that allows the user to manually enter or modify the version segments of a project.
/// </summary>
public class SetVersionDialog : DialogWindow
{
    private readonly TextBox _versionTextBox;
    private readonly string _originalVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetVersionDialog"/> class.
    /// </summary>
    /// <param name="currentVersion">The current version string of the project.</param>
    public SetVersionDialog(string currentVersion)
    {
        this.Title = "Set Version";
        this.Width = 320;
        this.Height = 160;
        this.MinHeight = 160;
        this.MinWidth = 320;
        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        this.ResizeMode = ResizeMode.NoResize;
        this.ShowInTaskbar = false;

        _originalVersion = currentVersion ?? string.Empty;

        // Styling the Dialog Window using VS colors
        this.SetResourceReference(DialogWindow.BackgroundProperty, VsBrushes.WindowKey);
        this.SetResourceReference(DialogWindow.ForegroundProperty, VsBrushes.WindowTextKey);

        StackPanel mainPanel = new()
        {
            Margin = new Thickness(16),
            Orientation = Orientation.Vertical
        };

        TextBlock label = new()
        {
            Text = "Enter or modify the version:",
            FontSize = 12
        };

        label.SetResourceReference(TextBlock.ForegroundProperty, VsBrushes.WindowTextKey);

        _versionTextBox = new TextBox
        {
            Text = _originalVersion,
            Margin = new Thickness(0, 8, 0, 16),
            Padding = new Thickness(4),
            FontSize = 12
        };

        _versionTextBox.SetResourceReference(TextBox.BackgroundProperty, VsBrushes.ComboBoxBackgroundKey);
        _versionTextBox.SetResourceReference(TextBox.ForegroundProperty, VsBrushes.WindowTextKey);
        _versionTextBox.SetResourceReference(TextBox.BorderBrushProperty, VsBrushes.ComboBoxBorderKey);

        StackPanel buttonPanel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Button okButton = new()
        {
            Content = "OK",
            IsDefault = true,
            Width = 75,
            Height = 23,
            Margin = new Thickness(0, 0, 8, 0)
        };

        okButton.Click += OkButton_Click;

        Button cancelButton = new()
        {
            Content = "Cancel",
            IsCancel = true,
            Width = 75,
            Height = 23
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        mainPanel.Children.Add(label);
        mainPanel.Children.Add(_versionTextBox);
        mainPanel.Children.Add(buttonPanel);

        this.Content = mainPanel;

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
}
