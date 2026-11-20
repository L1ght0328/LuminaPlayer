using System.Windows.Media;

namespace LuminaPlayer;

public sealed class PlaylistGroup
{
    public string Name { get; set; } = "Minha música";
    public Dictionary<string, List<string>> Playlists { get; set; } = new();
}

public sealed class AppConfig
{
    public List<string> Roots { get; set; } = new();
    public List<PlaylistGroup> Groups { get; set; } = new() { new PlaylistGroup() };
    public string Theme { get; set; } = "Dark";
    public string Accent { get; set; } = "#B65CFF";
    public string BackgroundImagePath { get; set; } = "";
    public bool Compact { get; set; }
    public bool ShowLibrary { get; set; } = true;
    public bool ShowPlaylists { get; set; } = true;
    public bool ShowEqualizer { get; set; } = true;
    public int Volume { get; set; } = 75;
    public bool Muted { get; set; }
    public string LoopMode { get; set; } = "Off";
    public Dictionary<string, double> EqBands { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> EqPresets { get; set; } = new();
}

public sealed class TrackEntry
{
    public string Title { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Path { get; init; } = "";
    public string Artist { get; init; } = "Artista desconhecido";
    public string Album { get; init; } = "Álbum desconhecido";
    public string Genre { get; init; } = "Sem gênero";
    public string? CoverPath { get; init; }
    public Brush CoverBrush { get; init; } = new SolidColorBrush(Color.FromRgb(74, 42, 105));
}

public sealed record FolderTileEntry(string Name, string Path, string CountLabel);
