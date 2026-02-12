# UnityMetaHaptics

A Unity package for playing Meta .haptics files on gamepad controllers. This package imports `.haptic` files created with Meta Haptics Studio and translates them into gamepad rumble patterns by converting frequency and amplitude data into motor speeds for left and right rumble motors.

## Features

- Import and play Meta `.haptic` files on gamepad controllers
- Automatic asset importing via Unity's ScriptedImporter system
- Advanced playback control with looping and time scaling
- Async/await pattern for playback management
- Motor crossfade curve for smooth frequency-to-motor translation
- Per-device vibration tracking and control
- Editor tools for emphasis processing (Emphasizer)

## Installation

1. Copy the `Packages/studio.shortsleeve.unitymetahaptics` folder into your Unity project's `Packages` directory
2. Unity will automatically recognize and import the package

### Requirements

- Unity 2021.2 or newer
- Unity Input System package

## Quick Start

### 1. Import a .haptic File

Simply drag a `.haptic` file into your Unity project. The `HapticImporter` will automatically convert it into a `HapticClip` asset.

### 2. Setup GamepadHapticsPlayer

Add the `GamepadHapticsPlayer` component to a GameObject in your scene:

```csharp
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using Studio.ShortSleeve.UnityMetaHaptics.Gamepad;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticExample : MonoBehaviour
{
    [SerializeField] private GamepadHapticsPlayer hapticPlayer;
    [SerializeField] private HapticClip hapticClip;

    void Start()
    {
        // Get the current gamepad
        Gamepad gamepad = Gamepad.current;

        if (gamepad != null && hapticClip != null)
        {
            // Create a haptic request
            HapticRequest request = new HapticRequest
            {
                Clip = hapticClip,
                GamepadDevice = gamepad,
                ShouldLoop = false,
                UseFixedTime = false,
                ApplyTimeScale = true
            };

            // Play the haptic
            HapticResponse response = hapticPlayer.Play(request);
        }
    }
}
```

### 3. Configure Motor Crossfade

The `GamepadHapticsPlayer` component requires a `motorCrossfadeCurve` (AnimationCurve) to determine how frequency values map to motor intensity. Configure this in the Inspector:

- Lower frequency values control the low-frequency motor (left rumble)
- Higher frequency values control the high-frequency motor (right rumble)
- The curve should range from 0 to 1 on both axes

## API Reference

### Core Components

#### HapticClip

A ScriptableObject that contains the imported haptic data from a `.haptic` file.

**Properties:**
- `byte[] json` - The raw JSON data from the .haptic file
- `DataModel dataModel` - The parsed data model containing amplitude and frequency envelopes

#### GamepadHapticsPlayer

MonoBehaviour that plays haptic clips on gamepad devices.

**Methods:**
- `HapticResponse Play(HapticRequest request, CancellationToken token = default)` - Plays a haptic clip and returns a response handle
- `bool IsBusy(Gamepad gamepad)` - Check if a specific gamepad is currently playing a haptic
- `void Stop(HapticResponse response)` - Stop a specific haptic playback
- `void StopAll()` - Stop all active haptic playback
- `bool IsValid(HapticResponse response)` - Check if a response handle is still valid

**Inspector Fields:**
- `AnimationCurve motorCrossfadeCurve` - Curve for translating frequency to motor speeds

### Request and Response

#### HapticRequest

Structure for requesting haptic playback.

**Fields:**
- `HapticClip Clip` - The haptic clip to play
- `Gamepad GamepadDevice` - The target gamepad device
- `bool ShouldLoop` - Whether to loop the haptic continuously
- `bool UseFixedTime` - Use FixedUpdate timing instead of Update
- `bool ApplyTimeScale` - Whether to respect Time.timeScale

#### HapticResponse

Handle for controlling an active haptic playback.

**Properties:**
- `long ID` - Unique identifier for this playback
- `Gamepad GamepadDevice` - The gamepad device being used

**Methods:**
- `void Stop()` - Stop this haptic playback
- `GetAwaiter()` - Allows awaiting the completion of the haptic

**Usage:**
```csharp
HapticResponse response = hapticPlayer.Play(request);

// Option 1: Stop manually
response.Stop();

// Option 2: Await completion
await response;
```

### Data Model

#### DataModel

Represents the structure of a Meta `.haptic` file.

**Key Classes:**
- `Version` - File format version (major, minor, patch)
- `Metadata` - Author, editor, source, project, tags, description
- `Signals` - Container for continuous signals
- `SignalContinuous` - Contains amplitude and frequency envelopes
- `Envelopes` - Arrays of amplitude and frequency breakpoints
- `AmplitudeBreakpoint` - Time, amplitude value, and optional emphasis
- `FrequencyBreakpoint` - Time and frequency value

### Interfaces

#### IHapticsPlayer

Interface for haptics player implementations.

**Methods:**
- `void Stop(HapticResponse response)` - Stop a specific playback
- `bool IsValid(HapticResponse response)` - Validate a response handle

## Advanced Usage

### Looping Haptics

```csharp
HapticRequest request = new HapticRequest
{
    Clip = myClip,
    GamepadDevice = Gamepad.current,
    ShouldLoop = true,  // Enable looping
    UseFixedTime = false,
    ApplyTimeScale = true
};

HapticResponse response = hapticPlayer.Play(request);

// Stop after some time
await Awaitable.WaitForSecondsAsync(5f);
response.Stop();
```

### Time Scaling

```csharp
// Respect Time.timeScale (pause menu, slow-mo, etc.)
HapticRequest request = new HapticRequest
{
    Clip = myClip,
    GamepadDevice = Gamepad.current,
    ShouldLoop = false,
    UseFixedTime = false,
    ApplyTimeScale = true  // Haptic speed follows Time.timeScale
};
```

### Fixed Timestep

For physics-synchronized haptics:

```csharp
HapticRequest request = new HapticRequest
{
    Clip = myClip,
    GamepadDevice = Gamepad.current,
    ShouldLoop = false,
    UseFixedTime = true,  // Use FixedUpdate timing
    ApplyTimeScale = true
};
```

### Checking Playback Status

```csharp
// Check if a gamepad is busy
if (hapticPlayer.IsBusy(Gamepad.current))
{
    Debug.Log("Gamepad is currently playing a haptic");
}

// Validate a response
if (hapticPlayer.IsValid(response))
{
    Debug.Log("Response is still active");
}
```

### Awaiting Completion

```csharp
HapticResponse response = hapticPlayer.Play(request);

try
{
    await response;
    Debug.Log("Haptic finished playing");
}
catch (OperationCanceledException)
{
    Debug.Log("Haptic was stopped");
}
```

### Cancellation Tokens

```csharp
CancellationTokenSource cts = new CancellationTokenSource();

HapticResponse response = hapticPlayer.Play(request, cts.Token);

// Cancel from elsewhere
cts.Cancel();
```

## How It Works

### Import Process

1. Unity detects `.haptic` files via the `HapticImporter` (Packages/studio.shortsleeve.unitymetahaptics/Editor/HapticImporter.cs:14)
2. The importer reads the JSON data from the file
3. Data is deserialized into a `DataModel` structure
4. A `HapticClip` ScriptableObject is created with both raw JSON and parsed data
5. The asset is added to the project

### Playback Process

1. `GamepadHapticsPlayer` receives a `HapticRequest`
2. The player extracts amplitude and frequency breakpoints from the clip's DataModel
3. Each frame (or fixed update), the player:
   - Calculates elapsed time
   - Interpolates current amplitude and frequency values from breakpoints
   - Maps frequency (0-1) to motor balance (low vs. high frequency motor)
   - Applies the motor crossfade curve to determine each motor's speed
   - Calls `Gamepad.SetMotorSpeeds()` with calculated values
4. Playback continues until the clip ends (or loops if requested)
5. Motors are reset to 0 when playback completes or is stopped

### Motor Mapping Algorithm

The frequency value (0-1) determines the balance between motors:
- `amountHigh = frequency`
- `amountLow = 1 - frequency`
- `lowMotorSpeed = amplitude * motorCrossfadeCurve.Evaluate(amountLow)`
- `highMotorSpeed = amplitude * motorCrossfadeCurve.Evaluate(amountHigh)`

This approach allows for smooth transitions between rumble motors based on the haptic's frequency envelope.

## Editor Tools

### Emphasizer

The package includes an `Emphasizer` utility (Packages/studio.shortsleeve.unitymetahaptics/Editor/Emphasizer.cs:15) for processing emphasis data in haptic clips. Emphasis allows certain moments in a haptic to be accentuated with amplitude spikes and ducking.

**Note:** Emphasis processing is currently disabled in the importer but can be enabled by uncommenting the code in HapticImporter.cs:34.

## Project Structure

```
Packages/studio.shortsleeve.unitymetahaptics/
├── package.json                          # Package manifest
├── Runtime/
│   ├── Common/                          # Core data structures and interfaces
│   │   ├── DataModel.cs                 # Meta .haptic file format
│   │   ├── HapticClip.cs                # Imported haptic asset
│   │   ├── GamepadRumble.cs             # Gamepad rumble data structure
│   │   ├── IHapticsPlayer.cs            # Player interface
│   │   ├── HapticRequest.cs             # Playback request structure
│   │   └── HapticResponse.cs            # Playback response handle
│   └── Gamepad/
│       └── GamepadHapticsPlayer.cs      # Gamepad haptics implementation
└── Editor/
    ├── HapticImporter.cs                # .haptic file importer
    └── Emphasizer.cs                    # Emphasis processing utility
```

## Comparison with NiceVibrations

This package is inspired by NiceVibrations but offers several improvements:

- More flexible API with request/response pattern
- Better async/await integration with Unity's Awaitable system
- Customizable motor crossfade curves
- Per-device playback tracking
- Cleaner separation between common interfaces and platform implementations
- Support for Unity's modern Input System package

## Credits

- Original NiceVibrations implementation by Lofelt
- Meta Haptics Studio for haptic authoring
- Short Sleeve Studio

## License

See the license file in the package directory.

## Links

- [GitHub Repository](https://github.com/ShortSleeveStudio/UnityMetaHaptics)
- [Meta Haptics Studio](https://www.meta.com/experiences/5149090915136847/)
