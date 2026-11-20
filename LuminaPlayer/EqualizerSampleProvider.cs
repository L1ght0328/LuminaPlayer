using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LuminaPlayer;

// Equalizador paramétrico em 10 bandas (peaking biquad), processado em tempo real.
public sealed class EqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly Biquad[][] filters;
    private readonly double[] centers = { 31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
    private readonly object gate = new();
    private readonly bool[] activeBands = new bool[10];
    private float level;
    public float Level => Volatile.Read(ref level);

    public EqualizerSampleProvider(ISampleProvider source)
    {
        this.source = source;
        filters = new Biquad[source.WaveFormat.Channels][];
        for (int ch = 0; ch < filters.Length; ch++)
        {
            filters[ch] = new Biquad[centers.Length];
            for (int i = 0; i < centers.Length; i++)
                filters[ch][i] = new Biquad(source.WaveFormat.SampleRate, centers[i], 1.0, 0);
        }
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public void SetBand(int index, double gainDb)
    {
        lock (gate)
        {
            activeBands[index] = Math.Abs(gainDb) > 0.01;
            for (int ch = 0; ch < filters.Length; ch++)
                filters[ch][index].Configure(WaveFormat.SampleRate, centers[index], 1.0, gainDb);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = source.Read(buffer, offset, count);
        float peak = 0;
        lock (gate)
        {
            bool hasActiveBand = activeBands.Any(active => active);
            int channels = WaveFormat.Channels;
            for (int n = 0; n < read; n++)
            {
                float sample = buffer[offset + n];
                if (hasActiveBand)
                {
                    int ch = n % channels;
                    for (int band = 0; band < centers.Length; band++)
                        if (activeBands[band]) sample = filters[ch][band].Process(sample);
                    sample = Math.Clamp(sample, -1f, 1f);
                    buffer[offset + n] = sample;
                }
                peak = Math.Max(peak, Math.Abs(sample));
            }
        }
        Volatile.Write(ref level, peak);
        return read;
    }

    private sealed class Biquad
    {
        private double b0, b1, b2, a1, a2, x1, x2, y1, y2;
        public Biquad(int rate, double freq, double q, double gain) => Configure(rate, freq, q, gain);

        public void Configure(int rate, double freq, double q, double gainDb)
        {
            double A = Math.Pow(10, gainDb / 40.0);
            double w = 2 * Math.PI * Math.Min(freq, rate * 0.45) / rate;
            double alpha = Math.Sin(w) / (2 * q);
            double cos = Math.Cos(w);
            double a0 = 1 + alpha / A;
            b0 = (1 + alpha * A) / a0;
            b1 = (-2 * cos) / a0;
            b2 = (1 - alpha * A) / a0;
            a1 = (-2 * cos) / a0;
            a2 = (1 - alpha / A) / a0;
        }

        public float Process(float x)
        {
            double y = b0*x + b1*x1 + b2*x2 - a1*y1 - a2*y2;
            x2=x1; x1=x; y2=y1; y1=y;
            return (float)y;
        }
    }
}
