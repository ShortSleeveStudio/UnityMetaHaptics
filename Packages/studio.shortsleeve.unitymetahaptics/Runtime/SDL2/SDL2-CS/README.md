# SDL2-CS Bindings

This directory contains the SDL2# (SDL2-CS) C# bindings for SDL2.

## Source

These bindings are from the official SDL2-CS repository:
- **Repository**: https://github.com/flibitijibibo/SDL2-CS
- **Author**: Ethan "flibitijibibo" Lee
- **License**: zlib/libpng (see LICENSE file)

## Files

- `SDL2.cs` - Main SDL2# bindings file containing P/Invoke declarations for SDL2 functions
- `LICENSE` - Original SDL2-CS license (zlib/libpng)
- `README` - Original SDL2-CS README

## Version

These bindings are periodically synced from the upstream SDL2-CS repository. Check the SDL2.cs header for the specific version information.

## Usage

The SDL2# bindings are used by the Unity Meta Haptics SDL2 implementation to access native SDL2 haptic device APIs. All Unity-specific implementation code is in the parent directory.

## Updates

To update these bindings:
1. Pull latest changes from https://github.com/flibitijibibo/SDL2-CS
2. Copy the updated `SDL2.cs` file from the `src/` directory
3. Verify compatibility with the Unity implementation
