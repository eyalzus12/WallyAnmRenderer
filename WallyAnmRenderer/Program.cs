global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using Raylib_cs;
using WallyAnmRenderer;

const int INITIAL_SCREEN_WIDTH = 1280;
const int INITIAL_SCREEN_HEIGHT = 720;
const uint key = 216619030;
const string brawlhallaPath = "C:/Program Files (x86)/Steam/steamapps/common/Brawlhalla";

GfxType gfx = new()
{
    AnimFile = "Animation_CharacterSelect.swf",
    AnimClass = "a__CharacterSelectAnimation",
    AnimScale = 2,
};

Loader loader = new(brawlhallaPath, key);

AnimationBuilder builder = new(loader);

BoneSpriteWithName[]? sprites = builder.BuildAnim(gfx, "IdleWitch", 1, Transform2D.IDENTITY);
if (sprites is null) return;

SpriteToShapeConverter converter = new(loader);
BoneShape[]? shapes = [.. sprites.SelectMany((sprite) => converter.ConvertToShapes(sprite) ?? [])];
if (shapes is null) return;

Rl.SetConfigFlags(ConfigFlags.VSyncHint);
Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, "WallyAnmRenderer");
Rl.SetExitKey(KeyboardKey.Null);

List<(Texture2D texture, Transform2D transform)> actions = [];
foreach (BoneShape shape in shapes)
{
    (Texture2D texture, Transform2D transform) = SwfShapeToTexture.ToTexture(loader, shape.SwfFilePath, shape.ShapeId, shape.AnimScale);
    Console.WriteLine(texture.Id);
    transform = shape.Transform * transform;
    uint tint = shape.Tint;
    double opacity = shape.Opacity;

    actions.Add((texture, transform));
}

while (!Rl.WindowShouldClose())
{
    Rl.BeginDrawing();
    Rl.ClearBackground(RlColor.Black);

    foreach ((Texture2D texture, Transform2D transform) in actions)
    {
        RaylibUtils.DrawTextureWithTransform(texture, INITIAL_SCREEN_WIDTH / 2, INITIAL_SCREEN_HEIGHT / 2, texture.Width, texture.Height, transform);
    }

    Rl.EndDrawing();
}

Rl.CloseWindow();