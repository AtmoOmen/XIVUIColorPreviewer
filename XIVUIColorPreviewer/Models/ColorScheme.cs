namespace XIVUIColorPreviewer.Models;

/// <summary>
/// A named color scheme that stores a list of text block configurations.
/// </summary>
public sealed class ColorScheme
{
    public string Name { get; set; } = string.Empty;
    public List<TextBlockConfigData> TextBlocks { get; set; } = [];
}

/// <summary>
/// Serializable data for a single text block (no ObservableObject dependency).
/// </summary>
public sealed class TextBlockConfigData
{
    public string PreviewText { get; set; } = "测试文本";
    public int ForegroundColorRow { get; set; }
    public bool StrokeEnabled { get; set; }
    public int StrokeColorRow { get; set; }
}
