# Wally Anm Renderer. An animation renderer for Brawlhalla.

**Currently under development.**

#### Present features:
* Load any .anm files and pick an animation.
* Select a legend skin, a weapon skin, and a color.
* A full implementation of the game's animation system.
* Pause, frame step, and seeing exact animation timeline.
* Listing the sprites and their data.

#### Upcoming features:
* Exporting as svg/png.
* Rendering of sidekicks, companions, and podiums.
* Support for animation loop points.
* Translating internal names.
* Correct timing for attack animations.

#### Unsupported swf features:
* ColorTransform (rarely used)
* Bitmap fill styles (rarely used)
* LineStyle2-exclusive features (not used by the game(?))

## Download
Download the latest release [here](https://github.com/eyalzus12/WallyAnmRenderer/releases/latest)

If the program doesn't launch on windows, you may need to install the [latest Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

### Building from source

make sure you have _git_ installed.

download the code:

`git clone --recurse-submodules https://github.com/eyalzus12/WallyAnmRenderer.git`

run the project (inside the WallyAnmRenderer folder created by git clone):

`dotnet run --project WallyAnmRenderer`

## Requirements

- .NET 8 SDK (if building from source)
- On Windows: latest Visual C++ Redistributable
- A Brawlhalla installation

## Submodules

- BrawlhallaAnimLib - C# library implementing Brawlhalla's animation logic.

- WallyAnmSpinzor - C# library for parsing Brawlhalla .anm files.

- BrawlhallaSwz - C# library for encrypting and decrypting Brawlhalla .swz files.

- SwiffCheese - C# library for converting flash vector graphics into svg.

- AbcDisassembler - C# library for parsing actionscript bytecode. Used to find the swz decryption key and the .a array for sprites.

## Package Dependencies

This list includes the dependencies from submodules.

- Svg.Skia (2.0.0.4) - Library based on SkiaSharp (2.88.8) for Svg rendering.

- NativeFileDialogSharp (0.5.0) - C# bindings for nativefiledialog, a C library for opening the platform's default file explorer dialog.

- Raylib-cs (7.0.0) - C# bindings for Raylib, a C rendering library.

- ImGui.NET (1.91.6.1) - C# bindings for ImGui, a C++ ui library.

- rlImgui-cs (3.1.0) - C# library for bridging between Raylib-cs and ImGui.NET.

- SwfLib (1.0.5) - C# library for parsing .swf files.

- OneOf (3.0.271) - C# tagged union library.

- Sep (0.9.0) - C# csv parsing library.