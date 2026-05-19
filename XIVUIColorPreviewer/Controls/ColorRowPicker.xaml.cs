using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using XIVUIColorPreviewer.Models;
using XIVUIColorPreviewer.Services;

namespace XIVUIColorPreviewer.Controls;

public sealed partial class ColorRowPicker : UserControl
{
    private UIColorDataService? _dataService;
    private bool _suppressSelectionChanged;
    private List<int> _recommendations = [];

    public static readonly DependencyProperty SelectedRowNumberProperty =
        DependencyProperty.Register(
            nameof(SelectedRowNumber), typeof(int), typeof(ColorRowPicker),
            new PropertyMetadata(0, OnSelectedRowNumberChanged));

    public int SelectedRowNumber
    {
        get => (int)GetValue(SelectedRowNumberProperty);
        set => SetValue(SelectedRowNumberProperty, value);
    }

    public event EventHandler<int>? RowNumberChanged;

    /// <summary>
    /// Fired when the dropdown is opened. Parent can use this to trigger recommendation refresh.
    /// </summary>
    public event EventHandler? DropDownOpened;

    public ColorRowPicker()
    {
        InitializeComponent();
        RowComboBox.DropDownOpened += RowComboBox_DropDownOpened;
    }

    public void Initialize(UIColorDataService dataService)
    {
        _dataService = dataService;
        PopulateComboBox();
    }

    /// <summary>
    /// Sets recommended row numbers to display at the top of the dropdown,
    /// separated from the full list by a divider.
    /// </summary>
    public void SetRecommendations(List<int> recommendedRows)
    {
        _recommendations = recommendedRows;
        RebuildDropdown();
    }

    /// <summary>
    /// Clears any recommendations, showing only the standard list.
    /// </summary>
    public void ClearRecommendations()
    {
        if (_recommendations.Count == 0) return;
        _recommendations = [];
        RebuildDropdown();
    }

    private void PopulateComboBox()
    {
        RebuildDropdown();
    }

    private void RebuildDropdown()
    {
        if (_dataService == null) return;

        _suppressSelectionChanged = true;
        RowComboBox.Items.Clear();

        // Add recommended items first
        if (_recommendations.Count > 0)
        {
            foreach (var rowNum in _recommendations)
            {
                var entry = _dataService.GetEntry(rowNum);
                if (entry == null) continue;

                var item = CreateComboBoxItem(entry, isRecommended: true);
                RowComboBox.Items.Add(item);
            }

            // Add separator
            var separator = new ComboBoxItem
            {
                Content = new MenuFlyoutSeparator { Margin = new Thickness(-12, 0, -12, 0) },
                IsEnabled = false,
                IsHitTestVisible = false,
                Tag = -1 // sentinel value
            };
            RowComboBox.Items.Add(separator);
        }

        // Add all items
        foreach (var entry in _dataService.Entries)
        {
            var item = CreateComboBoxItem(entry, isRecommended: false);
            RowComboBox.Items.Add(item);
        }

        _suppressSelectionChanged = false;
        SyncSelection();
    }

    private ComboBoxItem CreateComboBoxItem(UIColorEntry entry, bool isRecommended)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        // Recommendation indicator
        if (isRecommended)
        {
            var star = new TextBlock
            {
                Text = "★", // ★
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 50)),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(star);
        }

        // Row number text
        var rowText = new TextBlock
        {
            Text = entry.RowNumber.ToString(),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 36
        };
        panel.Children.Add(rowText);

        // Color swatches for each theme
        foreach (var themeName in _dataService!.ThemeNames)
        {
            if (entry.ThemeColors.TryGetValue(themeName, out var color))
            {
                var swatch = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(
                        Microsoft.UI.ColorHelper.FromArgb(color.A, color.R, color.G, color.B)),
                    BorderBrush = new SolidColorBrush(
                        Microsoft.UI.ColorHelper.FromArgb(80, 128, 128, 128)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(1, 0, 1, 0)
                };
                ToolTipService.SetToolTip(swatch, themeName);
                panel.Children.Add(swatch);
            }
        }

        return new ComboBoxItem { Content = panel, Tag = entry.RowNumber };
    }

    private void SyncSelection()
    {
        _suppressSelectionChanged = true;
        for (int i = 0; i < RowComboBox.Items.Count; i++)
        {
            if (RowComboBox.Items[i] is ComboBoxItem item && item.Tag is int rowNum && rowNum == SelectedRowNumber)
            {
                RowComboBox.SelectedIndex = i;
                _suppressSelectionChanged = false;
                return;
            }
        }
        _suppressSelectionChanged = false;
    }

    private static void OnSelectedRowNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorRowPicker picker)
        {
            picker.SyncSelection();
        }
    }

    private void RowComboBox_DropDownOpened(object? sender, object e)
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    private void RowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;
        if (RowComboBox.SelectedItem is ComboBoxItem item && item.Tag is int rowNum && rowNum >= 0)
        {
            SelectedRowNumber = rowNum;
            RowNumberChanged?.Invoke(this, rowNum);
        }
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataService == null || _dataService.RowNumbers.Count == 0) return;
        var idx = _dataService.RowNumbers.IndexOf(SelectedRowNumber);
        if (idx > 0)
        {
            SelectedRowNumber = _dataService.RowNumbers[idx - 1];
            RowNumberChanged?.Invoke(this, SelectedRowNumber);
        }
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataService == null || _dataService.RowNumbers.Count == 0) return;
        var idx = _dataService.RowNumbers.IndexOf(SelectedRowNumber);
        if (idx < _dataService.RowNumbers.Count - 1)
        {
            SelectedRowNumber = _dataService.RowNumbers[idx + 1];
            RowNumberChanged?.Invoke(this, SelectedRowNumber);
        }
    }
}
