using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleDebugger
    {
    public static partial class ConsoleDebugger
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
            public static bool IncludeCategory = true;
            public static bool EmitConsoleMessages = true;
            private static bool logrunning = false;
            /// <summary>
            /// Determines whether the logger is currently active.
            /// Setting this property to true will automcatically start the logger, while setting it to false will stop it.
            /// </summary>
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

        private static ConcurrentQueue<DebugMessageEntry> _messageQueue = new ConcurrentQueue<DebugMessageEntry>();
        private static ConcurrentQueue<DebugMessageEntry> _fileLogQueue = new ConcurrentQueue<DebugMessageEntry>();


        /// <summary>
        /// A dictionary of logging categories and their associated boolean values, indicating whether they are currently active.
        /// When passing DebugMessages, you can specify a category to filter messages based on the active logging categories.
        /// </summary>
        private static Dictionary<string, bool> LoggingCategories = new Dictionary<string, bool>()
        {
            { "General", true }
        };
        /// <summary>
        /// Adds a new logging category to the dictionary of logging categories.
        /// By default, the category will be active.
        /// </summary>
        /// <param name="category">The category that will be added.</param>
        public static void AddLoggingCategory(LoggingCategory category)
        {
            LoggingCategories.Add(category.CategoryName, true);
        }
        /// <summary>
        /// Removes a logging category from the dictionary of logging categories.
        /// </summary>
        /// <param name="category">The category that will be removed.</param>
        public static void RemoveLoggingCategory(LoggingCategory category)
        {
            LoggingCategories.Remove(category.CategoryName);
        }
        /// <summary>
        /// Activates a logging category, allowing messages with this category to be processed.
        /// </summary>
        /// <param name="category">The category that will activated for logging.</param>
        public static void ActivateLoggingCategory(LoggingCategory category)
        {
            LoggingCategories[category.CategoryName] = true;
        }
        /// <summary>
        /// Deactivates a logging category, preventing messages with this category from being processed to the console.
        /// </summary>
        /// <param name="category">The category that will be deactivated.</param>
        public static void DeactivateLoggingCategory(LoggingCategory category)
        {
            LoggingCategories[category.CategoryName] = false;
        }
        /// <summary>
        /// Checks whether a logging category is currently active.
        /// </summary>
        public static bool LoggingCategoryActive(LoggingCategory category)
        {
            return LoggingCategories[category.CategoryName];
        }

        public static bool LoggerIsRunning = true;

        static ConsoleDebugger()
            {
            Task.Run(ProcessMessageQueue);
            
            }

        private static async Task ProcessMessageQueue()
            {
            while (LoggerIsRunning)
                {
                if (_messageQueue.TryDequeue(out DebugMessageEntry entry))
                    {
                    if (LoggingConfiguration.LoggerActive == true)
                        {
                        _fileLogQueue.Enqueue(entry);
                        }
                    if ((LoggingCategories[entry.Category] == false) || (LoggingConfiguration.EmitConsoleMessages == false))
                    {
                        continue;
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
                                Console.Write(WarningMessage);
                                Console.ResetColor();
                                Console.WriteLine(' ' + entry.Message);
                                MessageBeep(0x00000030);
                                break;
                            case MessageType.Critical:
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(ErrorMessage);
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

        private static string GetCSVHeader()
        {
            StringBuilder header = new StringBuilder();
            if(LoggingConfiguration.IncludeTimestamp == true)
            {
                header.Append("Timestamp,");
            }
            header.Append("Tag,");
            if (LoggingConfiguration.IncludeCategory == true)
            {
                header.Append("Category,");
            }
            header.Append("Message");
            return header.ToString();
        }
        private static async Task ProcessLogQueue(CancellationToken cancelToken)
            {
            string logname = LoggingConfiguration.LoggerStyle == LogStyle.CSVFormat ? "log.csv" : "log.txt";
            if (LoggingConfiguration.LoggerStyle == LogStyle.CSVFormat)
            {
                using(var fw = new StreamWriter(logname, true))
                {
                    await fw.WriteLineAsync(GetCSVHeader());
                }
            }
            while (!cancelToken.IsCancellationRequested)
                {                
                if (_fileLogQueue.TryDequeue(out DebugMessageEntry entry))
                    {                    
                    try
                    {
                        using (var fileWriter = new StreamWriter(logname, true))
                            {
                            string tolog = "";
                            string tag = entrytag().ToString();
                            if (LoggingConfiguration.IncludeTimestamp == true)
                                {
                                tolog += $"{entry.TimeCreated.ToString()}";

                                if (LoggingConfiguration.LoggerStyle == LogStyle.CSVFormat)
                                    {
                                    if(tolog != "")
                                    {
                                        tolog += ",";
                                    }
                                    tolog += LoggingConfiguration.IncludeCategory ? $"{tag},({entry.Category}),{entry.Message}" : $"{tag},{entry.Message}";
                                    }
                                else
                                    {
                                    if(tolog != "")
                                    {
                                        tolog += ": ";
                                    }
                                    tolog += LoggingConfiguration.IncludeCategory ? $"{tag} ({entry.Category}) {entry.Message}" : $"{tag} {entry.Message}";
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
        /// <param name="category">The logging category this message falls under.</param>
        public static void DebugMessage(string message, LoggingCategory category = default)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message, category: category));
            }
        /// <summary>
        /// Enqueues a debug message with a specified foreground color.
        /// </summary>
        /// <param name="message">The text content of the debug message.</param>
        /// <param name="color">The desired foreground color for the message.</param>
        public static void DebugMessage(string message, ConsoleColor color, LoggingCategory category = default)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message, color, category: category));
            }
        /// <summary>
        /// Enqueues a debug message with an associated message type.
        /// </summary>
        /// <param name="message">The text content of the debug message.</param>
        /// <param name="type">The type of message (General, Warning, Critical) influencing its presentation.</param>
        public static void DebugMessage(string message, MessageType type, LoggingCategory category = default)
            {
            _messageQueue.Enqueue(new DebugMessageEntry(message, type: type, category: category));
            }
        // DebugBeep function:




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

        #endregion

        #region Structs
        public record struct LoggingCategory
        {
            public string CategoryName { get; }
            public LoggingCategory(string name)
            {
                CategoryName = name;
            }
        }
        private record struct DebugMessageEntry
            {
            public string Message { get; }
            public ConsoleColor? Color { get; }
            public MessageType? Type { get; }
            public string Category { get; }
            public DateTime TimeCreated { get; }
            public DebugMessageEntry(string message, ConsoleColor? color = null, MessageType? type = null, LoggingCategory? category = null)
                {
                Message = message;
                Color = color;
                Type = type;
                TimeCreated = DateTime.Now;
                Category = category?.CategoryName ?? "General";
            }
            }
        #endregion

        #region Dictionaries

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

        private static string ErrorMessage = WarningTriangle + "  " + MessageTypeDict[MessageType.Critical] + "  " + WarningTriangle;

        private static string WarningMessage = DoubleExlamError + " " + MessageTypeDict[MessageType.Warning] + " " + DoubleExlamError;

        private static string GeneralMessage = Asteriks + MessageTypeDict[MessageType.General] + Asteriks;

        #endregion

        }

    }
