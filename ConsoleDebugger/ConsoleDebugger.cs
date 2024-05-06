using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ConsoleDebugger
    {
    public static class ConsoleDebugger
        {
        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        /// <summary>
        /// Configuration settings for logging events to an external log file for viewing later.
        /// </summary>
        public static class LoggingConfiguration
            {
            private static CancellationTokenSource cancelTask = new CancellationTokenSource();
            private static CancellationToken cancellationToken = cancelTask.Token;
            public static LogStyle LoggerStyle = LogStyle.CSVFormat;
            public static bool IncludeTimestamp = true;
            private static bool logrunning = false;
            public static bool LoggerActive
                {
                get => logrunning;
                set
                    {
                    if (value == true)
                        {
                        Task.Run(() => ProcessLogQueue(cancellationToken));
                        logrunning = true;
                        }
                    else
                        {
                        cancelTask.Cancel();
                        cancelTask.Dispose();
                        cancelTask = new CancellationTokenSource();
                        logrunning = false;
                        }
                    }
                }
            }

        private static readonly List<FloatSynthesizer> _trackedSynthesizers = new List<FloatSynthesizer>();
        private static ConcurrentQueue<DebugMessageEntry> _messageQueue = new ConcurrentQueue<DebugMessageEntry>();
        private static ConcurrentQueue<BeepWrapper> _beepQueue = new ConcurrentQueue<BeepWrapper>();
        private static ConcurrentQueue<DebugMessageEntry> _fileLogQueue = new ConcurrentQueue<DebugMessageEntry>();

        private static bool _isRunning = true;

        static ConsoleDebugger()
            {
            Task.Run(ProcessMessageQueue);
            Task.Run(ProcessBeepQueue);
            AppDomain.CurrentDomain.ProcessExit += Cleanup;
            }
        static void Cleanup(object sender, EventArgs e)
            {
            foreach (FloatSynthesizer _synth in _trackedSynthesizers)
                {
                _synth.Stop();
                _synth.Dispose();
                }
            }
        private static async Task ProcessBeepQueue()
            {
            var signalGenerator = new SignalGenerator();

            while (_isRunning)
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
        private static async Task ProcessMessageQueue()
            {
            while (_isRunning)
                {
                if (_messageQueue.TryDequeue(out DebugMessageEntry entry))
                    {
                    // Set colors if provided
                    if (LoggingConfiguration.LoggerActive == true)
                        {
                        _fileLogQueue.Enqueue(entry);
                        }
                    if (LoggingConfiguration.IncludeTimestamp == true)
                        {
                        Console.Write(entry.TimeCreated + ": ");
                        }
                    if (entry.Color.HasValue)
                        {

                        Console.ForegroundColor = entry.Color.Value;
                        Console.Write(entry.Message);
                        Console.ResetColor();
                        Console.WriteLine();
                        }
                    else if (entry.Type.HasValue)
                        {
                        switch (entry.Type.Value)
                            {
                            case MessageType.Warning:
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(ErrorMessage);
                                Console.ResetColor();
                                Console.WriteLine(' ' + entry.Message);
                                MessageBeep(0x00000030);
                                break;
                            case MessageType.Critical:
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(WarningMessage);
                                Console.ResetColor();
                                Console.WriteLine(' ' + entry.Message);
                                MessageBeep(0x00000010);
                                break;
                            default:
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(GeneralMessage);
                                Console.ResetColor();
                                Console.WriteLine(' ' + entry.Message);
                                MessageBeep(0x00000010);
                                break;
                            }
                        }
                    else
                        {
                        Console.WriteLine(entry.Message);
                        }

                    // Reset color redundancy 
                    Console.ResetColor();
                    }
                await Task.Delay(5);
                }
            }

        // Start/Stop logger functions

        /// <summary>
        /// Begins logging to the external log file for later review.
        /// </summary>
        public static void StartLogging()
            {
            LoggingConfiguration.LoggerActive = true;
            }
        /// <summary>
        /// Stops logging to the external log file.
        /// </summary>
        public static void StopLogging()
            {
            LoggingConfiguration.LoggerActive = false;
            }

        private static async Task ProcessLogQueue(CancellationToken cancelToken)
            {
            while (!cancelToken.IsCancellationRequested)
                {
                if (_fileLogQueue.TryDequeue(out DebugMessageEntry entry))
                    {
                    try
                        {
                        using (var fileWriter = new StreamWriter("log", true))
                            {
                            string tolog = "";
                            string tag = entrytag();
                            if (LoggingConfiguration.IncludeTimestamp == true)
                                {
                                tolog += $"{entry.TimeCreated.ToString()}";
                                if (LoggingConfiguration.LoggerStyle == LogStyle.CSVFormat)
                                    {
                                    tolog += $",{tag},{entry.Message}";
                                    }
                                else
                                    {
                                    tolog += $": {tag} {entry.Message}";
                                    }
                                }
                            await fileWriter.WriteLineAsync(tolog);

                            // nested helper
                            string entrytag() => MessageTypeDict[entry.Type ?? MessageType.Debug];
                            }
                        }
                    catch (Exception ex)
                        {

                        }
                    }
                await Task.Delay(5);
                }
            }

        // DebugMessage functions:

        /// <summary>
        /// Enqueues a basic debug message to the processing queue.
        /// </summary>
        /// <param name="message">The text content of the debug message.</param>
        public static void DebugMessage(string message)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message));
            }
        /// <summary>
        /// Enqueues a debug message with a specified foreground color.
        /// </summary>
        /// <param name="message">The text content of the debug message.</param>
        /// <param name="color">The desired foreground color for the message.</param>
        public static void DebugMessage(string message, ConsoleColor color)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message, color));
            }
        /// <summary>
        /// Enqueues a debug message with an associated message type.
        /// </summary>
        /// <param name="message">The text content of the debug message.</param>
        /// <param name="type">The type of message (General, Warning, Critical) influencing its presentation.</param>
        public static void DebugMessage(string message, MessageType type)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message, type: type));
            }
        // DebugBeep function:

        /// <summary>
        /// Enqueues a request to play an audible beep with a specified pitch and duration.
        /// </summary>
        /// <param name="pitch">The pitch of the beep.</param>
        /// <param name="duration">The duration of the beep.</param>
        public static void DebugBeep(TonePitch pitch, ToneLength duration)
            {
            _beepQueue.Enqueue(new BeepWrapper(pitch, duration));
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
                                        Console.WriteLine(*_targetValue);
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

        #region Enums
        public enum LogStyle
            {
            CSVFormat,
            PlainTextFormat
            }
        public enum MessageType
            {
            General,
            Warning,
            Critical,
            Debug
            }
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
        private record struct DebugMessageEntry
            {
            public string Message { get; }
            public ConsoleColor? Color { get; }
            public MessageType? Type { get; }
            public DateTime TimeCreated { get; }
            public DebugMessageEntry(string message, ConsoleColor? color = null, MessageType? type = null)
                {
                Message = message;
                Color = color;
                Type = type;
                TimeCreated = DateTime.Now;
                }
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
        private static readonly Dictionary<MessageType, string> MessageTypeDict = new Dictionary<MessageType, string>
        {
            {MessageType.General, "General" },
            {MessageType.Warning, "Warning" },
            {MessageType.Critical, "Error" },
            {MessageType.Debug, "Debug" },
        };
        #endregion

        #region SpecificStrings&Chars
        private readonly static string CSVExtension = "*.csv";
        private readonly static string TXTExtention = "*.txt";

        private readonly static char WarningTriangle = '\u25B2';
        private readonly static char DoubleExlamError = '\u203C';
        private readonly static char Asteriks = '*';

        private static string ErrorMessage()
            {
            return WarningTriangle + "  " + MessageTypeDict[MessageType.Critical] + "  " + WarningTriangle;
            }
        private static string WarningMessage()
            {
            return DoubleExlamError + " " + MessageTypeDict[MessageType.Warning] + " " + DoubleExlamError;
            }
        private static string GeneralMessage()
            {
            return Asteriks + MessageTypeDict[MessageType.General] + Asteriks;

            }
        #endregion

        }

    }
