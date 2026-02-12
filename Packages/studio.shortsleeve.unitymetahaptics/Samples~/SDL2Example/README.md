# SDL2 Haptics Example

This example demonstrates how to use the SDL2 haptics system with Unity Input System gamepads.

## Setup

1. **Add SDL2HapticsSystem to scene:**
   - Option A: Create empty GameObject and add `SDL2HapticsSystem` component
   - Option B: Let it auto-create via `SDL2HapticsSystem.CreateInstance()` (shown in example)

2. **Configure SDL2HapticsSystem:**
   - `Auto Initialize`: Enable to automatically initialize SDL2 on Awake
   - `Don't Destroy On Load`: Enable to persist across scenes
   - `Verbose Logging`: Enable for debug output

3. **Add example script:**
   - Add `SDL2HapticsExample` component to a GameObject
   - Assign haptic clips in the Inspector
   - Connect a gamepad

## Three Usage Patterns

### 1. HapticRequest (Standard Unity Pattern)
```csharp
HapticRequest request = new HapticRequest
{
    Clip = myClip,
    GamepadDevice = myGamepad,
    ShouldLoop = false
};

HapticResponse response = player.Play(request);
```

**When to use**: Familiar Unity API pattern, easy migration from GamepadHapticsPlayer

### 2. Gamepad Convenience API (Simplest)
```csharp
HapticResponse response = player.Play(
    clip: myClip,
    gamepad: myGamepad,
    shouldLoop: true,
    amplitudeScale: 0.8f
);
```

**When to use**: Quick and simple, handles device mapping automatically

### 3. Direct SDL2 Device Access (Advanced)
```csharp
SDL2HapticDevice device = SDL2HapticsSystem.Instance.GetDeviceForGamepad(myGamepad);

// Query capabilities
Debug.Log($"Max Effects: {device.MaxEffects}");
Debug.Log($"Supports Custom: {device.SupportsCustomWaveforms}");

// Play with full control
HapticResponse response = player.Play(
    clip: myClip,
    device: device,
    shouldLoop: false,
    amplitudeScale: 1.0f
);
```

**When to use**: Need device capabilities info, manual device management, performance optimization

## Controls

- **A Button**: Play impact haptic (HapticRequest)
- **B Button**: Toggle looping haptic (Gamepad API)
- **X Button**: Play emphasis haptic (Advanced SDL2 API)
- **Y Button**: Stop all haptics

## Hot-Plug Support

The system automatically handles controller hot-plugging via Unity Input System events:
- Plug in controller → Automatically detected and mapped
- Unplug controller → Cleanly disposed, haptics stopped
- Reconnect → Re-mapped on next use

## Device Mapping

The SDL2DeviceMapper uses VID/PID (Vendor ID/Product ID) matching to reliably map Unity gamepads to SDL2 haptic devices:

1. **Automatic**: Unity Input System events trigger device refresh
2. **Lazy validation**: Devices validated on access, remapped if invalid
3. **Efficient**: ~1-2ms cost only when devices change (rare)

## Troubleshooting

### No haptics playing
- Check SDL2HapticsSystem is initialized (Debug → Log System Info context menu)
- Verify controller supports haptics (not all gamepads have rumble motors)
- Check device mapping: `SDL2HapticsSystem.Instance.Mapper.GetMappingInfo()`

### Controller not detected
- Make sure Unity Input System is enabled in Project Settings
- Verify controller works in Unity (test with Input System debugger)
- Check SDL2 detects joystick: SDL may need joystick subsystem initialization

### Looping has gap/discontinuity
- This is the problem SDL2 solves! With SDL2, looping should be seamless
- If you still hear gaps, the clip may have silence at start/end
- Check clip duration and ensure amplitude doesn't ramp to zero at edges

## Performance Notes

- **Zero overhead when idle**: No polling, event-driven
- **Device mapping cost**: ~1-2ms when controller plugged/unplugged (rare)
- **Playback cost**: Hardware-driven, no per-frame overhead
- **Memory**: Pre-converted samples stored in HapticClip asset

## Next Steps

- Try different haptic clips
- Experiment with amplitude scaling
- Test with multiple controllers
- Explore device capabilities via SDL2HapticDevice properties
