using Windows.UI;
using XIVUIColorPreviewer.Models;

namespace XIVUIColorPreviewer.Services;

/// <summary>
///     Recommends stroke colors that pair well with a given foreground color
///     across all themes simultaneously.
/// </summary>
public sealed class ColorRecommendationService
{
    private readonly UIColorDataService _dataService;

    // Minimum contrast ratio (simplified) between foreground and stroke across all themes
    private const double MinLuminanceDiff = 0.15;

    // Maximum number of recommendations
    private const int MaxRecommendations = 8;

    public ColorRecommendationService(UIColorDataService dataService) =>
        _dataService = dataService;

    /// <summary>
    ///     Returns row numbers of colors that make good stroke/outline colors
    ///     for the given foreground color row, evaluated across all themes.
    /// </summary>
    public List<int> GetStrokeRecommendations(int foregroundRowNumber)
    {
        var fgEntry = _dataService.GetEntry(foregroundRowNumber);
        if (fgEntry == null) return [];

        var candidates = new List<(int RowNumber, double Score)>();

        foreach (var entry in _dataService.Entries)
        {
            if (entry.RowNumber == foregroundRowNumber) continue;

            var score = EvaluateStrokeFitness(fgEntry, entry);
            if (score > 0) candidates.Add((entry.RowNumber, score));
        }

        return candidates
               .OrderByDescending(c => c.Score)
               .Take(MaxRecommendations)
               .Select(c => c.RowNumber)
               .ToList();
    }

    /// <summary>
    ///     Evaluates how well a candidate stroke color pairs with the foreground
    ///     across all themes. Returns 0 if unsuitable, higher = better.
    /// </summary>
    private double EvaluateStrokeFitness(UIColorEntry foreground, UIColorEntry candidate)
    {
        double totalScore  = 0;
        var    validThemes = 0;

        foreach (var theme in _dataService.ThemeNames)
        {
            if (!foreground.ThemeColors.TryGetValue(theme, out var fgColor)) continue;
            if (!candidate.ThemeColors.TryGetValue(theme, out var strokeColor)) continue;

            // Skip if either color is fully transparent
            if (fgColor.A < 20 || strokeColor.A < 20) continue;

            var themeScore = ScoreThemePair(fgColor, strokeColor);

            if (themeScore < 0)
            {
                // Fails in this theme — disqualify entirely
                return 0;
            }

            totalScore += themeScore;
            validThemes++;
        }

        if (validThemes == 0) return 0;

        // Average score across themes
        return totalScore / validThemes;
    }

    /// <summary>
    ///     Scores a foreground/stroke pair for a single theme.
    ///     Returns negative if the pair is unsuitable, positive otherwise.
    /// </summary>
    private static double ScoreThemePair(Color fg, Color stroke)
    {
        var fgLum     = GetRelativeLuminance(fg);
        var strokeLum = GetRelativeLuminance(stroke);

        // Luminance difference — stroke should be clearly distinguishable
        var lumDiff = Math.Abs(fgLum - strokeLum);
        if (lumDiff < MinLuminanceDiff) return -1; // Too similar, won't be visible as outline

        // Prefer strokes that are darker than the foreground (common pattern)
        // but also accept lighter strokes for dark foregrounds
        var darkerBonus = strokeLum < fgLum ? 0.3 : 0.0;

        // Hue relationship scoring
        var fgHue                  = GetHue(fg);
        var strokeHue              = GetHue(stroke);
        var hueDiff                = Math.Abs(fgHue - strokeHue);
        if (hueDiff > 180) hueDiff = 360 - hueDiff;

        // Analogous colors (similar hue, different luminance) work great as strokes
        double hueScore;

        if (hueDiff < 30)
        {
            // Same hue family — excellent for stroke if luminance differs
            hueScore = 1.0;
        }
        else if (hueDiff < 60)
        {
            // Analogous — good
            hueScore = 0.7;
        }
        else if (hueDiff > 150)
        {
            // Complementary — can work but less common for strokes
            hueScore = 0.4;
        }
        else
        {
            // Other relationships
            hueScore = 0.5;
        }

        // Neutral strokes (low saturation) are universally good
        var strokeSat    = GetSaturation(stroke);
        var neutralBonus = strokeSat < 0.15 ? 0.5 : 0.0;

        // Contrast ratio bonus (higher contrast = more readable)
        var contrastBonus = Math.Min(lumDiff * 2.0, 1.0);

        return hueScore + darkerBonus + neutralBonus + contrastBonus;
    }

    private static double GetRelativeLuminance(Color c)
    {
        var r = SrgbToLinear(c.R / 255.0);
        var g = SrgbToLinear(c.G / 255.0);
        var b = SrgbToLinear(c.B / 255.0);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static double SrgbToLinear(double v) =>
        v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

    private static double GetHue(Color c)
    {
        double r     = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var    max   = Math.Max(r, Math.Max(g, b));
        var    min   = Math.Min(r, Math.Min(g, b));
        var    delta = max - min;

        if (delta < 0.001) return 0;

        double hue;
        if (max == r)
            hue = 60 * ((g - b) / delta % 6);
        else if (max == g)
            hue = 60 * (((b - r) / delta) + 2);
        else
            hue = 60 * (((r - g) / delta) + 4);

        if (hue < 0) hue += 360;
        return hue;
    }

    private static double GetSaturation(Color c)
    {
        double r   = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var    max = Math.Max(r, Math.Max(g, b));
        var    min = Math.Min(r, Math.Min(g, b));
        if (max < 0.001) return 0;
        return (max - min) / max;
    }
}
