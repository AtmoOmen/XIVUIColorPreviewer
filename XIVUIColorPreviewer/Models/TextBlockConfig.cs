using CommunityToolkit.Mvvm.ComponentModel;

namespace XIVUIColorPreviewer.Models;

/// <summary>
/// Represents a single text block in the preview area.
/// </summary>
public partial class TextBlockConfig : ObservableObject
{
    [ObservableProperty]
    private string _previewText = "测试文本";

    [ObservableProperty]
    private int _foregroundColorRow;

    [ObservableProperty]
    private bool _strokeEnabled;

    [ObservableProperty]
    private int _strokeColorRow;
}
