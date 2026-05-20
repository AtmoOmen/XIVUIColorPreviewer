using CommunityToolkit.Mvvm.ComponentModel;

namespace XIVUIColorPreviewer.Models;

/// <summary>
///     Represents a single text block in the preview area.
/// </summary>
public partial class TextBlockConfig : ObservableObject
{
    [ObservableProperty]
    public partial string PreviewText { get; set; }

    [ObservableProperty]
    public partial int ForegroundColorRow { get; set; }

    [ObservableProperty]
    public partial bool StrokeEnabled { get; set; }

    [ObservableProperty]
    public partial int StrokeColorRow { get; set; }

    public TextBlockConfig()
    {
        PreviewText = "测试文本";
    }
}
