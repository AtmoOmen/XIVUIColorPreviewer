using Windows.UI;

namespace XIVUIColorPreviewer.Models;

/// <summary>
/// Represents a single row in the UIColor table.
/// Each row has a row number and a color value per theme.
/// </summary>
public sealed class UIColorEntry
{
    public int RowNumber { get; init; }

    /// <summary>
    /// Theme name -> ARGB Color value (after endianness conversion).
    /// </summary>
    public Dictionary<string, Color> ThemeColors { get; init; } = [];
}
