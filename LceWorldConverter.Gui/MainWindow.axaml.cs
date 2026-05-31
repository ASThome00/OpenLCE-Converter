using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LceWorldConverter;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgIcon = MsBox.Avalonia.Enums.Icon;

namespace LceWorldConverter.Gui;

public partial class MainWindow : Window
{
    private readonly string[] _stepTitles = ["Choose Direction", "Configure Options", "Review And Convert"];
    private readonly string[] _stepSubtitles =
    [
        "Choose what kind of conversion you want to run.",
        "Set paths and options for the selected conversion direction.",
        "Check the summary, then run the conversion and watch the live log."
    ];

    private readonly MainWindowViewModel _viewModel = new();
    private int _currentStep;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        WireDragAndDrop();
        ApplyDirectionState();
        GoToStep(0);
    }

    private void WireDragAndDrop()
    {
        JavaInputTextBox.AddHandler(DragDrop.DragOverEvent, PathTextBox_DragOver);
        JavaInputTextBox.AddHandler(DragDrop.DropEvent, JavaInputTextBox_Drop);

        JavaOutputTextBox.AddHandler(DragDrop.DragOverEvent, DirectoryTextBox_DragOver);
        JavaOutputTextBox.AddHandler(DragDrop.DropEvent, JavaOutputTextBox_Drop);

        LceInputTextBox.AddHandler(DragDrop.DragOverEvent, PathTextBox_DragOver);
        LceInputTextBox.AddHandler(DragDrop.DropEvent, LceInputTextBox_Drop);

        LceOutputTextBox.AddHandler(DragDrop.DragOverEvent, DirectoryTextBox_DragOver);
        LceOutputTextBox.AddHandler(DragDrop.DropEvent, LceOutputTextBox_Drop);
    }

    private void GoToStep(int step)
    {
        if (step < 0 || step > 2)
            return;

        _currentStep = step;
        Step0View.IsVisible = step == 0;
        Step1View.IsVisible = step == 1;
        Step2View.IsVisible = step == 2;

        StepTitleText.Text = _stepTitles[step];
        StepSubtitleText.Text = _stepSubtitles[step];
        BackButton.IsVisible = step > 0;
        NextButton.IsVisible = step < 2;
        NextButton.Content = step == 1 ? "Review" : "Next";

        UpdateStepBadges(step);
    }

    private void UpdateStepBadges(int activeStep)
    {
        UpdateStepBadge(Step0Badge, activeStep >= 0);
        UpdateStepBadge(Step1Badge, activeStep >= 1);
        UpdateStepBadge(Step2Badge, activeStep >= 2);
    }

    private static void UpdateStepBadge(Border badge, bool active)
    {
        badge.Background = active
            ? new SolidColorBrush(Color.FromRgb(21, 116, 92))
            : new SolidColorBrush(Color.FromRgb(200, 212, 207));

        if (badge.Child is TextBlock text)
            text.Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(24, 53, 45));
    }

    private void ApplyDirectionState()
    {
        bool javaSelected = _viewModel.SelectedDirection == ConversionDirection.JavaToLce;

        JavaStepPanel.IsVisible = javaSelected;
        LceStepPanel.IsVisible = !javaSelected;

        ApplyDirectionCardStyle(JavaDirectionCard, javaSelected);
        ApplyDirectionCardStyle(LceDirectionCard, !javaSelected);
    }

    private static void ApplyDirectionCardStyle(Border card, bool selected)
    {
        card.BorderBrush = selected
            ? new SolidColorBrush(Color.FromRgb(21, 116, 92))
            : new SolidColorBrush(Color.FromRgb(214, 224, 218));
        card.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        card.Background = selected
            ? new SolidColorBrush(Color.FromRgb(248, 252, 250))
            : Brushes.White;
    }

    private void JavaDirectionCard_Tapped(object? sender, TappedEventArgs e)
    {
        _viewModel.SelectedDirection = ConversionDirection.JavaToLce;
        ApplyDirectionState();
    }

    private void LceDirectionCard_Tapped(object? sender, TappedEventArgs e)
    {
        _viewModel.SelectedDirection = ConversionDirection.LceToJava;
        ApplyDirectionState();
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        GoToStep(_currentStep - 1);
    }

    private async void NextButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentStep == 1 && !await ValidateCurrentStepAsync())
            return;

        GoToStep(_currentStep + 1);
    }

    private async Task<bool> ValidateCurrentStepAsync()
    {
        if (_viewModel.TryBuildCurrentRequest(out _, out string title, out string message))
            return true;

        await ShowMessageAsync(title, message, MsgIcon.Warning);
        return false;
    }

    private async void ConvertButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryBuildCurrentRequest(out ConversionRequest? request, out string title, out string message))
        {
            await ShowMessageAsync(title, message, MsgIcon.Warning);
            return;
        }

        await RunConversionAsync(request!);
    }

    private async Task RunConversionAsync(ConversionRequest request)
    {
        ToggleBusy(true, "Converting...");
        LogTextBox.Clear();

        try
        {
            var logger = new UiConversionLogger(AppendLog);
            var service = new LceWorldConversionService();
            ConversionResult result = await Task.Run(() => service.Convert(request, logger));

            AppendLog(string.Empty);
            AppendLog($"Finished: {result.OutputPath}");

            await ShowMessageAsync(
                "OpenLCE Converter",
                $"Conversion complete.{Environment.NewLine}{Environment.NewLine}Output: {result.OutputPath}",
                MsgIcon.Success);
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
            await ShowMessageAsync("Conversion Failed", ex.Message, MsgIcon.Error);
        }
        finally
        {
            ToggleBusy(false, "Ready");
        }
    }

    private async Task ShowMessageAsync(string title, string message, MsgIcon icon)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, icon);
        await box.ShowWindowDialogAsync(this);
    }

    private void ToggleBusy(bool busy, string status)
    {
        Cursor = busy ? new Cursor(StandardCursorType.Wait) : Cursor.Default;
        ConvertButton.IsEnabled = !busy;
        BackButton.IsEnabled = !busy;
        NextButton.IsEnabled = !busy;
        StatusText.Text = status;
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (LogTextBox.Text?.Length > 0)
                LogTextBox.Text += Environment.NewLine;

            LogTextBox.Text += message;
            LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;

            if (!string.IsNullOrWhiteSpace(message))
                StatusText.Text = message;
        });
    }

    private async void BrowseJavaFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await BrowseForFolderAsync("Select Java world folder", _viewModel.JavaInputPath);
        if (path is null)
            return;

        _viewModel.JavaInputPath = path;
        AutoFillJavaOutput();
    }

    private async void BrowseJavaZipButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await BrowseForFileAsync(
            "Select Java world zip",
            new FilePickerFileType("Zip archives") { Patterns = ["*.zip"] });
        if (path is null)
            return;

        _viewModel.JavaInputPath = path;
        AutoFillJavaOutput();
    }

    private async void BrowseJavaOutputButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await BrowseForFolderAsync("Choose output folder for saveData.ms", _viewModel.JavaOutputPath);
        if (path is not null)
            _viewModel.JavaOutputPath = path;
    }

    private async void BrowseLceInputButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await BrowseForFileAsync(
            "Select saveData.ms",
            new FilePickerFileType("LCE save files") { Patterns = ["saveData.ms", "*.ms", "*"] });
        if (path is null)
            return;

        _viewModel.LceInputPath = path;
        AutoFillLceOutput();
    }

    private async void BrowseLceOutputButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await BrowseForFolderAsync("Choose output folder for Java world files", _viewModel.LceOutputPath);
        if (path is not null)
            _viewModel.LceOutputPath = path;
    }

    private async Task<string?> BrowseForFileAsync(string title, FilePickerFileType fileType)
    {
        IStorageProvider? storage = StorageProvider;
        if (storage is null)
            return null;

        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> BrowseForFolderAsync(string title, string currentPath)
    {
        IStorageProvider? storage = StorageProvider;
        if (storage is null)
            return null;

        IStorageFolder? startLocation = null;
        if (Directory.Exists(currentPath))
            startLocation = await storage.TryGetFolderFromPathAsync(currentPath);

        IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = startLocation,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private void AutoFillJavaOutput()
    {
        _viewModel.AutoFillJavaOutput(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
    }

    private void AutoFillLceOutput()
    {
        _viewModel.AutoFillLceOutput(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
    }

    private static void PathTextBox_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private static void DirectoryTextBox_DragOver(object? sender, DragEventArgs e)
    {
        string? path = GetFirstDroppedPath(e);
        e.DragEffects = path is not null && Directory.Exists(path) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void JavaInputTextBox_Drop(object? sender, DragEventArgs e)
    {
        string? path = GetFirstDroppedPath(e);
        if (path is null)
            return;

        if (!Directory.Exists(path) && !(File.Exists(path) && string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase)))
            return;

        _viewModel.JavaInputPath = path;
        AutoFillJavaOutput();
    }

    private void LceInputTextBox_Drop(object? sender, DragEventArgs e)
    {
        string? path = GetFirstDroppedPath(e);
        if (path is null || !File.Exists(path))
            return;

        _viewModel.LceInputPath = path;
        AutoFillLceOutput();
    }

    private void JavaOutputTextBox_Drop(object? sender, DragEventArgs e)
    {
        string? path = GetFirstDroppedPath(e);
        if (path is not null && Directory.Exists(path))
            _viewModel.JavaOutputPath = path;
    }

    private void LceOutputTextBox_Drop(object? sender, DragEventArgs e)
    {
        string? path = GetFirstDroppedPath(e);
        if (path is not null && Directory.Exists(path))
            _viewModel.LceOutputPath = path;
    }

    private static string? GetFirstDroppedPath(DragEventArgs e)
    {
        IStorageItem? first = e.Data.GetFiles()?.FirstOrDefault();
        return first?.TryGetLocalPath();
    }
}
