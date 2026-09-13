using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScriptForge.Core;

namespace ScriptForge.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SourceKind_Changed(object sender, RoutedEventArgs e)
    {
        if (EntryPanel is null) return;
        EntryPanel.Visibility = RbFolder.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
    {
        if (RbFolder.IsChecked == true)
        {
            var dialog = new OpenFolderDialog { Title = "Select the folder containing your scripts" };
            if (dialog.ShowDialog() == true)
            {
                TxtInput.Text = dialog.FolderName;
            }
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a .ps1 or .bat script",
                Filter = "Scripts (*.ps1;*.bat;*.cmd)|*.ps1;*.bat;*.cmd|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog() == true)
            {
                TxtInput.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(TxtOutput.Text))
                {
                    TxtOutput.Text = Path.ChangeExtension(dialog.FileName, ".exe");
                }
            }
        }
    }

    private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Choose where to save the executable",
            Filter = "Executable (*.exe)|*.exe",
            FileName = string.IsNullOrWhiteSpace(TxtOutput.Text) ? "output.exe" : TxtOutput.Text,
        };
        if (dialog.ShowDialog() == true)
        {
            TxtOutput.Text = dialog.FileName;
        }
    }

    private void BtnBrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select an icon", Filter = "Icon files (*.ico)|*.ico" };
        if (dialog.ShowDialog() == true)
        {
            TxtIcon.Text = dialog.FileName;
            try
            {
                IconPreview.Source = new BitmapImage(new Uri(dialog.FileName));
            }
            catch
            {
                IconPreview.Source = null;
            }
        }
    }

    private async void BtnBuild_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInput.Text) || string.IsNullOrWhiteSpace(TxtOutput.Text))
        {
            MessageBox.Show(this, "Please choose a source and an output path.", "ScriptForge",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var options = new BuildOptions
        {
            InputPath = TxtInput.Text.Trim(),
            OutputPath = TxtOutput.Text.Trim(),
            EntryRelativePath = string.IsNullOrWhiteSpace(TxtEntry.Text) ? null : TxtEntry.Text.Trim(),
            IconPath = string.IsNullOrWhiteSpace(TxtIcon.Text) ? null : TxtIcon.Text.Trim(),
            Version = string.IsNullOrWhiteSpace(TxtVersion.Text) ? "1.0.0.0" : TxtVersion.Text.Trim(),
            Product = TxtProduct.Text.Trim(),
            Company = TxtCompany.Text.Trim(),
            Copyright = TxtCopyright.Text.Trim(),
            Description = TxtDescription.Text.Trim(),
            RequireAdmin = ChkAdmin.IsChecked == true,
            ShowConsole = ChkGui.IsChecked != true,
            Progress = new Progress<string>(AppendLog),
        };

        SetBusy(true);
        TxtLog.Clear();
        TxtStatus.Text = "Building...";

        var result = await BuildEngine.BuildAsync(options);

        SetBusy(false);

        if (result.Success)
        {
            TxtStatus.Text = $"Built successfully: {result.OutputPath}";
            MessageBox.Show(this, $"Executable created:\n{result.OutputPath}", "ScriptForge",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            TxtStatus.Text = "Build failed.";
            AppendLog("ERROR: " + result.Error);
            MessageBox.Show(this, result.Error ?? "Unknown error.", "Build failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AppendLog(string line)
    {
        TxtLog.AppendText(line + Environment.NewLine);
        LogScroll.ScrollToEnd();
    }

    private void SetBusy(bool busy)
    {
        BtnBuild.IsEnabled = !busy;
        BtnBrowseInput.IsEnabled = !busy;
        BtnBrowseOutput.IsEnabled = !busy;
        BtnBrowseIcon.IsEnabled = !busy;
    }
}
