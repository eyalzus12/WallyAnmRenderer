global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using WallyAnmRenderer;

PathPreferences prefs = await PathPreferences.Load();
Editor editor = new(prefs);
editor.Run();