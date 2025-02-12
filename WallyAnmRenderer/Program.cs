global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
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
const string? COSTUME_TYPE = "Armortaur";
const string? WEAPON_SKIN_TYPE = "AxeArmortaur";
const string ANIMATION = "IdleMinotaur";
const string? COLOR_SCHEME = "Brawlhalloween";

static SepReader readerFromText(string text)
{
    SepReaderOptions reader = new(Sep.New(',')) { DisableColCountCheck = true };
    return reader.FromText(text.Split('\n', 2)[1]);
}

string game = Path.Join(brawlhallaPath, "Game.swz");
Dictionary<string, string> gameFiles = SwzUtils.GetFilesFromSwz(game, key, ["costumeTypes.csv", "weaponSkinTypes.csv", "ColorSchemeTypes.xml", "colorExceptionTypes.csv"]);

string costumeTypesContent = gameFiles["costumeTypes.csv"];
CostumeTypes costumeTypes;
using (SepReader reader = readerFromText(costumeTypesContent))
    costumeTypes = new(reader);

string weaponSkinTypesContent = gameFiles["weaponSkinTypes.csv"];
WeaponSkinTypes weaponSkinTypes;
using (SepReader reader = readerFromText(weaponSkinTypesContent))
    weaponSkinTypes = new(reader, costumeTypes);

string colorSchemeTypesContent = gameFiles["ColorSchemeTypes.xml"];
Dictionary<string, ColorScheme> colorSchemes = [];
XElement colorSchemeElement = XElement.Parse(colorSchemeTypesContent);
foreach (XElement colorScheme in colorSchemeElement.Elements())
{
    string name = colorScheme.Attribute("ColorSchemeName")?.Value ?? throw new Exception();
    if (name == "Template") continue;
    colorSchemes[name] = new(colorScheme);
}

string colorExceptionTypesContent = gameFiles["colorExceptionTypes.csv"];
ColorExceptionTypes colorExceptionTypes;
using (SepReader reader = readerFromText(colorExceptionTypesContent))
    colorExceptionTypes = new(reader);

IGfxType gfx = new GfxType()
{
    AnimFile = ANIM_FILE,
    AnimClass = ANIM_CLASS,
    AnimScale = 2,
};

LogCallback.Init();

IGfxType PopulateGfx(IGfxType gfx, string? costumeType, string? weaponSkinType, string? colorScheme)
{
    ColorScheme? scheme = null;
    if (colorScheme is not null)
    {
        if (!colorSchemes.TryGetValue(colorScheme, out scheme))
        {
            throw new ArgumentException($"Invalid color scheme {colorScheme}");
        }
    }

    CostumeTypesGfxInfo? skinInfo = null;
    if (costumeType is not null)
    {
        if (costumeTypes.GfxInfo.TryGetValue(costumeType, out skinInfo))
        {
            gfx = skinInfo.ToGfxType(gfx, scheme, colorExceptionTypes);
        }
        else
        {
            throw new ArgumentException($"Invalid costume type {costumeType}");
        }
    }

    if (weaponSkinType is not null)
    {
        if (weaponSkinTypes.GfxInfo.TryGetValue(weaponSkinType, out WeaponSkinTypesGfxInfo? weaponSkin))
        {
            gfx = weaponSkin.ToGfxType(gfx, scheme, colorExceptionTypes, skinInfo);
        }
        else
        {
            throw new ArgumentException($"Invalid weapon skin type {weaponSkinType}");
        }
    }

    return gfx;
}
gfx = PopulateGfx(gfx, COSTUME_TYPE, WEAPON_SKIN_TYPE, COLOR_SCHEME);

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