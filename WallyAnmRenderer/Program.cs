global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
using System.Collections.Generic;
using System.IO;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;
using nietras.SeparatedValues;
using Raylib_cs;
using WallyAnmRenderer;

const int INITIAL_SCREEN_WIDTH = 1280;
const int INITIAL_SCREEN_HEIGHT = 720;
const int ANIM_FPS = 24;

const uint key = 216619030;
const string brawlhallaPath = "C:/Program Files (x86)/Steam/steamapps/common/Brawlhalla";

const string ANIM_FILE = "Animation_CharacterSelect.swf";
const string ANIM_CLASS = "a__CharacterSelectAnimation";
const string? COSTUME_TYPE = "Megaman";
const string? WEAPON_SKIN_TYPE = "PistolMegaman";
const string ANIMATION = "SelectedMegaman";


static SepReader readerFromText(string text)
{
    SepReaderOptions reader = new(Sep.New(',')) { DisableColCountCheck = true };
    return reader.FromText(text.Split('\n', 2)[1]);
}

string game = Path.Join(brawlhallaPath, "Game.swz");
Dictionary<string, string> initFiles = SwzUtils.GetFilesFromSwz(game, key, ["costumeTypes.csv", "weaponSkinTypes.csv"]);

string costumeTypesContent = initFiles["costumeTypes.csv"];
CostumeTypes costumeTypes;
using (SepReader reader = readerFromText(costumeTypesContent))
    costumeTypes = new(reader);

string weaponSkinTypesContent = initFiles["weaponSkinTypes.csv"];
WeaponSkinTypes weaponSkinTypes;
using (SepReader reader = readerFromText(weaponSkinTypesContent))
    weaponSkinTypes = new(reader, costumeTypes);

IGfxType gfx = new GfxType()
{
    AnimFile = ANIM_FILE,
    AnimClass = ANIM_CLASS,
    AnimScale = 2,
};

LogCallback.Init();

IGfxType PopulateGfx(IGfxType gfx, string? costumeType, string? weaponSkinType)
{
    CostumeTypesGfxInfo? skinInfo = null;
    if (costumeType is not null &&
        costumeTypes.GfxInfo.TryGetValue(costumeType, out skinInfo))
    {
        gfx = skinInfo.ToGfxType(gfx, null);
    }

    if (weaponSkinType is not null &&
        weaponSkinTypes.GfxInfo.TryGetValue(weaponSkinType, out WeaponSkinTypesGfxInfo? weaponSkin))
    {
        gfx = weaponSkin.ToGfxType(gfx, null, skinInfo);
    }

    return gfx;
}
gfx = PopulateGfx(gfx, COSTUME_TYPE, WEAPON_SKIN_TYPE);

Transform2D center = Transform2D.CreateTranslate(INITIAL_SCREEN_WIDTH / 2, 3 * INITIAL_SCREEN_HEIGHT / 4) * Transform2D.CreateScale(0.75, 0.75);
Animator animator = new(brawlhallaPath, key);

Rl.SetConfigFlags(ConfigFlags.VSyncHint);
Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, "WallyAnmRenderer");
Rl.SetExitKey(KeyboardKey.Null);

float time = 0;

while (!Rl.WindowShouldClose())
{
    Rl.BeginDrawing();
    Rl.ClearBackground(RlColor.Black);

    time += Rl.GetFrameTime();

    bool finishedLoading = animator.Animate(gfx, ANIMATION, (long)Math.Floor(ANIM_FPS * time), center);
    if (!finishedLoading) Rl.DrawText("Loading...", 0, 0, 30, RlColor.White);

    Rl.EndDrawing();
}

Rl.CloseWindow();