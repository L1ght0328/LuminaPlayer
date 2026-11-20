using Microsoft.Win32;
using NAudio.Wave;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LuminaPlayer;

public partial class MainWindow : Window
{
    private readonly AppConfig config = ConfigStore.Load();
    private readonly LibraryService library;
    private readonly AudioService audio = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private List<string> visibleTracks = new();
    private List<string> queue = new();
    private int queueIndex = -1;
    private bool dragging;
    private bool muted;
    private string loopMode = "Off";
    private readonly Slider[] eqSliders = new Slider[10];
    private readonly double[] frequencies = {31,62,125,250,500,1000,2000,4000,8000,16000};

    public MainWindow()
    {
        InitializeComponent();
        library = new LibraryService(config);
        muted = config.Muted; loopMode = config.LoopMode;
        VolumeSlider.Value = config.Volume;
        timer.Tick += (_,_) => UpdatePlaybackUi();
        timer.Start();
        audio.PlaybackEnded += () => Dispatcher.Invoke(AdvanceAutomatic);
        BuildEq();
        ApplyPreferences();
        RefreshAll();
        KeyDown += MainWindow_KeyDown;
        Closed += (_,_) => { timer.Stop(); audio.Dispose(); };
        SearchBox.ToolTip = "Pesquisar por nome de arquivo";
    }

    private void BuildEq()
    {
        for (int i=0;i<10;i++)
        {
            var panel = new StackPanel { Margin = new Thickness(7,2,7,2) };
            panel.Children.Add(new TextBlock { Text = frequencies[i] >= 1000 ? $"{frequencies[i]/1000:0.#}k" : $"{frequencies[i]:0}Hz", Foreground=Brushes.Gainsboro, HorizontalAlignment=HorizontalAlignment.Center });
            var slider = new Slider { Minimum=-12, Maximum=12, Value=0, Orientation=Orientation.Vertical, Height=92, Width=25 };
            int band=i;
            slider.ValueChanged += (_,_) => { audio.SetBand(band,slider.Value); config.EqBands[band.ToString()] = slider.Value; ConfigStore.Save(config); };
            panel.Children.Add(slider);
            panel.Children.Add(new TextBlock { Text="dB", Foreground=Brushes.Gray, HorizontalAlignment=HorizontalAlignment.Center, FontSize=10 });
            eqSliders[i]=slider; EqPanel.Children.Add(panel);
            if (config.EqBands.TryGetValue(i.ToString(),out double value)) slider.Value=value;
        }
    }

    private void ApplyPreferences()
    {
        muted=config.Muted; loopMode=config.LoopMode;
        LoopButton.Content=LoopLabel();
        VolumeSlider.Value=config.Volume;
        MuteButton.Content=muted?"Ativar som":"Mudo";
        LibraryGroup.Visibility=config.ShowLibrary?Visibility.Visible:Visibility.Collapsed;
        PlaylistsGroup.Visibility=config.ShowPlaylists?Visibility.Visible:Visibility.Collapsed;
        EqualizerGroup.Visibility=config.ShowEqualizer?Visibility.Visible:Visibility.Collapsed;
        if (config.Compact) Height=680;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.Theme=="Light"?"#F2F3F7":"#171922"));
        Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.Theme=="Light"?"#20232B":"#EEEEF5"));
    }

    private void RefreshAll()
    {
        RootsList.ItemsSource=null; RootsList.ItemsSource=config.Roots.ToList();
        RefreshCollections(); RefreshTracks();
    }

    private void RefreshCollections()
    {
        var rows=new List<string>();
        foreach(var group in config.Groups)
        {
            rows.Add("▣ "+group.Name);
            rows.AddRange(group.Playlists.Keys.Select(p=>"   └ "+p));
        }
        CollectionsList.ItemsSource=rows;
    }

    private void RefreshTracks()
    {
        var query=SearchBox.Text?.Trim()??"";
        visibleTracks=library.AllTracks().Where(p=>Path.GetFileName(p).Contains(query,StringComparison.OrdinalIgnoreCase)).ToList();
        TracksList.ItemsSource=visibleTracks.Select(p=>Path.GetFileNameWithoutExtension(p)).ToList();
    }

    private void AddFolder_Click(object sender,RoutedEventArgs e)
    {
        using var dialog=new System.Windows.Forms.FolderBrowserDialog { Description="Selecione uma pasta com músicas", UseDescriptionForTitle=true, ShowNewFolderButton=false };
        if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)
        {
            StatusText.Text="Indexando arquivos…";
            var found=library.Scan(dialog.SelectedPath);
            RefreshAll(); StatusText.Text=$"{found.Count} arquivos encontrados · Offline";
        }
    }

    private void SearchBox_TextChanged(object sender,TextChangedEventArgs e) { if(IsLoaded) RefreshTracks(); }

    private void Track_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        int index=TracksList.SelectedIndex;
        if(index<0)return;
        queue=visibleTracks.ToList(); queueIndex=index; PlayCurrent();
    }

    private void Root_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(RootsList.SelectedItem is not string root)return;
        queue=library.Scan(root); queueIndex=0; PlayCurrent();
    }

    private void Collection_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if(CollectionsList.SelectedItem is not string label)return;
        string trimmed=label.Trim();
        if(label.StartsWith("▣ "))return;
        foreach(var group in config.Groups)
            if(group.Playlists.TryGetValue(trimmed.Replace("└","").Trim(),out var tracks))
            { queue=tracks.Where(File.Exists).ToList(); queueIndex=0; PlayCurrent(); return; }
    }

    private void PlayCurrent()
    {
        if(queueIndex<0||queueIndex>=queue.Count)return;
        try
        {
            audio.Play(queue[queueIndex]);
            NowPlaying.Text=Path.GetFileNameWithoutExtension(queue[queueIndex]);
            StatusText.Text="Reproduzindo · Offline";
            PlayButton.Content="Ⅱ";
        }
        catch(Exception ex) { MessageBox.Show(ex.Message,"Lumina Player",MessageBoxButton.OK,MessageBoxImage.Warning); }
    }

    private void PlayPause_Click(object sender,RoutedEventArgs e)
    {
        if(audio.CurrentPath is null)
        {
            if(queue.Count==0) { queue=visibleTracks.ToList(); queueIndex=0; }
            PlayCurrent(); return;
        }
        audio.TogglePause(); PlayButton.Content=audio.IsPlaying?"Ⅱ":"▶";
    }
    private void Previous_Click(object sender,RoutedEventArgs e)
    {
        if(audio.GetTime().Position.TotalSeconds>3) { audio.Seek(TimeSpan.Zero); return; }
        if(queue.Count==0)return; queueIndex=(queueIndex-1+queue.Count)%queue.Count; PlayCurrent();
    }
    private void Next_Click(object sender,RoutedEventArgs e) => AdvanceManual();
    private void AdvanceManual()
    {
        if(queue.Count==0)return;
        if(loopMode=="One") { PlayCurrent(); return; }
        queueIndex++;
        if(queueIndex>=queue.Count) queueIndex=loopMode=="Playlist"?0:queue.Count-1;
        PlayCurrent();
    }
    private void AdvanceAutomatic()
    {
        if(queue.Count==0)return;
        if(loopMode=="One") { PlayCurrent(); return; }
        if(queueIndex+1<queue.Count) { queueIndex++; PlayCurrent(); }
        else if(loopMode=="Playlist") { queueIndex=0; PlayCurrent(); }
        else { PlayButton.Content="▶"; StatusText.Text="Fim da fila"; }
    }
    private void Loop_Click(object sender,RoutedEventArgs e)
    {
        loopMode=loopMode switch {"Off"=>"One","One"=>"Playlist",_=>"Off"};
        config.LoopMode=loopMode; ConfigStore.Save(config); LoopButton.Content=LoopLabel();
    }
    private string LoopLabel()=>loopMode switch {"One"=>"Repetir: faixa","Playlist"=>"Repetir: playlist",_=>"Repetir: desligado"};
    private void Mute_Click(object sender,RoutedEventArgs e)
    {
        muted=!muted; config.Muted=muted; audio.SetVolume((int)VolumeSlider.Value,muted);
        MuteButton.Content=muted?"Ativar som":"Mudo"; ConfigStore.Save(config);
    }
    private void Volume_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> e)
    {
        if(audio is null)return;
        config.Volume=(int)VolumeSlider.Value; audio.SetVolume(config.Volume,muted); ConfigStore.Save(config);
    }
    private void UpdatePlaybackUi()
    {
        var (pos,dur)=audio.GetTime();
        Elapsed.Text=pos.ToString(@"m\:ss"); Duration.Text=dur.ToString(@"m\:ss");
        if(!dragging && dur.TotalMilliseconds>0) Progress.Value=pos.TotalMilliseconds/dur.TotalMilliseconds*1000;
        if(audio.CurrentPath is not null) PlayButton.Content=audio.IsPlaying?"Ⅱ":"▶";
    }
    private void Progress_MouseDown(object sender,MouseButtonEventArgs e)=>dragging=true;
    private void Progress_MouseUp(object sender,MouseButtonEventArgs e)
    {
        dragging=false;
        var (_,duration)=audio.GetTime();
        if(duration.TotalMilliseconds>0)audio.Seek(TimeSpan.FromMilliseconds(duration.TotalMilliseconds*Progress.Value/1000));
    }
    private void Progress_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> e) { }
    private void Preset_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!IsLoaded||PresetCombo.SelectedIndex<=0)return;
        string preset=((ComboBoxItem)PresetCombo.SelectedItem).Content.ToString()??"";
        double[] values=preset switch
        {
            "Bass Boost"=>new[]{5d,4,3,1,0,0,0,1,2,2},
            "Rock"=>new[]{4d,3,1,-1,-2,1,3,4,4,3},
            "Pop"=>new[]{-1d,1,3,4,2,-1,-1,1,2,2},
            _=>new double[10]
        };
        for(int i=0;i<10;i++)eqSliders[i].Value=values[i];
    }
    private void SavePreset_Click(object sender,RoutedEventArgs e)
    {
        string? name=Microsoft.VisualBasic.Interaction.InputBox("Nome do preset:","Salvar equalização");
        if(string.IsNullOrWhiteSpace(name))return;
        config.EqPresets[name]=eqSliders.Select((slider,i)=>(Key:i.ToString(),Value:slider.Value)).ToDictionary(x=>x.Key,x=>x.Value);
        ConfigStore.Save(config); MessageBox.Show("Preset salvo nas configurações locais.");
    }
    private void NewGroup_Click(object sender,RoutedEventArgs e)
    {
        string? name=Microsoft.VisualBasic.Interaction.InputBox("Nome da categoria:","Nova categoria");
        if(string.IsNullOrWhiteSpace(name)||config.Groups.Any(g=>g.Name==name))return;
        config.Groups.Add(new PlaylistGroup{Name=name});ConfigStore.Save(config);RefreshCollections();
    }
    private void RenameGroup_Click(object sender,RoutedEventArgs e)
    {
        if(CollectionsList.SelectedItem is not string label||!label.StartsWith("▣ "))return;
        var group=config.Groups.FirstOrDefault(g=>g.Name==label[2..]);if(group is null)return;
        string? name=Microsoft.VisualBasic.Interaction.InputBox("Novo nome:","Renomear categoria",group.Name);
        if(string.IsNullOrWhiteSpace(name)||config.Groups.Any(g=>g.Name==name))return;
        group.Name=name;ConfigStore.Save(config);RefreshCollections();
    }
    private void NewPlaylist_Click(object sender,RoutedEventArgs e)
    {
        var groups=config.Groups.Select(g=>g.Name).ToArray();
        string? group=Microsoft.VisualBasic.Interaction.InputBox("Digite o nome exato da categoria:\n"+string.Join("\n",groups),"Nova playlist",groups.FirstOrDefault()??"");
        var target=config.Groups.FirstOrDefault(g=>g.Name==group);if(target is null)return;
        string? name=Microsoft.VisualBasic.Interaction.InputBox("Nome da playlist:","Nova playlist");
        if(string.IsNullOrWhiteSpace(name)||target.Playlists.ContainsKey(name))return;
        target.Playlists[name]=new List<string>();ConfigStore.Save(config);RefreshCollections();
    }
    private void AddToPlaylist_Click(object sender,RoutedEventArgs e)
    {
        var selected=TracksList.SelectedItems.Cast<string>().ToList();
        if(selected.Count==0){MessageBox.Show("Selecione uma ou mais faixas.");return;}
        string? path=visibleTracks.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==selected[0]);
        if(path is null)return;
        var targets=config.Groups.SelectMany(g=>g.Playlists.Keys.Select(p=>(g,p))).ToList();
        if(targets.Count==0){MessageBox.Show("Crie uma playlist primeiro.");return;}
        string? playlist=Microsoft.VisualBasic.Interaction.InputBox("Digite playlist no formato Categoria / Playlist:\n"+string.Join("\n",targets.Select(t=>$"{t.g.Name} / {t.p}")),"Adicionar músicas");
        var target=targets.FirstOrDefault(t=>$"{t.g.Name} / {t.p}"==playlist);
        if(target.g is null)return;
        var paths=selected.Select(name=>visibleTracks.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name)).Where(p=>p is not null).Cast<string>();
        target.g.Playlists[target.p].AddRange(paths.Where(p=>!target.g.Playlists[target.p].Contains(p)));
        ConfigStore.Save(config);RefreshCollections();
    }
    private void Menu_Click(object sender,RoutedEventArgs e)
    {
        var menu=new ContextMenu();
        AddMenu(menu,"Tema escuro/claro",()=>{config.Theme=config.Theme=="Dark"?"Light":"Dark";ApplyTheme();ConfigStore.Save(config);});
        AddMenu(menu,"Mostrar/ocultar biblioteca",()=>{config.ShowLibrary=!config.ShowLibrary;ApplyPreferences();ConfigStore.Save(config);});
        AddMenu(menu,"Mostrar/ocultar playlists",()=>{config.ShowPlaylists=!config.ShowPlaylists;ApplyPreferences();ConfigStore.Save(config);});
        AddMenu(menu,"Mostrar/ocultar equalizador",()=>{config.ShowEqualizer=!config.ShowEqualizer;ApplyPreferences();ConfigStore.Save(config);});
        AddMenu(menu,"Layout compacto",()=>{config.Compact=!config.Compact;Height=config.Compact?680:790;ConfigStore.Save(config);});
        AddMenu(menu,"Gerenciar pastas vinculadas",()=>ManageRoots());
        menu.IsOpen=true;
    }
    private static void AddMenu(ContextMenu menu,string title,Action action)
    {
        var item=new MenuItem{Header=title};item.Click+=(_,_)=>action();menu.Items.Add(item);
    }
    private void ManageRoots()
    {
        if(config.Roots.Count==0)return;
        string? root=Microsoft.VisualBasic.Interaction.InputBox("Pastas vinculadas:\n"+string.Join("\n",config.Roots)+"\n\nDigite o caminho exato para remover:","Gerenciar pastas");
        if(!string.IsNullOrWhiteSpace(root)&&config.Roots.Remove(root)){ConfigStore.Save(config);RefreshAll();}
    }
    private void MainWindow_KeyDown(object sender,System.Windows.Input.KeyEventArgs e)
    {
        if(e.Key==Key.Space){PlayPause_Click(this,new RoutedEventArgs());e.Handled=true;}
        if(e.KeyboardDevice.Modifiers==ModifierKeys.Control&&e.Key==Key.Right){AdvanceManual();e.Handled=true;}
        if(e.KeyboardDevice.Modifiers==ModifierKeys.Control&&e.Key==Key.Left){Previous_Click(this,new RoutedEventArgs());e.Handled=true;}
    }
}
