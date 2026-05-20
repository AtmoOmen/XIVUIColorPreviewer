using Windows.UI;
using Microsoft.Extensions.Logging;
using XIVUIColorPreviewer.Models;

namespace XIVUIColorPreviewer.Services;

/// <summary>
///     Loads and provides access to the UIColor data table.
///     Attempts online fetch first, falls back to bundled CSV.
/// </summary>
public sealed class UIColorDataService
{
    private const string OnlineUrl =
        "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/en/UIColor.csv";

    private static readonly ILogger<UIColorDataService> s_logger = Log.For<UIColorDataService>();
    private static readonly HttpClient s_httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    public List<string>       ThemeNames { get; private set; } = [];
    public List<UIColorEntry> Entries    { get; private set; } = [];
    public List<int>          RowNumbers { get; private set; } = [];

    public async Task LoadAsync()
    {
        var csv = await FetchCsvAsync();
        ParseCsv(csv);
        s_logger.LogInformation("Loaded {EntryCount} color entries, {ThemeCount} themes", Entries.Count, ThemeNames.Count);
    }

    private async Task<string> FetchCsvAsync()
    {
        try
        {
            s_logger.LogDebug("Fetching CSV from {Url}", OnlineUrl);
            var result = await s_httpClient.GetStringAsync(OnlineUrl);
            s_logger.LogInformation("Online CSV fetched successfully ({Length} chars)", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            s_logger.LogWarning(ex, "Online fetch failed, falling back to local asset");
            // Fallback to bundled asset
            var localPath = Path.Combine
            (
                AppContext.BaseDirectory,
                "Assets",
                "Game",
                "Sheets",
                "UIColor.csv"
            );
            s_logger.LogDebug("Loading local CSV from {Path}", localPath);
            return await File.ReadAllTextAsync(localPath);
        }
    }

    private void ParseCsv(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) return;

        // Header: #,Dark,Light,ClassicFF,...
        var headers = lines[0].Split(',');
        ThemeNames = headers[1..].ToList();

        var entries    = new List<UIColorEntry>();
        var rowNumbers = new List<int>();

        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 2) continue;

            if (!int.TryParse(parts[0], out var rowNum)) continue;

            var themeColors = new Dictionary<string, Color>();
            for (var t = 0; t < ThemeNames.Count && t + 1 < parts.Length; t++)
                if (uint.TryParse(parts[t + 1], out var rawValue)) themeColors[ThemeNames[t]] = ConvertToColor(rawValue);
                else themeColors[ThemeNames[t]]                                               = Color.FromArgb(0, 0, 0, 0);

            entries.Add(new UIColorEntry { RowNumber = rowNum, ThemeColors = themeColors });
            rowNumbers.Add(rowNum);
        }

        Entries    = entries;
        RowNumbers = rowNumbers;
    }

    /// <summary>
    ///     Converts a raw uint32 value from the CSV to a Color.
    ///     The raw value is stored as RGBA in big-endian byte order.
    /// </summary>
    private static Color ConvertToColor(uint rawValue)
    {
        var r = (byte)((rawValue >> 24) & 0xFF);
        var g = (byte)((rawValue >> 16) & 0xFF);
        var b = (byte)((rawValue >> 8)  & 0xFF);
        var a = (byte)(rawValue         & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }

    public UIColorEntry? GetEntry(int rowNumber) =>
        Entries.FirstOrDefault(e => e.RowNumber == rowNumber);
}
