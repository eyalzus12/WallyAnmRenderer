global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
using System.Collections.Generic;
using System.IO;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Reading;
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
const string? COSTUME_TYPE = "DarthMaul";
const string? WEAPON_SKIN_TYPE = "SpearDarthMaul";
const string ANIMATION = "SelectedDarthMaul";

LogCallback.Init();

IGfxType gfx = new GfxType()
{
    AnimFile = ANIM_FILE,
    AnimClass = ANIM_CLASS,
    AnimScale = 2,
};
static IGfxType PopulateGfx(IGfxType gfx, string? costumeType, string? weaponSkinType)
{
    string game = Path.Join(brawlhallaPath, "Game.swz");
    Dictionary<string, string> initFiles = SwzUtils.GetFilesFromSwz(game, key, ["costumeTypes.csv", "weaponSkinTypes.csv"]);
    string costumeTypes = initFiles["costumeTypes.csv"];
    string weaponSkinTypes = initFiles["weaponSkinTypes.csv"];

    static SepReader readerFromText(string text)
    {
        SepReaderOptions reader = new(Sep.New(',')) { DisableColCountCheck = true };
        return reader.FromText(text.Split('\n', 2)[1]);
    }

    CostumeTypesGfxInfo? skinInfo = null;
    if (costumeType is not null)
    {
        using SepReader costumeTypesReader = readerFromText(costumeTypes);
        SepReaderAdapter_CostumeTypes adapter_costumeTypes = new(costumeTypesReader);
        if (adapter_costumeTypes.TryGetCol(costumeType, out ICsvRow? skin))
        {
            skinInfo = CostumeTypesCsvReader.GetGfxTypeInfo(skin);
            gfx = skinInfo.ToGfxType(gfx, null);
        }
    }

    if (weaponSkinType is not null)
    {
        using SepReader costumeTypesReader = readerFromText(costumeTypes);
        SepReaderAdapter_CostumeTypes adapter_costumeTypes = new(costumeTypesReader);
        using SepReader weaponSkinTypesReader = readerFromText(weaponSkinTypes);
        SepReaderAdapter_WeaponSkinTypes adapter_weaponSkinTypes = new(weaponSkinTypesReader);
        if (adapter_weaponSkinTypes.TryGetCol(weaponSkinType, out ICsvRow? weaponSkin))
        {
            gfx = WeaponSkinTypesReader.GetGfxTypeInfo(weaponSkin, adapter_costumeTypes).ToGfxType(gfx, null, skinInfo);
        }
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