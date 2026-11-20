using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LuminaPlayer;

public sealed class AudioService : IDisposable
{
    private IWavePlayer? output;
    private WaveStream? reader;
    private EqualizerSampleProvider? equalizer;
    public string? CurrentPath { get; private set; }
    public bool IsPlaying => output?.PlaybackState == PlaybackState.Playing;
    public event Action? PlaybackEnded;

    public void Play(string path)
    {
        Stop();
        CurrentPath = path;
        try
        {
            // AudioFileReader cobre MP3/WAV/AIFF. MediaFoundationReader serve
            // como fallback para formatos que os codecs do Windows reconhecem.
            try { reader = new AudioFileReader(path); }
            catch { reader = new MediaFoundationReader(path); }

            ISampleProvider source = reader is AudioFileReader afr
                ? afr
                : reader.ToSampleProvider();

            equalizer = new EqualizerSampleProvider(source);
            output = new WasapiOut(AudioClientShareMode.Shared, 80);
            output.PlaybackStopped += OnPlaybackStopped;
            output.Init(equalizer);
            output.Volume = 0.75f;
            output.Play();
        }
        catch
        {
            Stop();
            throw new InvalidOperationException(
                "Não foi possível decodificar este arquivo. O formato pode não ser compatível com os codecs instalados no Windows.");
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (reader is not null && reader.Position >= reader.Length)
            PlaybackEnded?.Invoke();
    }

    public void TogglePause()
    {
        if (output is null) return;
        if (IsPlaying) output.Pause(); else output.Play();
    }

    public void Stop()
    {
        if (output is not null)
        {
            output.PlaybackStopped -= OnPlaybackStopped;
            output.Stop();
            output.Dispose();
            output = null;
        }
        reader?.Dispose();
        reader = null;
        equalizer = null;
        CurrentPath = null;
    }

    public void SetVolume(int value, bool muted)
    {
        if (output is not null)
            output.Volume = muted ? 0 : Math.Clamp(value, 0, 100) / 100f;
    }

    public void SetBand(int index, double db) => equalizer?.SetBand(index, db);

    public (TimeSpan Position, TimeSpan Duration) GetTime()
    {
        if (reader is null) return (TimeSpan.Zero, TimeSpan.Zero);
        return (reader.CurrentTime, reader.TotalTime);
    }

    public void Seek(TimeSpan position)
    {
        if (reader is null) return;
        reader.CurrentTime = position < TimeSpan.Zero ? TimeSpan.Zero :
            position > reader.TotalTime ? reader.TotalTime : position;
    }

    public void Dispose() => Stop();
}
