using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using XIVUIColorPreviewer.Controls;
using XIVUIColorPreviewer.Models;
using XIVUIColorPreviewer.Services;
using XIVUIColorPreviewer.ViewModels;

namespace XIVUIColorPreviewer;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel               _viewModel    = new();
    private readonly List<ThemePreviewCard>      _previewCards = [];
    private          TextBlockConfig?            _selectedTextBlock;
    private          ColorRecommendationService? _recommendationService;

    public MainWindow()
    {
        InitializeComponent();
        Title     =  "FFXIV UIColor 配色预览器";
        Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated;

        await _viewModel.InitializeAsync();
        _viewModel.PreviewChanged += RefreshAllPreviews;

        // Initialize pickers
        ForegroundPicker.Initialize(_viewModel.ColorDataService);
        StrokePicker.Initialize(_viewModel.ColorDataService);
        ForegroundPicker.RowNumberChanged += ForegroundPicker_RowNumberChanged;
        StrokePicker.RowNumberChanged     += StrokePicker_RowNumberChanged;
        StrokePicker.DropDownOpened       += StrokePicker_DropDownOpened;

        _recommendationService = new ColorRecommendationService(_viewModel.ColorDataService);

        if (_viewModel.ColorDataService.RowNumbers.Count > 0)
        {
            ForegroundPicker.SelectedRowNumber = _viewModel.ColorDataService.RowNumbers[0];
            StrokePicker.SelectedRowNumber     = _viewModel.ColorDataService.RowNumbers[0];
        }

        TextBlocksList.ItemsSource = _viewModel.TextBlocks;
        SchemesList.ItemsSource    = _viewModel.Schemes;

        BuildPreviewGrid();

        LoadingRing.IsActive   = false;
        MainContent.Visibility = Visibility.Visible;
    }

    private void BuildPreviewGrid()
    {
        PreviewPanel.Children.Clear();
        _previewCards.Clear();

        var   themeNames = _viewModel.ThemeNames;
        Grid? currentRow = null;

        for (var i = 0; i < themeNames.Count; i++)
        {
            if (i % 2 == 0)
            {
                currentRow = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                    ColumnSpacing = 12
                };
                PreviewPanel.Children.Add(currentRow);
            }

            var card = new ThemePreviewCard();
            card.Initialize(themeNames[i], _viewModel.ColorDataService);
            card.TextBlockClicked += Card_TextBlockClicked;
            Grid.SetColumn(card, i % 2);
            currentRow!.Children.Add(card);
            _previewCards.Add(card);
        }
    }

    /// <summary>
    ///     When a text block is clicked in any preview card, select it and load into editor.
    /// </summary>
    private void Card_TextBlockClicked(object? sender, TextBlockConfig config) =>
        SelectTextBlock(config);

    private void SelectTextBlock(TextBlockConfig config)
    {
        _selectedTextBlock          = config;
        UpdateBlockButton.IsEnabled = true;

        // Load settings into editor
        PreviewTextBox.Text                = config.PreviewText;
        ForegroundPicker.SelectedRowNumber = config.ForegroundColorRow;
        StrokeCheckBox.IsChecked           = config.StrokeEnabled;
        StrokePanel.Visibility             = config.StrokeEnabled ? Visibility.Visible : Visibility.Collapsed;
        StrokePicker.SelectedRowNumber     = config.StrokeColorRow;

        _viewModel.ForegroundColorRow = config.ForegroundColorRow;
        _viewModel.StrokeEnabled      = config.StrokeEnabled;
        _viewModel.StrokeColorRow     = config.StrokeColorRow;
        _viewModel.PreviewText        = config.PreviewText;

        // Update selection highlight in all cards
        foreach (var card in _previewCards) card.SetSelectedBlock(config);

        // Also select in the list
        TextBlocksList.SelectedItem = config;
    }

    private void RefreshAllPreviews()
    {
        foreach (var card in _previewCards) card.UpdatePreview(_viewModel.TextBlocks);
    }

    private void PreviewTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
        _viewModel.PreviewText = PreviewTextBox.Text;

    private void ForegroundPicker_RowNumberChanged(object? sender, int rowNumber) =>
        _viewModel.ForegroundColorRow = rowNumber;

    private void StrokeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _viewModel.StrokeEnabled = StrokeCheckBox.IsChecked == true;
        StrokePanel.Visibility   = _viewModel.StrokeEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StrokePicker_RowNumberChanged(object? sender, int rowNumber) =>
        _viewModel.StrokeColorRow = rowNumber;

    private void StrokePicker_DropDownOpened(object? sender, EventArgs e)
    {
        // Compute stroke recommendations based on current foreground color
        if (_recommendationService == null) return;

        var recommendations = _recommendationService.GetStrokeRecommendations(_viewModel.ForegroundColorRow);
        StrokePicker.SetRecommendations(recommendations);
    }

    private void AddPreview_Click(object sender, RoutedEventArgs e) =>
        _viewModel.AddTextBlockCommand.Execute(null);

    private void UpdateBlock_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTextBlock == null) return;

        _selectedTextBlock.PreviewText        = PreviewTextBox.Text;
        _selectedTextBlock.ForegroundColorRow = _viewModel.ForegroundColorRow;
        _selectedTextBlock.StrokeEnabled      = _viewModel.StrokeEnabled;
        _selectedTextBlock.StrokeColorRow     = _viewModel.StrokeColorRow;
        // PropertyChanged auto-triggers preview refresh
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TextBlockConfig config) _viewModel.MoveTextBlockUpCommand.Execute(config);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TextBlockConfig config) _viewModel.MoveTextBlockDownCommand.Execute(config);
    }

    private void RemoveBlock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TextBlockConfig config)
        {
            if (config == _selectedTextBlock)
            {
                _selectedTextBlock          = null;
                UpdateBlockButton.IsEnabled = false;
                foreach (var card in _previewCards)
                    card.SetSelectedBlock(null);
            }

            _viewModel.RemoveTextBlockCommand.Execute(config);
        }
    }

    private void TextBlocksList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TextBlockConfig config) SelectTextBlock(config);
    }

    private async void SaveScheme_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NewSchemeName = SchemeNameBox.Text;
        await _viewModel.SaveSchemeCommand.ExecuteAsync(null);
        SchemeNameBox.Text = string.Empty;
    }

    private void SchemesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ColorScheme scheme) _viewModel.LoadSchemeCommand.Execute(scheme);
    }

    private async void DeleteScheme_Click(object sender, RoutedEventArgs e)
    {
        ColorScheme? scheme = null;
        if (sender is Button btn)
            scheme = btn.Tag as ColorScheme;
        else if (sender is MenuFlyoutItem menuItem)
            scheme = menuItem.Tag as ColorScheme;

        if (scheme != null)
            await _viewModel.DeleteSchemeCommand.ExecuteAsync(scheme);
    }

    private async void RenameScheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is ColorScheme scheme)
        {
            var dialog = new ContentDialog
            {
                Title             = "重命名方案",
                PrimaryButtonText = "确定",
                CloseButtonText   = "取消",
                DefaultButton     = ContentDialogButton.Primary,
                XamlRoot          = Content.XamlRoot
            };

            var textBox = new TextBox { Text = scheme.Name };
            dialog.Content = textBox;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text)) await _viewModel.RenameSchemeAsync(scheme, textBox.Text.Trim());
        }
    }

    private void SchemeItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Grid grid) return;

        // Get the scheme from the DataContext
        var scheme = grid.DataContext as ColorScheme;
        if (scheme == null || scheme.TextBlocks.Count == 0) return;

        // Build a preview tooltip showing text blocks with their colors (using first theme)
        var previewTheme = _viewModel.ThemeNames.Count > 0 ? _viewModel.ThemeNames[0] : null;
        if (previewTheme == null) return;

        var panel = new StackPanel
        {
            Spacing    = 2,
            Padding    = new Thickness(4),
            Background = new SolidColorBrush(ColorHelper.FromArgb(220, 30, 30, 30))
        };

        foreach (var tb in scheme.TextBlocks)
        {
            var fgEntry = _viewModel.ColorDataService.GetEntry(tb.ForegroundColorRow);
            var fgColor = fgEntry?.ThemeColors.TryGetValue(previewTheme, out var c) == true
                              ? c
                              : Color.FromArgb(255, 255, 255, 255);

            var textBlock = new TextBlock
            {
                Text     = tb.PreviewText,
                FontSize = 14,
                Foreground = new SolidColorBrush
                (
                    ColorHelper.FromArgb(fgColor.A, fgColor.R, fgColor.G, fgColor.B)
                )
            };
            panel.Children.Add(textBlock);
        }

        ToolTipService.SetToolTip(grid, panel);
    }

    private void PreviewArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // If the event wasn't handled by a text block click, deselect
        if (!e.Handled) DeselectTextBlock();
    }

    private void DeselectTextBlock()
    {
        if (_selectedTextBlock == null) return;

        _selectedTextBlock          = null;
        UpdateBlockButton.IsEnabled = false;
        TextBlocksList.SelectedItem = null;

        foreach (var card in _previewCards) card.SetSelectedBlock(null);
    }
}
