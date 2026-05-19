using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XIVUIColorPreviewer.Models;
using XIVUIColorPreviewer.Services;

namespace XIVUIColorPreviewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UIColorDataService _colorDataService = new();
    private readonly ColorSchemeService _schemeService = new();

    [ObservableProperty]
    private string _previewText = "测试文本";

    [ObservableProperty]
    private int _foregroundColorRow;

    [ObservableProperty]
    private bool _strokeEnabled;

    [ObservableProperty]
    private int _strokeColorRow;

    [ObservableProperty]
    private string _newSchemeName = string.Empty;

    [ObservableProperty]
    private ColorScheme? _selectedScheme;

    [ObservableProperty]
    private bool _isLoading = true;

    public ObservableCollection<TextBlockConfig> TextBlocks { get; } = [];
    public ObservableCollection<ColorScheme> Schemes { get; } = [];

    public UIColorDataService ColorDataService => _colorDataService;
    public List<string> ThemeNames => _colorDataService.ThemeNames;

    public event Action? PreviewChanged;

    public MainViewModel()
    {
        TextBlocks.CollectionChanged += TextBlocks_CollectionChanged;
    }

    private void TextBlocks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TextBlockConfig item in e.NewItems)
            {
                item.PropertyChanged += TextBlockConfig_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (TextBlockConfig item in e.OldItems)
            {
                item.PropertyChanged -= TextBlockConfig_PropertyChanged;
            }
        }
    }

    private void TextBlockConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyPreviewChanged();
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        await _colorDataService.LoadAsync();
        await _schemeService.LoadAsync();

        foreach (var scheme in _schemeService.Schemes)
        {
            Schemes.Add(scheme);
        }

        // Set defaults
        if (_colorDataService.RowNumbers.Count > 0)
        {
            ForegroundColorRow = _colorDataService.RowNumbers[0];
            StrokeColorRow = _colorDataService.RowNumbers[0];
        }

        IsLoading = false;
    }

    [RelayCommand]
    private void AddTextBlock()
    {
        var config = new TextBlockConfig
        {
            PreviewText = PreviewText,
            ForegroundColorRow = ForegroundColorRow,
            StrokeEnabled = StrokeEnabled,
            StrokeColorRow = StrokeColorRow
        };
        TextBlocks.Add(config);
        NotifyPreviewChanged();
    }

    [RelayCommand]
    private void RemoveTextBlock(TextBlockConfig? config)
    {
        if (config != null)
        {
            TextBlocks.Remove(config);
            NotifyPreviewChanged();
        }
    }

    [RelayCommand]
    private void MoveTextBlockUp(TextBlockConfig? config)
    {
        if (config == null) return;
        var idx = TextBlocks.IndexOf(config);
        if (idx > 0)
        {
            TextBlocks.Move(idx, idx - 1);
            NotifyPreviewChanged();
        }
    }

    [RelayCommand]
    private void MoveTextBlockDown(TextBlockConfig? config)
    {
        if (config == null) return;
        var idx = TextBlocks.IndexOf(config);
        if (idx < TextBlocks.Count - 1)
        {
            TextBlocks.Move(idx, idx + 1);
            NotifyPreviewChanged();
        }
    }

    [RelayCommand]
    private async Task SaveSchemeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSchemeName)) return;

        var scheme = new ColorScheme
        {
            Name = NewSchemeName.Trim(),
            TextBlocks = TextBlocks.Select(tb => new TextBlockConfigData
            {
                PreviewText = tb.PreviewText,
                ForegroundColorRow = tb.ForegroundColorRow,
                StrokeEnabled = tb.StrokeEnabled,
                StrokeColorRow = tb.StrokeColorRow
            }).ToList()
        };

        await _schemeService.AddSchemeAsync(scheme);

        // Refresh schemes list
        Schemes.Clear();
        foreach (var s in _schemeService.Schemes)
        {
            Schemes.Add(s);
        }

        NewSchemeName = string.Empty;
    }

    [RelayCommand]
    private void LoadScheme(ColorScheme? scheme)
    {
        if (scheme == null) return;

        SelectedScheme = scheme;
        TextBlocks.Clear();
        foreach (var data in scheme.TextBlocks)
        {
            TextBlocks.Add(new TextBlockConfig
            {
                PreviewText = data.PreviewText,
                ForegroundColorRow = data.ForegroundColorRow,
                StrokeEnabled = data.StrokeEnabled,
                StrokeColorRow = data.StrokeColorRow
            });
        }
        NotifyPreviewChanged();
    }

    [RelayCommand]
    private async Task DeleteSchemeAsync(ColorScheme? scheme)
    {
        if (scheme == null) return;

        await _schemeService.DeleteSchemeAsync(scheme.Name);
        Schemes.Remove(scheme);
        if (SelectedScheme == scheme)
        {
            SelectedScheme = null;
        }
    }

    public async Task RenameSchemeAsync(ColorScheme scheme, string newName)
    {
        await _schemeService.RenameSchemeAsync(scheme.Name, newName);
        scheme.Name = newName;

        // Refresh list to reflect name change
        var idx = Schemes.IndexOf(scheme);
        if (idx >= 0)
        {
            Schemes.RemoveAt(idx);
            Schemes.Insert(idx, scheme);
        }
    }

    public void NotifyPreviewChanged()
    {
        PreviewChanged?.Invoke();
    }
}
