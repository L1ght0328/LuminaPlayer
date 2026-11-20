using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using TagLib;

namespace LuminaPlayer;

public sealed class LibraryService(AppConfig config)
{
    public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".wav", ".aiff", ".aif", ".wma", ".m4a", ".m4b", ".aac", ".flac" };

    private List<string>? cache;
    private List<TrackEntry>? entryCache;
    private static readonly (Color A, Color B)[] CoverPalette =
    {
        (Color.FromRgb(112, 35, 135), Color.FromRgb(22, 32, 83)),
        (Color.FromRgb(25, 113, 130), Color.FromRgb(21, 35, 77)),
        (Color.FromRgb(150, 40, 115), Color.FromRgb(36, 22, 75)),
        (Color.FromRgb(182, 70, 75), Color.FromRgb(49, 22, 67)),
        (Color.FromRgb(48, 83, 145), Color.FromRgb(28, 27, 77)),
        (Color.FromRgb(166, 87, 45), Color.FromRgb(56, 35, 86))
    };

    public List<string> Scan(string root)
    {
        var results = EnumerateAudioFiles(root);
        if (!config.Roots.Contains(root, StringComparer.OrdinalIgnoreCase)) config.Roots.Add(root);
        cache = config.Roots.Where(Directory.Exists).SelectMany(EnumerateAudioFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
        entryCache = null;
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

    public List<TrackEntry> AllEntries()
    {
        if (entryCache is not null) return entryCache;
        entryCache = AllTracks().Select(CreateEntry).ToList();
        return entryCache;
    }

    public int CountForRoot(string root) => AllTracks().Count(p => p.StartsWith(root, StringComparison.OrdinalIgnoreCase));

    private static TrackEntry CreateEntry(string path)
    {
        string title = Path.GetFileNameWithoutExtension(path);
        string folder = Directory.GetParent(path)?.Name ?? "";
        string parent = Directory.GetParent(path)?.Parent?.Name ?? "";
        string artist = "Artista desconhecido";
        string album = string.IsNullOrWhiteSpace(folder) ? "Álbum desconhecido" : folder;
        string genre = "Sem gênero";
        string? cover = FindSidecarCover(Path.GetDirectoryName(path)!);

        try
        {
            using var media = TagLib.File.Create(path);
            if (!string.IsNullOrWhiteSpace(media.Tag.Title)) title = media.Tag.Title;
            if (!string.IsNullOrWhiteSpace(media.Tag.FirstPerformer)) artist = media.Tag.FirstPerformer;
            else if (!string.IsNullOrWhiteSpace(parent)) artist = parent;
            if (!string.IsNullOrWhiteSpace(media.Tag.Album)) album = media.Tag.Album;
            if (!string.IsNullOrWhiteSpace(media.Tag.FirstGenre)) genre = media.Tag.FirstGenre;
            if (cover is null && media.Tag.Pictures?.FirstOrDefault() is IPicture picture)
                cover = SaveEmbeddedCover(path, picture.Data.Data);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(parent)) artist = parent;
        }

        int paletteIndex = (int)(uint)StringComparer.OrdinalIgnoreCase.GetHashCode(album + artist) % CoverPalette.Length;
        if (paletteIndex < 0) paletteIndex = -paletteIndex;
        var colors = CoverPalette[paletteIndex];
        var brush = new LinearGradientBrush(colors.A, colors.B, 35);
        brush.Freeze();
        return new TrackEntry { Title = title, FileName = Path.GetFileName(path), Path = path, Artist = artist, Album = album, Genre = genre, CoverPath = cover, CoverBrush = brush };
    }

    private static string? FindSidecarCover(string folder)
    {
        foreach (string name in new[] { "cover.jpg", "cover.png", "folder.jpg", "album.jpg", "front.jpg" })
        {
            string candidate = System.IO.Path.Combine(folder, name);
            if (System.IO.File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static string? SaveEmbeddedCover(string audioPath, byte[] bytes)
    {
        if (bytes.Length == 0) return null;
        string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LuminaPlayer", "Covers");
        Directory.CreateDirectory(folder);
        string key = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(audioPath))).Substring(0, 24);
        string extension = bytes.Length > 3 && bytes[0] == 0x89 && bytes[1] == 0x50 ? ".png" : ".jpg";
        string target = System.IO.Path.Combine(folder, key + extension);
        if (!System.IO.File.Exists(target)) System.IO.File.WriteAllBytes(target, bytes);
        return target;
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
                    if (Extensions.Contains(System.IO.Path.GetExtension(file))) results.Add(System.IO.Path.GetFullPath(file));
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    try
                    {
                        var attributes = Directory.GetAttributes(directory);
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
