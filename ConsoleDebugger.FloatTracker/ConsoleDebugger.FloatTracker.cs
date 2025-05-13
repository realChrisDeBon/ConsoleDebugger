using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using static ConsoleDebugger.ConsoleDebugger;

namespace ConsoleDebugger.FloatTracker
{
    public static partial class ConsoleDebugger
    {
        private static readonly List<FloatSynthesizer> _trackedSynthesizers = new List<FloatSynthesizer>();

        static void Cleanup(object sender, EventArgs e)
        {
            foreach (FloatSynthesizer _synth in _trackedSynthesizers)
            {
                _synth.Stop();
                _synth.Dispose();
            }
        }

        /// <summary>
        /// Initializes the float tracking system. This should be called once at the start of the application.
        /// </summary>
        public static void InitializeFloatTracking()
        {
            AppDomain.CurrentDomain.ProcessExit += Cleanup;
        }

        // StartTrackingFloat function:

        /// <summary>
        /// Begins monitoring a float variable, generating a tone whose pitch changes dynamically 
        /// based on the variable's value within a specified range. This function uses unsafe 
        /// code for direct memory access. 
        /// </summary>
        /// <param name="target">A reference to the float variable to be tracked.</param>
        /// <param name="minrange">The minimum value of the variable that corresponds to the lowest expected value of the float.</param>
        /// <param name="maxrange">The maximum value of the variable that corresponds to the highest expected value of the float.</param>
        /// <returns>A FloatSynthesizer instance, or null if an error occurs during setup.</returns>
        public static FloatSynthesizer StartTrackingFloat(ref float target, float minrange, float maxrange)
        {
            unsafe
            {
                fixed (float* ptr = &target) // Pin the variable in memory
                {
                    try
                    {
                        return new FloatSynthesizer(ptr, minrange, maxrange);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }


        public class FloatSynthesizer : IDisposable
        {
            private bool _disposed = false;
            private unsafe float* _targetValue;
            private float _minValue;
            private float _maxValue;
            private bool synth_isRunning = true;
            private SignalGenerator _siggen;
            private static WaveFormat waveformat = new WaveFormat(44100, 1);

            public unsafe FloatSynthesizer(float* targetValue, float minValue, float maxValue)
            {
                _targetValue = targetValue;
                _minValue = minValue;
                _maxValue = maxValue;

                _siggen = new SignalGenerator(44100, 1)
                {
                    Type = SignalGeneratorType.Sin,
                    Frequency = 700,
                    Gain = 0.9f
                };
                Task.Run(() => RunSynthesizer());
                _trackedSynthesizers.Add(this);
            }

            private unsafe void RunSynthesizer()
            {
                while (synth_isRunning)
                {

                    using (var waveOut = new WaveOutEvent())
                    {
                        using (var audioStream = new MemoryStream())
                        {
                            using (var audioWriter = new WaveFileWriter(audioStream, waveformat))
                            {
                                float percentage = (*_targetValue - _minValue) / (_maxValue - _minValue) * 100;
                                percentage = Math.Max(0, Math.Min(percentage, 100)); // Clamp between 0 and 100

                                float volume = percentage / 100; // Normalize to 0-1 range
                                waveOut.Volume = volume;

                                WriteSignal(_siggen, audioWriter, 0.05f);
                                audioStream.Position = 0;

                                using (var audioPlayer = new RawSourceWaveStream(audioStream, waveformat))
                                {
                                    waveOut.DeviceNumber = 0;
                                    waveOut.Init(audioPlayer);
                                    waveOut.Play();
                                    // Wait for audio playback to finish
                                    while (waveOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        System.Threading.Thread.Sleep(1);
                                    }

                                    // Clean up
                                    waveOut.Stop();
                                    waveOut.Dispose();
                                }

                            }
                        }
                    }


                    Thread.Sleep(25); // Adjust sleep time as needed
                }
            }

            private static void WriteSignal(SignalGenerator signalGenerator, WaveFileWriter writer, float durationSeconds)
            {
                double sampleCount = (int)(writer.WaveFormat.SampleRate * durationSeconds);
                float[] buffer = new float[(int)sampleCount];

                signalGenerator.Read(buffer, 0, buffer.Length);
                writer.WriteSamples(buffer, 0, buffer.Length);
            }

            public void Stop()
            {
                synth_isRunning = false;
            }
            public void Dispose()
            {
                unsafe
                {
                    _targetValue = null;
                }
            }

        }


    }
}
