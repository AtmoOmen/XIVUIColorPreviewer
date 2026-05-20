using System.Text.Json;
using XIVUIColorPreviewer.Models;

namespace XIVUIColorPreviewer.Services;

/// <summary>
///     Manages persistence of color schemes to a local JSON file.
/// </summary>
public sealed class ColorSchemeService
{
    private static readonly string s_schemesFilePath = Path.Combine
    (
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XIVUIColorPreviewer",
        "schemes.json"
    );

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    public List<ColorScheme> Schemes { get; private set; } = [];

    public async Task LoadAsync()
    {
        if (!File.Exists(s_schemesFilePath))
        {
            Schemes = [];
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(s_schemesFilePath);
            Schemes = JsonSerializer.Deserialize<List<ColorScheme>>(json, s_jsonOptions) ?? [];
        }
        catch
        {
            Schemes = [];
        }
    }

    public async Task SaveAsync()
    {
        var dir = Path.GetDirectoryName(s_schemesFilePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Schemes, s_jsonOptions);
        await File.WriteAllTextAsync(s_schemesFilePath, json);
    }

    public async Task AddSchemeAsync(ColorScheme scheme)
    {
        // Replace if same name exists
        Schemes.RemoveAll(s => s.Name == scheme.Name);
        Schemes.Add(scheme);
        await SaveAsync();
    }

    public async Task DeleteSchemeAsync(string name)
    {
        Schemes.RemoveAll(s => s.Name == name);
        await SaveAsync();
    }

    public async Task RenameSchemeAsync(string oldName, string newName)
    {
        var scheme = Schemes.FirstOrDefault(s => s.Name == oldName);

        if (scheme != null)
        {
            scheme.Name = newName;
            await SaveAsync();
        }
    }
}
