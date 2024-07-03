<h1 align="center">
<img src="https://github.com/realChrisDeBon/ConsoleDebugger/assets/97779307/b4e98abb-75bd-41f1-ad27-2cbac025295f" width="175" height="175" alt="ConsoleDebugger logo">
  
   ConsoleDebugger
</h1>

## Overview

ConsoleDebugger is a lightweight utility designed for console applications that involve multiple asynchronous operations, background processes, and network activities. It provides functionalities to log debug messages, play audible beeps, and dynamically track float variables with a contnious tone, making it useful for debugging and monitoring applications with complex behavior. ConsoleDebugger is ideal for console applications where you have multiple asynchronous methods, background processes, or network operations and need a way to keep track of multiple moving parts. 

## Installation

To use ConsoleDebugger in your C# console application, follow these steps:

1. Install the ConsoleDebugger package via NuGet Package Manager:
```dotnet
dotnet add package ConsoleDebugger --version 1.0.3
```
2. Copy the `ConsoleDebugger.cs` file into your project directory.

3. Include `using ConsoleDebugger.ConsoleDebugger;` or optionally for a more functional approach `using static ConsoleDebugger.ConsoleDebugger;` at the top of your C# files where you want to use ConsoleDebugger functionalities.

## Demonstration (Audio)


https://github.com/realChrisDeBon/ConsoleDebugger/assets/97779307/d46c0960-5f72-4e81-848b-5b458c8f59f2


## Usage

### Logging Debug Messages

You can log debug messages with different colors and message types using the `DebugMessage` function:
```csharp
Result result = SomeNetworkFunction() // example funtion
ConsoleDebugger.DebugMessage("We started the network function.");
if(result == Good){
   //denote network results in blue
   ConsoleDebugger.DebugMessage($"Here's the results {result}", ConsoleColor.Blue);
} else {
   ConsoleDebugger.DebugMessage($"Critical error occurred: {result.Message}", MessageType.Critical);
}
```

### Optional File Logging

You can enable file logging for debug messages by calling the `StartLogging` function:
```csharp
ConsoleDebugger.StartLogging();
```
To stop file logging, use the `StopLogging` function:
```csharp
ConsoleDebugger.StopLogging();
```
File logging is configured in the `LoggingConfiguration` class, where you can set options such as log style (CSV or plain text) and timestamp inclusion.

**Note:** Ensure proper error handling and file management practices when using file logging to avoid potential issues with file access and resources.

### Playing Audible Beeps

You can enqueue requests to play audible beeps with specific pitches and durations using the `DebugBeep` function:

```csharp
ConsoleDebugger.DebugBeep(TonePitch.Do, ToneLength.Short);
ConsoleDebugger.DebugBeep(TonePitch.Re, ToneLength.Short);
ConsoleDebugger.DebugBeep(TonePitch.Mi, ToneLength.Medium);
ConsoleDebugger.DebugBeep(TonePitch.Fa, ToneLength.Medium);
ConsoleDebugger.DebugBeep(TonePitch.Sol, ToneLength.Long);
```

### Tracking Float Variables

You can start tracking float variables dynamically and generate tones based on their values using the `StartTrackingFloat` function:

```csharp
float valueToTrack = 0.0f;
FloatSynthesizer synthesizer = ConsoleDebugger.StartTrackingFloat(ref valueToTrack, 0.0f, 100.0f);
```
or 
```csharp
float valueToTrack = 0.0f;
StartTrackingFloat(ref valueToTrack, 0.0f, 100.0f);
```
As `valueToTrack` fluctuates, it will become more audible the closer to the maximum value that it gets. The closer it gets to the minimum value, the less audible and more quiet it will become. 
This can be useful in scenarios where you may be receiving large quantities of represenative data within a certain range, or may be preforming algorithmic operations, and need some way to better understand how the values are being effected.
## Functions Details

### `DebugMessage(string message)`
Enqueues a basic debug message to the processing queue.

### `DebugMessage(string message, ConsoleColor color)`
Enqueues a debug message with a specified foreground color.

### `DebugMessage(string message, MessageType type)`
Enqueues a debug message with an associated message type (General, Warning, Critical).

### `DebugBeep(TonePitch pitch, ToneLength duration)`
Enqueues a request to play an audible beep with a specified pitch and duration.

### `StartTrackingFloat(ref float target, float minrange, float maxrange) : FloatSynthesizer`
Begins monitoring a float variable, generating a tone whose pitch changes dynamically based on the variable's value within a specified range.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
