# SDL2 Haptics Implementation

This directory contains the SDL2-based haptics implementation for Unity Meta Haptics.

## Directory Structure

```
SDL2/
├── SDL2-CS/                      # SDL2# (SDL2-CS) C# bindings (third-party)
│   ├── SDL2.cs                   # Main SDL2 P/Invoke bindings
│   ├── LICENSE                   # SDL2-CS license (zlib/libpng)
│   └── README.md                 # SDL2-CS documentation
│
├── NativeLibraries/              # Native SDL2 libraries (.dylib, .dll, .so)
│
├── SDL2HapticsSystem.cs          # Unity MonoBehaviour lifecycle manager
├── SDL2HapticsManager.cs         # Singleton manager for SDL2 subsystem
├── SDL2HapticsPlayer.cs          # Plays haptic clips via SDL2
├── SDL2DeviceMapper.cs           # Maps Unity gamepads to SDL2 devices
├── SDL2HapticDevice.cs           # Wrapper for SDL2 haptic device handles
├── SDL2Effect.cs                 # Manages uploaded SDL2 haptic effects
├── SDL2EffectBuilder.cs          # Builds SDL_HapticEffect structs
├── BreakpointToSampleConverter.cs # Converts breakpoints to sample data
└── UnityMetaHaptics.SDL2.asmdef  # Assembly definition
```

## Overview

The SDL2 implementation provides hardware-driven haptic playback using SDL2's haptic API. This offers more precise timing and hardware-accelerated looping compared to software-based approaches.

### Key Components

- **SDL2HapticsSystem**: Main entry point. Add this MonoBehaviour to your scene to initialize SDL2 haptics.
- **SDL2HapticsPlayer**: Implements `IHapticsPlayer` for playing `HapticClip` assets via SDL2.
- **SDL2DeviceMapper**: Maps Unity Input System `Gamepad` devices to SDL2 haptic devices using VID/PID matching.
- **SDL2HapticsManager**: Low-level singleton managing SDL2 subsystem initialization and device enumeration.

### Usage

```csharp
// Option 1: Automatic setup (recommended)
// Add SDL2HapticsSystem component to a GameObject in your scene
// It will auto-initialize on Awake

// Option 2: Manual setup
SDL2HapticsSystem.CreateInstance();

// Play a haptic clip
SDL2HapticsPlayer player = new SDL2HapticsPlayer();
HapticResponse response = player.Play(hapticClip, Gamepad.current);

// Stop playback
response.Stop();
```

## Dependencies

- **SDL2 native library**: Pre-built binaries are included in `NativeLibraries/` for macOS, Windows, and Linux
- **SDL2-CS bindings**: Located in `SDL2-CS/` subdirectory
- **Unity Input System**: Required for gamepad device mapping

## Platform Support

- ✅ macOS (via SDL2 dylib)
- ✅ Windows (via SDL2 dll)
- ✅ Linux (via SDL2 so)
- ❌ iOS (use CoreHaptics instead)
- ❌ Android (use Android Vibrator API instead)
- ❌ WebGL (not supported)

## Architecture Notes

The SDL2 implementation uses a hybrid approach:
1. **Event-driven hot-plug detection**: Listens to Unity Input System device events
2. **Lazy validation**: Only validates device handles when actually used
3. **VID/PID matching**: Reliably pairs Unity gamepads with SDL2 devices using hardware IDs

See [SDL2_IMPLEMENTATION_PLAN.md](/SDL2_IMPLEMENTATION_PLAN.md) for detailed architecture documentation.
