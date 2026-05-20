using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using XIVUIColorPreviewer.Models;
using XIVUIColorPreviewer.Services;

namespace XIVUIColorPreviewer.Controls;

public sealed partial class ThemePreviewCard : UserControl
{
    private string                          _themeName = string.Empty;
    private UIColorDataService?             _dataService;
    private IReadOnlyList<TextBlockConfig>? _currentTextBlocks;
    private TextBlockConfig?                _selectedBlock;

    // Minimum padding around text content
    private const double PaddingH    = 32;
    private const double PaddingV    = 24;
    private new const double FontSize = 18;
    private const double LineHeight  = FontSize * 1.5;
    private const double LineSpacing = 4;

    /// <summary>
    ///     Fired when a text block is clicked in the preview.
    /// </summary>
    public event EventHandler<TextBlockConfig>? TextBlockClicked;

    public ThemePreviewCard() =>
        InitializeComponent();

    public void Initialize(string themeName, UIColorDataService dataService)
    {
        _themeName         = themeName;
        _dataService       = dataService;
        ThemeNameText.Text = themeName;

        var bgPath                                      = Path.Combine(AppContext.BaseDirectory, "Assets", "Game", "Backgrounds", $"{themeName}.png");
        if (File.Exists(bgPath)) BackgroundImage.Source = new BitmapImage(new Uri(bgPath));
    }

    public void SetSelectedBlock(TextBlockConfig? config)
    {
        _selectedBlock = config;
        // Re-render to show selection highlight
        if (_currentTextBlocks != null)
            RenderContent();
    }

    public void UpdatePreview(IReadOnlyList<TextBlockConfig> textBlocks)
    {
        if (_dataService == null) return;
        _currentTextBlocks = textBlocks;
        RenderContent();
    }

    private void RenderContent()
    {
        if (_currentTextBlocks == null || _dataService == null) return;

        TextStackPanel.Children.Clear();

        if (_currentTextBlocks.Count == 0)
        {
            // Show placeholder
            PreviewGrid.MinHeight = 80;
            PreviewGrid.MinWidth  = 160;
            return;
        }

        // Calculate content dimensions
        double maxTextWidth = 0;

        foreach (var config in _currentTextBlocks)
        {
            var w                              = EstimateTextWidth(config.PreviewText, FontSize);
            if (w > maxTextWidth) maxTextWidth = w;
        }

        var totalContentHeight = (_currentTextBlocks.Count * LineHeight) + ((_currentTextBlocks.Count - 1) * LineSpacing);

        // Size the preview area: content + padding on all sides
        var areaWidth  = maxTextWidth       + (PaddingH * 2);
        var areaHeight = totalContentHeight + (PaddingV * 2);

        PreviewGrid.MinWidth  = Math.Max(areaWidth,  160);
        PreviewGrid.MinHeight = Math.Max(areaHeight, 80);
        PreviewGrid.MaxHeight = areaHeight;

        // Render each text block as a clickable Grid
        foreach (var config in _currentTextBlocks)
        {
            var blockContainer = CreateTextBlockElement(config);
            TextStackPanel.Children.Add(blockContainer);
        }
    }

    private Grid CreateTextBlockElement(TextBlockConfig config)
    {
        var container = new Grid
        {
            Height     = LineHeight,
            Margin     = new Thickness(0, LineSpacing / 2, 0, LineSpacing / 2),
            Tag        = config,
            Background = new SolidColorBrush(Colors.Transparent) // enable hit testing
        };

        // Selection highlight
        if (config == _selectedBlock)
        {
            container.Background = new SolidColorBrush
            (
                ColorHelper.FromArgb(40, 255, 255, 255)
            );
        }

        // Click handler
        container.PointerPressed += BlockContainer_PointerPressed;

        var fgEntry = _dataService!.GetEntry(config.ForegroundColorRow);
        var fgColor = GetThemeColor(fgEntry);
        var fgBrush = new SolidColorBrush
        (
            ColorHelper.FromArgb(fgColor.A, fgColor.R, fgColor.G, fgColor.B)
        );

        // Render stroke if enabled
        if (config.StrokeEnabled)
        {
            var strokeEntry = _dataService.GetEntry(config.StrokeColorRow);
            var strokeColor = GetThemeColor(strokeEntry);
            var strokeBrush = new SolidColorBrush
            (
                ColorHelper.FromArgb(strokeColor.A, strokeColor.R, strokeColor.G, strokeColor.B)
            );

            int[] offsets = [-1, 0, 1];

            foreach (var dx in offsets)
            {
                foreach (var dy in offsets)
                {
                    if (dx == 0 && dy == 0) continue;
                    container.Children.Add
                    (
                        new TextBlock
                        {
                            Text                = config.PreviewText,
                            FontSize            = FontSize,
                            Foreground          = strokeBrush,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment   = VerticalAlignment.Center,
                            Margin              = new Thickness(dx, dy, -dx, -dy),
                            IsHitTestVisible    = false
                        }
                    );
                }
            }
        }

        // Foreground text
        container.Children.Add
        (
            new TextBlock
            {
                Text                = config.PreviewText,
                FontSize            = FontSize,
                Foreground          = fgBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                IsHitTestVisible    = false
            }
        );

        return container;
    }

    private void BlockContainer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.Tag is TextBlockConfig config)
        {
            _selectedBlock = config;
            TextBlockClicked?.Invoke(this, config);
            RenderContent();
            e.Handled = true; // Prevent bubbling to deselect handler
        }
    }

    private Color GetThemeColor(UIColorEntry? entry)
    {
        if (entry == null) return Color.FromArgb(255, 255, 255, 255);
        return entry.ThemeColors.TryGetValue(_themeName, out var color)
                   ? color
                   : Color.FromArgb(255, 255, 255, 255);
    }

    private static double EstimateTextWidth(string text, double fontSize)
    {
        double width                   = 0;
        foreach (var ch in text) width += ch > 0x7F ? fontSize : fontSize * 0.6;
        return width;
    }
}
