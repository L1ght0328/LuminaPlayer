using System.IO;

namespace LuminaPlayer;

public sealed class LibraryService(AppConfig config)
{
    // Media Foundation/NAudio decodificam formatos reconhecidos pelo Windows.
    public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".wav", ".aiff", ".aif", ".wma", ".m4a", ".aac", ".flac", ".ogg", ".opus" };

    public List<string> Scan(string root)
    {
        var results = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                if (Extensions.Contains(Path.GetExtension(file))) results.Add(Path.GetFullPath(file));
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        if (!config.Roots.Contains(root, StringComparer.OrdinalIgnoreCase)) config.Roots.Add(root);
        ConfigStore.Save(config);
        return results.OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public List<string> AllTracks()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in config.Roots)
            if (Directory.Exists(root))
                foreach (var path in ScanWithoutPersist(root)) set.Add(path);
        return set.OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static IEnumerable<string> ScanWithoutPersist(string root)
    {
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories); }
        catch { return Array.Empty<string>(); }
        return files.Where(f => Extensions.Contains(Path.GetExtension(f))).Select(Path.GetFullPath);
    }
}
