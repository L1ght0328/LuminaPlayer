using Microsoft.Win32;
using NAudio.Wave;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace LuminaPlayer;

public partial class MainWindow : Window
{
    private readonly AppConfig config = ConfigStore.Load();
    private readonly LibraryService library;
    private readonly AudioService audio = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private List<TrackEntry> visibleTracks = new();
    private const int PageSize = 60;
    private int currentPage;
    private List<string> queue = new();
    private int queueIndex = -1;
    private bool dragging;
    private bool syncingProgress;
    private bool muted;
    private string loopMode = "Off";
    private readonly Slider[] eqSliders = new Slider[10];
    private readonly double[] frequencies = {31,62,125,250,500,1000,2000,4000,8000,16000};
    private bool loadingPresets;
    private string currentView = "All";
    private string? selectedRoot;
    private TrackEntry? currentTrack;

    public MainWindow()
    {
        InitializeComponent();
        library = new LibraryService(config);
        muted = config.Muted; loopMode = config.LoopMode;
        timer.Tick += (_, _) => UpdatePlaybackUi();
        timer.Start();
        audio.PlaybackEnded += () => Dispatcher.BeginInvoke(new Action(AdvanceAutomatic));
        BuildEq();
        ApplyPreferences();
        RootsList.ItemsSource = config.Roots.ToList();
        FolderTiles.ItemsSource = config.Roots.Select(root => new FolderTileEntry(System.IO.Path.GetFileName(root.TrimEnd(System.IO.Path.DirectorySeparatorChar)), root, "Pasta local")).ToList();
        RefreshCollections();
        StatusText.Text = config.Roots.Count == 0 ? "Adicione uma pasta para começar" : "Preparando sua biblioteca local…";
        Loaded += async (_, _) => await LoadInitialLibraryAsync();
        Closed += (_, _) => { timer.Stop(); audio.Dispose(); };
        SearchBox.ToolTip = "Pesquisar por título ou nome do arquivo";
    }

    private void BuildEq()
    {
        for (int i = 0; i < 10; i++)
        {
            var panel = new StackPanel { Margin = new Thickness(1, 0, 1, 0), Width = 25 };
            panel.Children.Add(new TextBlock { Text = frequencies[i] >= 1000 ? $"{frequencies[i] / 1000:0.#}k" : $"{frequencies[i]:0}", Foreground = Brushes.Gainsboro, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, FontSize = 8 });
            var slider = new Slider { Minimum = -12, Maximum = 12, Value = 0, Orientation = Orientation.Vertical, Height = 78, Width = 18, ToolTip = "Ganho em dB" };
            int band = i;
            slider.ValueChanged += (_, _) =>
            {
                audio.SetBand(band, slider.Value);
                config.EqBands[band.ToString()] = slider.Value;
            };
            slider.PreviewMouseLeftButtonUp += (_, _) => ConfigStore.Save(config);
            panel.Children.Add(slider);
            eqSliders[i] = slider;
            EqPanel.Children.Add(panel);
            if (config.EqBands.TryGetValue(i.ToString(), out double value)) slider.Value = value;
        }
        foreach (string presetName in config.EqPresets.Keys.OrderBy(k => k, StringComparer.CurrentCultureIgnoreCase))
            if (!PresetCombo.Items.Cast<object>().OfType<ComboBoxItem>().Any(i => string.Equals(i.Content?.ToString(), presetName, StringComparison.OrdinalIgnoreCase)))
                PresetCombo.Items.Add(new ComboBoxItem { Content = presetName });
    }

    private void ApplyPreferences()
    {
        muted = config.Muted;
        loopMode = config.LoopMode;
        LoopButton.Content = LoopLabel();
        VolumeSlider.Value = config.Volume;
        MuteButton.Content = muted ? "◖×" : "◖))";
        EqualizerCard.Visibility = config.ShowEqualizer ? Visibility.Visible : Visibility.Collapsed;
        ApplyTheme();
    }

    private void SetBrush(string key, string color) => Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

    private void ApplyTheme()
    {
        bool light = config.Theme == "Light";
        SetBrush("AppBg", light ? "#FFF2EEF8" : "#FF100D1B");
        SetBrush("PanelBg", light ? "#EFFFFFFF" : "#E6191428");
        SetBrush("SurfaceBg", light ? "#E6FFFFFF" : "#E6241D38");
        SetBrush("RaisedBg", light ? "#FFF0E9FA" : "#FF30234A");
        SetBrush("PrimaryText", light ? "#FF241A31" : "#FFF8F5FF");
        SetBrush("SecondaryText", light ? "#FF756A83" : "#FFBDB3D0");
        SetBrush("Line", light ? "#FFDCD2E8" : "#FF49365F");
        SetBrush("Accent", string.IsNullOrWhiteSpace(config.Accent) ? "#B65CFF" : config.Accent);
        SetBrush("AccentSoft", config.Theme == "Light" ? "#FFE8D8FA" : "#49306D");
        if (!string.IsNullOrWhiteSpace(config.BackgroundImagePath) && File.Exists(config.BackgroundImagePath))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit(); image.UriSource = new Uri(config.BackgroundImagePath, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 1800; image.EndInit(); image.Freeze();
                Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill, Opacity = 0.34 };
            }
            catch { Background = new LinearGradientBrush(light ? Color.FromRgb(245,239,250) : Color.FromRgb(34,18,55), light ? Color.FromRgb(222,211,239) : Color.FromRgb(13,12,25), 35); }
        }
        else Background = new LinearGradientBrush(light ? Color.FromRgb(245,239,250) : Color.FromRgb(34,18,55), light ? Color.FromRgb(222,211,239) : Color.FromRgb(13,12,25), 35);
        Foreground = (Brush)Resources["PrimaryText"];
    }

    private void RefreshAll()
    {
        RootsList.ItemsSource = null;
        RootsList.ItemsSource = config.Roots.ToList();
        FolderTiles.ItemsSource = config.Roots.Select(root => new FolderTileEntry(System.IO.Path.GetFileName(root.TrimEnd(System.IO.Path.DirectorySeparatorChar)), root, $"{library.CountForRoot(root):N0} itens")).ToList();
        RefreshCollections();
        RefreshTracks();
    }

    private void RefreshCollections()
    {
        var rows = new List<string>();
        foreach (var group in config.Groups)
        {
            rows.Add("▾  " + group.Name);
            rows.AddRange(group.Playlists.Keys.Select(p => "     ♫  " + p));
        }
        CollectionsList.ItemsSource = rows;
    }

    private void RefreshTracks()
    {
        string query = SearchBox.Text?.Trim() ?? "";
        var all = library.AllEntries();
        IEnumerable<TrackEntry> filtered = all.Where(t =>
            t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Artist.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Album.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Genre.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.FileName.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (currentView == "Folders" && !string.IsNullOrWhiteSpace(selectedRoot))
        {
            string normalizedRoot = System.IO.Path.GetFullPath(selectedRoot).TrimEnd(
                System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar);
            filtered = filtered.Where(t =>
            {
                string fullPath = System.IO.Path.GetFullPath(t.Path);
                return fullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                       fullPath.StartsWith(normalizedRoot + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                       fullPath.StartsWith(normalizedRoot + System.IO.Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            });
        }
        else if (currentView == "Albums") filtered = filtered.GroupBy(t => t.Artist + "|" + t.Album).Select(g => g.First());
        else if (currentView == "Artists") filtered = filtered.GroupBy(t => t.Artist).Select(g => g.First());
        else if (currentView == "Genres") filtered = filtered.GroupBy(t => t.Genre).Select(g => g.First());

        visibleTracks = filtered.OrderBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
        int pageCount = Math.Max(1, (int)Math.Ceiling(visibleTracks.Count / (double)PageSize));
        currentPage = Math.Clamp(currentPage, 0, pageCount - 1);
        TracksList.ItemsSource = visibleTracks.Skip(currentPage * PageSize).Take(PageSize).ToList();
        string unit = currentView switch { "Albums" => "álbuns", "Artists" => "artistas", "Genres" => "gêneros", _ => "faixas" };
        TrackCount.Text = $"{visibleTracks.Count:N0} {unit}";
        TrackPageLabel.Text = $"{currentPage + 1} / {pageCount}";
        UpdateTabStyles();
    }

    private void UpdateTabStyles()
    {
        var active = (Brush)Resources["AccentSoft"];
        foreach (var pair in new[] { ("All", TabAll), ("Music", TabMusic), ("Albums", TabAlbums), ("Artists", TabArtists), ("Genres", TabGenres), ("Folders", TabFolders) })
            pair.Item2.Background = currentView == pair.Item1 ? active : Brushes.Transparent;
    }

    private void SetView(string view)
    {
        currentView = view;
        currentPage = 0;
        RefreshTracks();
    }

    private void TabAll_Click(object sender, RoutedEventArgs e) => SetView("All");
    private void TabMusic_Click(object sender, RoutedEventArgs e) => SetView("Music");
    private void TabAlbums_Click(object sender, RoutedEventArgs e) => SetView("Albums");
    private void TabArtists_Click(object sender, RoutedEventArgs e) => SetView("Artists");
    private void TabGenres_Click(object sender, RoutedEventArgs e) => SetView("Genres");
    private void TabFolders_Click(object sender, RoutedEventArgs e) => SetView("Folders");

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Escolha uma pasta com suas músicas", Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        StatusText.Text = "Adicionando pasta à biblioteca…";
        SetLibraryControlsEnabled(false);
        List<string> found;
        try
        {
            found = await Task.Run(() =>
            {
                var files = library.Scan(dialog.FolderName);
                _ = library.AllEntries(); // lê tags/capas fora da thread visual
                return files;
            });
            RefreshAll();
            StatusText.Text = found.Count == 0
                ? "Pasta adicionada, mas nenhum áudio compatível foi encontrado. Verifique se há MP3, WAV, FLAC, M4A, AAC ou WMA."
                : $"{found.Count:N0} músicas encontradas · Clique numa capa para reproduzir";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível ler essa pasta.";
            MessageBox.Show(ex.Message, "Lumina Player", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { SetLibraryControlsEnabled(true); }
    }

    private async Task LoadInitialLibraryAsync()
    {
        SetLibraryControlsEnabled(false);
        int count = await Task.Run(() => library.AllEntries().Count);
        RefreshAll();
        SetLibraryControlsEnabled(true);
        StatusText.Text = count == 0 ? "Biblioteca local vazia · Adicione uma pasta" : $"{count:N0} músicas disponíveis · Offline";
    }

    private void SetLibraryControlsEnabled(bool enabled) { SearchBox.IsEnabled = enabled; TracksList.IsEnabled = enabled; }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) { currentPage = 0; RefreshTracks(); } }
    private void AllMusic_Click(object sender, RoutedEventArgs e) { SearchBox.Text = ""; SetView("All"); }
    private void PreviousPage_Click(object sender, RoutedEventArgs e) { if (currentPage > 0) { currentPage--; RefreshTracks(); } }
    private void NextPage_Click(object sender, RoutedEventArgs e) { if ((currentPage + 1) * PageSize < visibleTracks.Count) { currentPage++; RefreshTracks(); } }
    private void FocusFolders_Click(object sender, RoutedEventArgs e)
    {
        currentView = "Folders";
        RootsList.Focus();
        if (RootsList.SelectedItem is string root) selectedRoot = root;
        currentPage = 0;
        RefreshTracks();
        StatusText.Text = "Escolha uma pasta para filtrar sua biblioteca.";
    }

    private void Root_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RootsList.SelectedItem is not string root) return;
        selectedRoot = root;
        currentView = "Folders";
        currentPage = 0;
        RefreshTracks();
        StatusText.Text = $"Pasta selecionada: {System.IO.Path.GetFileName(root.TrimEnd(System.IO.Path.DirectorySeparatorChar))}";
    }

    private async void FolderTile_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not FolderTileEntry folder) return;
        StatusText.Text = "Preparando a pasta…";
        try
        {
            var files = await Task.Run(() => { var found = library.Scan(folder.Path); _ = library.AllEntries(); return found; });
            RefreshAll();
            queue = files;
            queueIndex = 0;
            if (queue.Count > 0) PlayCurrent();
            else StatusText.Text = "Nenhuma música compatível encontrada nesta pasta";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível abrir esta pasta.";
            MessageBox.Show(ex.Message, "Lumina Player", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Track_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TracksList.SelectedItem is not TrackEntry selected) return;
        var all = library.AllEntries();
        queue = currentView switch
        {
            "Albums" => all.Where(t => t.Album == selected.Album && t.Artist == selected.Artist).Select(t => t.Path).ToList(),
            "Artists" => all.Where(t => t.Artist == selected.Artist).Select(t => t.Path).ToList(),
            "Genres" => all.Where(t => t.Genre == selected.Genre).Select(t => t.Path).ToList(),
            _ => visibleTracks.Select(t => t.Path).ToList()
        };
        queueIndex = queue.IndexOf(selected.Path);
        PlayCurrent();
    }

    private void TrackCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TrackEntry selected) return;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (TracksList.SelectedItems.Contains(selected)) TracksList.SelectedItems.Remove(selected);
            else TracksList.SelectedItems.Add(selected);
            return;
        }
        TracksList.SelectedItem = selected;
        PlayTrack(selected);
    }

    private void PlayTrack(TrackEntry selected)
    {
        var all = library.AllEntries();
        queue = currentView switch
        {
            "Albums" => all.Where(t => t.Album == selected.Album && t.Artist == selected.Artist).Select(t => t.Path).ToList(),
            "Artists" => all.Where(t => t.Artist == selected.Artist).Select(t => t.Path).ToList(),
            "Genres" => all.Where(t => t.Genre == selected.Genre).Select(t => t.Path).ToList(),
            _ => visibleTracks.Select(t => t.Path).ToList()
        };
        queueIndex = queue.IndexOf(selected.Path);
        if (queueIndex < 0) { queue = new List<string> { selected.Path }; queueIndex = 0; }
        PlayCurrent();
    }

    private void PlayAll_Click(object sender, RoutedEventArgs e)
    {
        queue = visibleTracks.Select(t => t.Path).ToList();
        if (queue.Count == 0)
        {
            StatusText.Text = "Nenhuma faixa encontrada. Adicione uma pasta com arquivos de áudio compatíveis.";
            return;
        }
        queueIndex = 0;
        PlayCurrent();
    }

    private void Playlists_Click(object sender, RoutedEventArgs e)
    {
        CollectionsList.Focus();
        StatusText.Text = "Escolha uma playlist na barra lateral. Dê duplo clique para reproduzir.";
    }

    private void Collection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CollectionsList.SelectedItem is string label && !label.StartsWith("▾"))
            StatusText.Text = $"Playlist selecionada: {label.Replace("♫", "").Trim()} · duplo clique para tocar";
    }

    private async void Root_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RootsList.SelectedItem is not string root) return;
        StatusText.Text = "Preparando a pasta…";
        queue = await Task.Run(() => { var files = library.Scan(root); _ = library.AllEntries(); return files; });
        queueIndex = 0;
        if (queue.Count > 0) PlayCurrent();
    }

    private void Collection_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CollectionsList.SelectedItem is not string label || label.StartsWith("▾")) return;
        string playlistName = label.Trim().Replace("♫", "").Trim();
        foreach (var group in config.Groups)
            if (group.Playlists.TryGetValue(playlistName, out var tracks))
            {
                queue = tracks.Where(File.Exists).ToList();
                queueIndex = 0;
                PlayCurrent();
                return;
            }
    }

    private void PlayCurrent()
    {
        if (queueIndex < 0 || queueIndex >= queue.Count) return;
        try
        {
            audio.Play(queue[queueIndex]);
            for (int i = 0; i < eqSliders.Length; i++) audio.SetBand(i, eqSliders[i].Value);
            audio.SetVolume(config.Volume, muted);
            currentTrack = library.AllEntries().FirstOrDefault(t => string.Equals(t.Path, queue[queueIndex], StringComparison.OrdinalIgnoreCase));
            NowPlaying.Text = currentTrack?.Title ?? System.IO.Path.GetFileNameWithoutExtension(queue[queueIndex]);
            NowArtist.Text = currentTrack?.Artist ?? "Artista desconhecido";
            NowAlbum.Text = currentTrack?.Album ?? "Arquivo local";
            DockTrack.Text = NowPlaying.Text;
            NowCover.Source = null;
            DockCover.Source = null;
            if (!string.IsNullOrWhiteSpace(currentTrack?.CoverPath) && File.Exists(currentTrack.CoverPath))
            {
                var cover = new BitmapImage();
                cover.BeginInit(); cover.UriSource = new Uri(currentTrack.CoverPath, UriKind.Absolute); cover.CacheOption = BitmapCacheOption.OnLoad; cover.DecodePixelWidth = 480; cover.EndInit(); cover.Freeze();
                NowCover.Source = cover; DockCover.Source = cover; NowCoverGlyph.Visibility = Visibility.Collapsed; DockCoverGlyph.Visibility = Visibility.Collapsed;
            }
            else { NowCoverGlyph.Visibility = Visibility.Visible; DockCoverGlyph.Visibility = Visibility.Visible; }
            LoadLyrics(queue[queueIndex]);
            StatusText.Text = "Tocando agora · Arquivo local";
            DockStatus.Text = "Tocando agora · Offline";
            PlayButton.Content = "Ⅱ"; DockPlayButton.Content = "Ⅱ";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível reproduzir este arquivo";
            MessageBox.Show(ex.Message, "Lumina Player", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadLyrics(string audioPath)
    {
        try
        {
            string lrc = System.IO.Path.ChangeExtension(audioPath, ".lrc");
            string txt = System.IO.Path.ChangeExtension(audioPath, ".txt");
            string? source = File.Exists(lrc) ? lrc : File.Exists(txt) ? txt : null;
            LyricsText.Text = source is null ? "Nenhuma letra local encontrada. Adicione um arquivo .lrc ou .txt com o mesmo nome da música." : File.ReadAllText(source).Trim();
        }
        catch { LyricsText.Text = "Não foi possível carregar as letras locais."; }
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (audio.CurrentPath is null)
        {
            if (queue.Count == 0) { queue = visibleTracks.Select(t => t.Path).ToList(); queueIndex = 0; }
            if (queue.Count == 0) { StatusText.Text = "Adicione uma pasta que contenha músicas compatíveis."; return; }
            PlayCurrent();
            return;
        }
        var playbackTime = audio.GetTime();
        if (playbackTime.Duration > TimeSpan.Zero && playbackTime.Position >= playbackTime.Duration)
            audio.Seek(TimeSpan.Zero);
        audio.TogglePause();
        PlayButton.Content = audio.IsPlaying ? "Ⅱ" : "▶"; DockPlayButton.Content = PlayButton.Content;
        StatusText.Text = audio.IsPlaying ? "Tocando agora · Arquivo local" : "Pausado";
        DockStatus.Text = audio.IsPlaying ? "Tocando agora · Offline" : "Pausado";
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (audio.GetTime().Position.TotalSeconds > 3) { audio.Seek(TimeSpan.Zero); return; }
        if (queue.Count == 0) return;
        queueIndex = (queueIndex - 1 + queue.Count) % queue.Count;
        PlayCurrent();
    }

    private void Next_Click(object sender, RoutedEventArgs e) => AdvanceManual();
    private void AdvanceManual()
    {
        if (queue.Count == 0) return;
        if (loopMode == "One") { PlayCurrent(); return; }
        if (queueIndex + 1 < queue.Count) queueIndex++;
        else if (loopMode == "Playlist") queueIndex = 0;
        else return;
        PlayCurrent();
    }

    private void AdvanceAutomatic()
    {
        if (queue.Count == 0) return;
        if (loopMode == "One") { PlayCurrent(); return; }
        if (queueIndex + 1 < queue.Count) { queueIndex++; PlayCurrent(); }
        else if (loopMode == "Playlist") { queueIndex = 0; PlayCurrent(); }
        else { PlayButton.Content = "▶"; DockPlayButton.Content = "▶"; StatusText.Text = "Fim da fila"; DockStatus.Text = "Fim da fila"; }
    }

    private void Loop_Click(object sender, RoutedEventArgs e)
    {
        loopMode = loopMode switch { "Off" => "One", "One" => "Playlist", _ => "Off" };
        config.LoopMode = loopMode;
        ConfigStore.Save(config);
        LoopButton.Content = LoopLabel();
    }
    private string LoopLabel() => loopMode switch { "One" => "↻ 1", "Playlist" => "↻ ∞", _ => "↻" };

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        muted = !muted;
        config.Muted = muted;
        audio.SetVolume(config.Volume, muted);
        MuteButton.Content = muted ? "◖×" : "◖))";
        ConfigStore.Save(config);
    }

    private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (audio is null || config is null) return;
        config.Volume = (int)VolumeSlider.Value;
        audio.SetVolume(config.Volume, muted);
    }

    private void Volume_MouseUp(object sender, MouseButtonEventArgs e) => ConfigStore.Save(config);

    private void UpdatePlaybackUi()
    {
        var (position, duration) = audio.GetTime();
        Elapsed.Text = position.ToString(@"m\:ss");
        Duration.Text = duration.ToString(@"m\:ss");
        if (!dragging && duration.TotalMilliseconds > 0)
        {
            double value = position.TotalMilliseconds / duration.TotalMilliseconds * 1000;
            syncingProgress = true;
            Progress.Value = value;
            DockProgress.Value = value;
            syncingProgress = false;
        }
        if (audio.CurrentPath is not null)
        {
            PlayButton.Content = audio.IsPlaying ? "Ⅱ" : "▶"; DockPlayButton.Content = PlayButton.Content;
            DockElapsed.Text = Elapsed.Text; DockDuration.Text = Duration.Text; DockProgress.Value = Progress.Value;
        }
        float level = audio.IsPlaying ? audio.Level : 0f;
        int barIndex = 0;
        foreach (var bar in VisualizerBars.Children.OfType<System.Windows.Shapes.Rectangle>())
        {
            double modulation = 0.28 + 0.72 * Math.Abs(Math.Sin((barIndex + 1) * 0.83));
            bar.Height = Math.Max(3, 3 + level * 27 * modulation);
            barIndex++;
        }
    }

    private void Progress_MouseDown(object sender, MouseButtonEventArgs e) => dragging = true;
    private void Progress_MouseUp(object sender, MouseButtonEventArgs e)
    {
        dragging = false;
        var (_, duration) = audio.GetTime();
        if (duration.TotalMilliseconds > 0)
            audio.Seek(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * Progress.Value / 1000));
    }
    private void Progress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (syncingProgress || DockProgress is null) return;
        syncingProgress = true;
        DockProgress.Value = Progress.Value;
        syncingProgress = false;
    }

    private void DockProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (syncingProgress || Progress is null) return;
        syncingProgress = true;
        Progress.Value = DockProgress.Value;
        syncingProgress = false;
    }

    private void DockProgress_MouseDown(object sender, MouseButtonEventArgs e) => dragging = true;
    private void DockProgress_MouseUp(object sender, MouseButtonEventArgs e)
    {
        dragging = false;
        var (_, duration) = audio.GetTime();
        if (duration.TotalMilliseconds > 0)
            audio.Seek(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * DockProgress.Value / 1000));
    }

    private void Preset_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || loadingPresets || PresetCombo.SelectedItem is not ComboBoxItem item) return;
        string preset = item.Content?.ToString() ?? "Manual";
        if (config.EqPresets.TryGetValue(preset, out var saved))
        {
            for (int i = 0; i < 10; i++) if (saved.TryGetValue(i.ToString(), out var value)) eqSliders[i].Value = value;
            return;
        }
        double[] values = preset switch
        {
            "Bass Boost" => new[] { 5d, 4, 3, 1, 0, 0, 0, 1, 2, 2 },
            "Rock" => new[] { 4d, 3, 1, -1, -2, 1, 3, 4, 4, 3 },
            "Pop" => new[] { -1d, 1, 3, 4, 2, -1, -1, 1, 2, 2 },
            _ => new double[10]
        };
        for (int i = 0; i < 10; i++) eqSliders[i].Value = values[i];
        ConfigStore.Save(config);
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Nome do preset:", "Salvar equalização");
        if (string.IsNullOrWhiteSpace(name)) return;
        config.EqPresets[name] = eqSliders.Select((slider, i) => (Key: i.ToString(), Value: slider.Value)).ToDictionary(x => x.Key, x => x.Value);
        ConfigStore.Save(config);
        loadingPresets = true;
        PresetCombo.Items.Add(new ComboBoxItem { Content = name });
        PresetCombo.SelectedIndex = PresetCombo.Items.Count - 1;
        loadingPresets = false;
        StatusText.Text = $"Preset '{name}' salvo";
    }

    private void NewGroup_Click(object sender, RoutedEventArgs e)
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Nome da categoria:", "Nova categoria");
        if (string.IsNullOrWhiteSpace(name) || config.Groups.Any(g => g.Name == name)) return;
        config.Groups.Add(new PlaylistGroup { Name = name });
        ConfigStore.Save(config);
        RefreshCollections();
    }

    private void RenameGroup_Click(object sender, RoutedEventArgs e)
    {
        if (CollectionsList.SelectedItem is not string label || !label.StartsWith("▾")) return;
        var group = config.Groups.FirstOrDefault(g => g.Name == label.Replace("▾", "").Trim());
        if (group is null) return;
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Novo nome:", "Renomear categoria", group.Name);
        if (string.IsNullOrWhiteSpace(name) || config.Groups.Any(g => g.Name == name)) return;
        group.Name = name;
        ConfigStore.Save(config);
        RefreshCollections();
    }

    private void NewPlaylist_Click(object sender, RoutedEventArgs e)
    {
        var groups = config.Groups.Select(g => g.Name).ToArray();
        string? groupName = Microsoft.VisualBasic.Interaction.InputBox("Digite o nome exato da categoria:\n" + string.Join("\n", groups), "Nova playlist", groups.FirstOrDefault() ?? "");
        var target = config.Groups.FirstOrDefault(g => g.Name == groupName);
        if (target is null) return;
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Nome da playlist:", "Nova playlist");
        if (string.IsNullOrWhiteSpace(name) || target.Playlists.ContainsKey(name)) return;
        target.Playlists[name] = new List<string>();
        ConfigStore.Save(config);
        RefreshCollections();
    }

    private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
    {
        var selected = TracksList.SelectedItems.Cast<TrackEntry>().ToList();
        if (selected.Count == 0) { MessageBox.Show("Selecione uma ou mais músicas na biblioteca."); return; }
        var targets = config.Groups.SelectMany(g => g.Playlists.Keys.Select(p => (Group: g, Playlist: p))).ToList();
        if (targets.Count == 0) { MessageBox.Show("Crie uma playlist primeiro."); return; }
        string? playlist = Microsoft.VisualBasic.Interaction.InputBox("Digite playlist no formato Categoria / Playlist:\n" + string.Join("\n", targets.Select(t => $"{t.Group.Name} / {t.Playlist}")), "Adicionar músicas");
        var target = targets.FirstOrDefault(t => $"{t.Group.Name} / {t.Playlist}" == playlist);
        if (target.Group is null) return;
        target.Group.Playlists[target.Playlist].AddRange(selected.Select(t => t.Path).Where(p => !target.Group.Playlists[target.Playlist].Contains(p)));
        ConfigStore.Save(config);
        RefreshCollections();
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        AddMenu(menu, "Alternar tema claro/escuro", () => { config.Theme = config.Theme == "Dark" ? "Light" : "Dark"; ApplyTheme(); ConfigStore.Save(config); });
        AddMenu(menu, "Mostrar/ocultar equalizador", () => { config.ShowEqualizer = !config.ShowEqualizer; EqualizerCard.Visibility = config.ShowEqualizer ? Visibility.Visible : Visibility.Collapsed; ConfigStore.Save(config); });
        AddMenu(menu, "Mostrar/ocultar barra lateral", () => { SidebarBorder.Visibility = SidebarBorder.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; });
        AddMenu(menu, "Layout compacto", () => { config.Compact = !config.Compact; Height = config.Compact ? 720 : 850; Width = config.Compact ? 1180 : 1440; ConfigStore.Save(config); });
        AddMenu(menu, "Escolher imagem de fundo…", ChooseBackgroundImage);
        AddMenu(menu, "Remover imagem de fundo", ClearBackgroundImage);
        AddMenu(menu, "Cor de destaque: Roxo", () => SetAccent("#B65CFF"));
        AddMenu(menu, "Cor de destaque: Azul", () => SetAccent("#579BFF"));
        AddMenu(menu, "Cor de destaque: Rosa", () => SetAccent("#F05FAF"));
        AddMenu(menu, "Cor de destaque: Verde", () => SetAccent("#45C6A3"));
        AddMenu(menu, "Nova categoria", () => NewGroup_Click(this, new RoutedEventArgs()));
        AddMenu(menu, "Nova playlist", () => NewPlaylist_Click(this, new RoutedEventArgs()));
        AddMenu(menu, "Adicionar seleção à playlist", () => AddToPlaylist_Click(this, new RoutedEventArgs()));
        AddMenu(menu, "Renomear categoria selecionada", () => RenameGroup_Click(this, new RoutedEventArgs()));
        AddMenu(menu, "Reexaminar biblioteca", RescanLibrary);
        AddMenu(menu, "Gerenciar pastas vinculadas", ManageRoots);
        AddMenu(menu, "Sobre o Lumina Player", () => MessageBox.Show("Lumina Player\nReprodutor de música local e offline.\n\nProjetado para baixo consumo: sem streaming, anúncios ou serviços em segundo plano.", "Sobre", MessageBoxButton.OK, MessageBoxImage.Information));
        menu.IsOpen = true;
    }

    private void SetAccent(string color)
    {
        config.Accent = color;
        ApplyTheme();
        ConfigStore.Save(config);
    }

    private void ChooseBackgroundImage()
    {
        var dialog = new OpenFileDialog { Title = "Escolha uma imagem para o fundo", Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.webp", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        config.BackgroundImagePath = dialog.FileName;
        ApplyTheme(); ConfigStore.Save(config);
    }

    private void ClearBackgroundImage()
    {
        config.BackgroundImagePath = "";
        ApplyTheme(); ConfigStore.Save(config);
    }

    private void ShowEqualizer_Click(object sender, RoutedEventArgs e)
    {
        config.ShowEqualizer = !config.ShowEqualizer;
        EqualizerCard.Visibility = config.ShowEqualizer ? Visibility.Visible : Visibility.Collapsed;
        ConfigStore.Save(config);
    }

    private void FocusCollections_Click(object sender, RoutedEventArgs e) => CollectionsList.Focus();

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        if (queue.Count == 0)
        {
            queue = visibleTracks.Select(t => t.Path).ToList();
            if (queue.Count == 0) return;
            queue = queue.OrderBy(_ => Random.Shared.Next()).ToList();
            queueIndex = 0;
            PlayCurrent();
            return;
        }
        var current = queueIndex >= 0 && queueIndex < queue.Count ? queue[queueIndex] : null;
        var remaining = queue.Skip(Math.Max(queueIndex + 1, 0)).OrderBy(_ => Random.Shared.Next()).ToList();
        queue = (current is null ? new List<string>() : new List<string> { current }).Concat(remaining).ToList();
        queueIndex = current is null ? 0 : 0;
        StatusText.Text = "Fila embaralhada";
        DockStatus.Text = "Fila embaralhada";
    }

    private static void AddMenu(ContextMenu menu, string title, Action action)
    {
        var item = new MenuItem { Header = title };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private async void RescanLibrary()
    {
        StatusText.Text = "Reexaminando pastas e arquivos de áudio…";
        SetLibraryControlsEnabled(false);
        try
        {
            await Task.Run(() =>
            {
                foreach (string root in config.Roots.ToList())
                    if (Directory.Exists(root)) library.Scan(root);
                _ = library.AllEntries();
            });
            RefreshAll();
            StatusText.Text = $"{library.AllEntries().Count:N0} faixas encontradas · Biblioteca atualizada";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível atualizar a biblioteca.";
            MessageBox.Show(ex.Message, "Lumina Player", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { SetLibraryControlsEnabled(true); }
    }

    private void ManageRoots()
    {
        if (config.Roots.Count == 0) { MessageBox.Show("Nenhuma pasta foi adicionada."); return; }
        string? root = Microsoft.VisualBasic.Interaction.InputBox("Pastas vinculadas:\n" + string.Join("\n", config.Roots) + "\n\nDigite o caminho exato para remover:", "Gerenciar pastas");
        if (!string.IsNullOrWhiteSpace(root) && config.Roots.Remove(root)) { ConfigStore.Save(config); RefreshAll(); }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !(Keyboard.FocusedElement is TextBox)) { PlayPause_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Right) { AdvanceManual(); e.Handled = true; }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Left) { Previous_Click(this, new RoutedEventArgs()); e.Handled = true; }
    }
}

