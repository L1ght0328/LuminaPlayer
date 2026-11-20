using System.IO;
using System.Text.Json;

namespace LuminaPlayer;

public static class ConfigStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LuminaPlayer");
    private static readonly string FilePath = Path.Combine(Folder, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath)) ?? new AppConfig();
        }
        catch { /* Config corrompida: inicia com padrões sem impedir abertura. */ }
        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}
