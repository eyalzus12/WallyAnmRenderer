global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
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
const string COSTUME_TYPE = "Ahsoka";
const string? WEAPON_SKIN_TYPE = "KatarAhsoka";
const string ANIMATION = "SelectedAhsoka";

LogCallback.Init();

Transform2D center = Transform2D.CreateTranslate(INITIAL_SCREEN_WIDTH / 2, 3 * INITIAL_SCREEN_HEIGHT / 4) * Transform2D.CreateScale(0.75, 0.75);
Animator animator = new(brawlhallaPath, key);

string game = Path.Join(brawlhallaPath, "Game.swz");
string costumeTypes = SwzUtils.GetFileFromSwz(game, key, "costumeTypes.csv") ?? throw new Exception();
string weaponSkinTypes = SwzUtils.GetFileFromSwz(game, key, "weaponSkinTypes.csv") ?? throw new Exception();

IGfxType gfx = new GfxType()
{
    AnimFile = ANIM_FILE,
    AnimClass = ANIM_CLASS,
    AnimScale = 2,
};

using (SepReader costumeTypesReader = Sep.New(',').Reader().FromText(costumeTypes.Split('\n', 2)[1]))
{
    SepReaderAdapter_CostumeTypes adapter_costumeTypes = new(costumeTypesReader);
    if (adapter_costumeTypes.TryGetCol(COSTUME_TYPE, out ICsvRow? skin))
    {
        CostumeTypesGfxInfo skinInfo = CostumeTypesCsvReader.GetGfxTypeInfo(skin);
        gfx = skinInfo.ToGfxType(gfx, null);
        if (WEAPON_SKIN_TYPE is not null)
        {
            using SepReader weaponSkinTypesReader = Sep.New(',').Reader().FromText(weaponSkinTypes.Split('\n', 2)[1]);
            SepReaderAdapter_WeaponSkinTypes adapter_weaponSkinTypes = new(weaponSkinTypesReader);
            if (adapter_weaponSkinTypes.TryGetCol(WEAPON_SKIN_TYPE!, out ICsvRow? weaponSkin))
            {
                gfx = WeaponSkinTypesReader.GetGfxTypeInfo(weaponSkin, adapter_costumeTypes).ToGfxType(gfx, null, skinInfo);
            }
        }
    }
}

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