using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Collections.Concurrent;
using static ConsoleDebugger.ConsoleDebugger;

namespace ConsoleDebugger.Beeps
{
    public static partial class ConsoleDebugger
    {
        private static ConcurrentQueue<BeepWrapper> _beepQueue = new ConcurrentQueue<BeepWrapper>();

        /// <summary>
        /// Enqueues a request to play an audible beep with a specified pitch and duration.
        /// </summary>
        /// <param name="pitch">The pitch of the beep.</param>
        /// <param name="duration">The duration of the beep.</param>
        public static void DebugBeep(TonePitch pitch, ToneLength duration)
        {
            _beepQueue.Enqueue(new BeepWrapper(pitch, duration));
        }

        /// <summary>
        /// Initializes the beep processing task. This should be called once at the start of the application.
        /// </summary>
        public static void InitializeBeeps()
        {
            Task.Run(ProcessBeepQueue);
        }

        private static async Task ProcessBeepQueue()
        {
            
            var signalGenerator = new SignalGenerator();

            while (LoggerIsRunning)
            {
                if (_beepQueue.TryDequeue(out BeepWrapper beep))
                {
                    int selectedFrequency = PitchDict[beep.Pitch];
                    float durationSeconds = LenDict[beep.Duration] * 0.01f; // Milliseconds to seconds

                    using (var ms = new MemoryStream())
                    {
                        using (var writer = new WaveFileWriter(ms, new WaveFormat(44100, 1))) // Adjust WaveFormat as needed
                        {
                            WriteSignal(selectedFrequency, durationSeconds, writer);
                            ms.Position = 0;

                            using (var rawSource = new RawSourceWaveStream(ms, writer.WaveFormat))
                            {
                                var waveOut = new WaveOutEvent();
                                waveOut.DeviceNumber = 0;
                                waveOut.Volume = 1.0f;
                                waveOut.Init(rawSource);
                                waveOut.Play();
                                await Task.Delay((int)durationSeconds * 100); // Wait for duration
                                waveOut.Stop();
                                waveOut.Dispose();
                            }
                        }
                    }
                }
                await Task.Delay(10); // Small delay to avoid excessive loop cycles
            }
        }


        private static void WriteSignal(int frequency, float durationSeconds, WaveFileWriter writer)
        {
            var signalGenerator = new SignalGenerator(writer.WaveFormat.SampleRate, 1);
            signalGenerator.Type = SignalGeneratorType.SawTooth;
            signalGenerator.Frequency = frequency;
            signalGenerator.Gain = 1.0;

            int sampleCount = (int)(writer.WaveFormat.SampleRate * durationSeconds);
            float[] buffer = new float[sampleCount];
            signalGenerator.Read(buffer, 0, buffer.Length);
            writer.WriteSamples(buffer, 0, buffer.Length);
        }

        #region Structs
        private struct BeepWrapper
        {
            public TonePitch Pitch { get; set; }
            public ToneLength Duration { get; set; }
            public BeepWrapper(TonePitch pitch, ToneLength duration)
            {
                Pitch = pitch;
                Duration = duration;
            }
        }
        #endregion

        #region Enums
        public enum ToneLength
        {
            ReallyBrief,
            Brief,
            ReallyShort,
            Short,
            Medium,
            Long,
            ReallyLong
        }
        public enum TonePitch
        {
            Do,
            Re,
            Mi,
            Fa,
            Sol,
            La,
            Ti
        }
        #endregion

        #region Dictionaries
        private static readonly Dictionary<ToneLength, int> LenDict = new Dictionary<ToneLength, int>
        {
            { ToneLength.ReallyBrief, 100 },
            { ToneLength.Brief, 250 },
            { ToneLength.ReallyShort, 500 },
            { ToneLength.Short, 750 },
            { ToneLength.Medium, 1000 },
            { ToneLength.Long, 1500 },
            { ToneLength.ReallyLong, 2000 }
        };
        private static readonly Dictionary<TonePitch, int> PitchDict = new Dictionary<TonePitch, int>
        {
            { TonePitch.Do, 262 },
            { TonePitch.Re, 294 },
            { TonePitch.Mi, 330 },
            { TonePitch.Fa, 349 },
            { TonePitch.Sol, 392 },
            { TonePitch.La, 440 },
            { TonePitch.Ti, 494 }
        };
        #endregion

    }
}
