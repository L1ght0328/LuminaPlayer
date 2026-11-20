using System.IO;

namespace LuminaPlayer;

public sealed class LibraryService(AppConfig config)
{
    public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".wav", ".aiff", ".aif", ".wma", ".m4a", ".aac", ".flac", ".ogg", ".opus" };

    private List<string>? cache;

    public List<string> Scan(string root)
    {
        var results = EnumerateAudioFiles(root);
        if (!config.Roots.Contains(root, StringComparer.OrdinalIgnoreCase)) config.Roots.Add(root);
        cache = config.Roots.Where(Directory.Exists).SelectMany(EnumerateAudioFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
        ConfigStore.Save(config);
        return results;
    }

    public List<string> AllTracks()
    {
        if (cache is null)
            cache = config.Roots.Where(Directory.Exists).SelectMany(EnumerateAudioFiles)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
        return cache;
    }

    private static List<string> EnumerateAudioFiles(string root)
    {
        var results = new List<string>();
        if (!Directory.Exists(root)) return results;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                    if (Extensions.Contains(Path.GetExtension(file))) results.Add(Path.GetFullPath(file));
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    try
                    {
                        var attributes = File.GetAttributes(directory);
                        if ((attributes & FileAttributes.ReparsePoint) == 0) pending.Push(directory);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        return results;
    }
}
