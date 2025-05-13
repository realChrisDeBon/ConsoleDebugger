<h1 align="center">
<img src="https://github.com/realChrisDeBon/ConsoleDebugger/assets/97779307/b4e98abb-75bd-41f1-ad27-2cbac025295f" width="175" height="175" alt="ConsoleDebugger logo">
  
   ConsoleDebugger
</h1>

<h3 align="center">
Quick, simple, easy logging and debugging.
</h3>

## Overview

ConsoleDebugger is a lightweight utility for console applications, providing robust logging and debug message management. It is ideal for applications with multiple asynchronous methods, background processes, or network operations. As of v1.0.8, advanced features such as audible beeps and float tracking are now available as separate extension packages, reducing the core package size and dependencies for users who only need basic logging.

## Installation

To use ConsoleDebugger in your C# console application:

1. Install the core ConsoleDebugger package via NuGet:
```dotnet
dotnet add package ConsoleDebugger --version 1.0.8
```
2. Add the following using directive in your C# files:
```csharp
using ConsoleDebugger.ConsoleDebugger;
```
Or, for a more functional approach:
```csharp
using static ConsoleDebugger.ConsoleDebugger;
```

### Optional Extensions

- **Audible Beeps:**  
  Install `ConsoleDebugger.Beeps` for beep/tone support:
  ```dotnet
  dotnet add package ConsoleDebugger.Beeps --version 1.0.8
  ```
  ```csharp
  using ConsoleDebugger.Beeps;
  ```

- **Float Tracking:**  
  Install `ConsoleDebugger.FloatTracker` for auditory float tracking:
  ```dotnet
  dotnet add package ConsoleDebugger.FloatTracker --version 1.0.8
  ```
  ```csharp
  using ConsoleDebugger.FloatTracker;
  ```

## Usage

### Logging Debug Messages

Log debug messages with different colors and message types:
```csharp
Result result = SomeNetworkFunction(); // example function
ConsoleDebugger.DebugMessage("We started the network function.");
if(result == Good){
   ConsoleDebugger.DebugMessage($"Here's the result {result.ToString()}", ConsoleColor.Blue);
} else {
   ConsoleDebugger.DebugMessage($"Critical error occurred: {result.Message}", MessageType.Critical);
}
```

#### Logging Categories

You can define and control logging categories:
```csharp
var netCategory = new LoggingCategory("Network");
ConsoleDebugger.AddLoggingCategory(netCategory);
ConsoleDebugger.DebugMessage("Network started", netCategory);
```

You can also control which logging categories are shown in the console output. By default, all categories are considered enabled. Use `ActivateLoggingCategory(category)` to enable console output for a category, or `DeactivateLoggingCategory(category)` to suppress it so the category no longer outputs to the console. 

**Note:** Logging categories are always included in the file log if file logging is enabled, regardless of their console output status.

### Optional File Logging

You can log messages to a file in either `.txt` or `.csv` format. By default, logs include the message, but you can configure additional options such as including timestamps, logging categories, and message types. The output format and included fields are controlled via the `LoggingConfiguration` class.

- **.txt format:** Plain text log entries, optionally with timestamp, category, and message type.
- **.csv format:** Comma-separated values, suitable for importing into spreadsheets or analysis tools. Each field (timestamp, category, type, message) appears as a column if enabled.

Enable or disable file logging:
```csharp
ConsoleDebugger.StartLogging();
// ... your code ...
ConsoleDebugger.StopLogging();
```
Configure logging style and options via the `LoggingConfiguration` class, for example:
```csharp
var config = new LoggingConfiguration {
    IncludeTimestamp = true,
    IncludeCategory = true,
    IncludeMessageType = true,
    LogFormat = LogFormat.Csv // or LogFormat.Txt
};
ConsoleDebugger.SetLoggingConfiguration(config);
```

### Audible Beeps (Extension)

To use beeps, ensure you have installed the `ConsoleDebugger.Beeps` package and initialized beeps:
```csharp
ConsoleDebugger.InitializeBeeps();
ConsoleDebugger.DebugBeep(TonePitch.Do, ToneLength.Short);
ConsoleDebugger.DebugBeep(TonePitch.Re, ToneLength.Short);
```

### Tracking Float Variables (Extension)

To use float tracking, ensure you have installed the `ConsoleDebugger.FloatTracker` package and initialized float tracking:
```csharp
ConsoleDebugger.InitializeFloatTracking();
float valueToTrack = 0.0f;
var synthesizer = ConsoleDebugger.StartTrackingFloat(ref valueToTrack, 0.0f, 100.0f);
```
As `valueToTrack` changes, an auditory tone will represent its value within the specified range.

## Function Details

### Core

- `DebugMessage(string message, LoggingCategory category = default)`
- `DebugMessage(string message, ConsoleColor color, LoggingCategory category = default)`
- `DebugMessage(string message, MessageType type, LoggingCategory category = default)`
- `StartLogging()`, `StopLogging()`
- Logging category management: `AddLoggingCategory`, `RemoveLoggingCategory`, `ActivateLoggingCategory`, `DeactivateLoggingCategory`, `LoggingCategoryActive`

### Beeps Extension

- `DebugBeep(TonePitch pitch, ToneLength duration)`
- `InitializeBeeps()`

### Float Tracker Extension

- `StartTrackingFloat(ref float target, float minrange, float maxrange) : FloatSynthesizer`
- `InitializeFloatTracking()`

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
