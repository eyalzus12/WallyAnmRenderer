global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Swf;
using nietras.SeparatedValues;
using Raylib_cs;
using WallyAnmRenderer;

const int INITIAL_SCREEN_WIDTH = 1280;
const int INITIAL_SCREEN_HEIGHT = 720;
const uint key = 216619030;
const string brawlhallaPath = "C:/Program Files (x86)/Steam/steamapps/common/Brawlhalla";

string game = Path.Join(brawlhallaPath, "Game.swz");
string costumeTypes = SwzUtils.GetFileFromSwz(game, key, "costumeTypes.csv") ?? throw new Exception();

ICsvRow? witch = null;
using (SepReader reader = Sep.New(',').Reader().FromText(costumeTypes.Split('\n', 2)[1]))
{
    foreach (SepReader.Row row in reader)
    {
        if (row["CostumeName"].ToString() == "Rayman")
        {
            witch = new SepRowAdapter(reader.Header, row);
            break;
        }
    }
}

IGfxType gfx = new GfxType()
{
    AnimFile = "Animation_CharacterSelect.swf",
    AnimClass = "a__CharacterSelectAnimation",
    AnimScale = 2,
};
if (witch is not null)
{
    gfx = CostumeTypesCsvReader.GetGfxTypeInfo(witch).ToGfxType(gfx, null);
}

Loader loader = new(brawlhallaPath, key);

AnimationBuilder builder = new(loader);

Transform2D center = Transform2D.CreateTranslate(INITIAL_SCREEN_WIDTH / 2, 3 * INITIAL_SCREEN_HEIGHT / 4);
BoneSpriteWithName[]? sprites = builder.BuildAnim(gfx, "IdleRayman", 0, center);
if (sprites is null) return;

foreach (BoneSpriteWithName sprite in sprites)
{
    Console.WriteLine($"{sprite.SwfFilePath} {sprite.SpriteName}\n{sprite.Transform}\n\n");
}

SpriteToShapeConverter converter = new(loader);
BoneShape[]? shapes = [.. sprites.SelectMany((sprite) => converter.ConvertToShapes(sprite) ?? [])];
if (shapes is null) return;

Rl.SetConfigFlags(ConfigFlags.VSyncHint);
Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, "WallyAnmRenderer");
Rl.SetExitKey(KeyboardKey.Null);

List<(Texture2D texture, Transform2D transform)> actions = [];
foreach (BoneShape bone in shapes)
{
    (Texture2D texture, Transform2D transform) = SwfShapeToTexture.ToTexture(loader, bone.SwfFilePath, bone.ShapeId, bone.AnimScale);
    Rl.SetTextureWrap(texture, TextureWrap.Clamp);

    Console.WriteLine($"{bone.SwfFilePath} {bone.ShapeId}\n{transform}\n{bone.Transform}\n\n");
    transform = bone.Transform * transform;
    uint tint = bone.Tint;
    double opacity = bone.Opacity;

    actions.Add((texture, transform));
}

float time = 0;

while (!Rl.WindowShouldClose())
{
    Rl.BeginDrawing();
    Rl.ClearBackground(RlColor.Black);

    time += Rl.GetFrameTime();

    foreach ((Texture2D texture, Transform2D transform) in actions)
    {
        RaylibUtils.DrawTextureWithTransform(texture, 0, 0, texture.Width, texture.Height, transform);
    }

    Rl.EndDrawing();
}

Rl.CloseWindow();