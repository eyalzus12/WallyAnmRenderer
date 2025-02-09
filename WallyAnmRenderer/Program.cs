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
const uint key = 216619030;
const string brawlhallaPath = "C:/Program Files (x86)/Steam/steamapps/common/Brawlhalla";

const string ANIM_FILE = "Animation_Sword.swf";
const string ANIM_CLASS = "a__1HandRearAnimation";
const string COSTUME_TYPE = "Ahsoka";
const string WEAPON_SKIN_TYPE = "SwordChewbacca";
const string ANIMATION = "AttackSpecialCat3_SwapAhsoka";

Transform2D center = Transform2D.CreateTranslate(INITIAL_SCREEN_WIDTH / 2, 3 * INITIAL_SCREEN_HEIGHT / 4) * Transform2D.CreateScale(0.5, 0.5);
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
        using SepReader weaponSkinTypesReader = Sep.New(',').Reader().FromText(weaponSkinTypes.Split('\n', 2)[1]);
        SepReaderAdapter_WeaponSkinTypes adapter_weaponSkinTypes = new(weaponSkinTypesReader);
        if (adapter_weaponSkinTypes.TryGetCol(WEAPON_SKIN_TYPE, out ICsvRow? weaponSkin))
        {
            gfx = WeaponSkinTypesReader.GetGfxTypeInfo(weaponSkin, adapter_costumeTypes).ToGfxType(gfx, null, skinInfo);
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

    bool finishedLoading = animator.Animate(gfx, ANIMATION, (long)Math.Floor(12 * time), center);
    if (!finishedLoading) Rl.DrawText("Loading...", 0, 0, 30, RlColor.White);

    Rl.EndDrawing();
}

Rl.CloseWindow();