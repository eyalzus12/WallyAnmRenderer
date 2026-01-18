using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AbcDisassembler;
using AbcDisassembler.Instructions;
using AbcDisassembler.Multinames;
using AbcDisassembler.Traits;
using SwfLib;
using SwfLib.Data;
using SwfLib.Tags;
using SwfLib.Tags.ActionsTags;
using SwfLib.Tags.BitmapTags;
using SwfLib.Tags.ControlTags;
using SwfLib.Tags.ShapeTags;
using SwfLib.Tags.TextTags;

namespace WallyAnmRenderer;

public sealed class SwfFileData
{
    public SwfFile Swf { get; private init; } = null!;
    public Dictionary<string, ushort> SymbolClass { get; } = [];
    public Dictionary<ushort, string> ReverseSymbolClass { get; } = [];
    public Dictionary<ushort, DefineSpriteTag> SpriteTags { get; } = [];
    public Dictionary<ushort, ShapeBaseTag> ShapeTags { get; } = [];
    public Dictionary<ushort, DefineTextBaseTag> TextTags { get; } = [];
    public Dictionary<ushort, DefineEditTextTag> EditTextTags { get; } = [];
    public Dictionary<ushort, BitmapBaseTag> BitmapTags { get; } = [];
    public Dictionary<string, uint[]> SpriteA { get; } = [];

    private SwfFileData() { }

    public static SwfFileData CreateFrom(SwfFile file, CancellationToken ctoken = default)
    {
        ctoken.ThrowIfCancellationRequested();

        SwfFileData swf = new() { Swf = file };
        PopulateSpriteADict(swf, ctoken);
        ctoken.ThrowIfCancellationRequested();

        SymbolClassTag? symbolClass = null;

        //find symbol class
        foreach (SwfTagBase tag in swf.Swf.Tags)
        {
            if (tag is SymbolClassTag symbolClassTag)
            {
                symbolClass = symbolClassTag;
                break;
            }
        }
        if (symbolClass is null)
        {
            throw new Exception("No symbol class in swf");
        }

        foreach (SwfSymbolReference reference in symbolClass.References)
        {
            swf.SymbolClass[reference.SymbolName] = reference.SymbolID;
            swf.ReverseSymbolClass[reference.SymbolID] = reference.SymbolName;
        }

        ctoken.ThrowIfCancellationRequested();

        foreach (SwfTagBase tag in swf.Swf.Tags)
        {
            if (tag is DefineSpriteTag sprite)
            {
                swf.SpriteTags[sprite.SpriteID] = sprite;
            }
            else if (tag is ShapeBaseTag shape)
            {
                swf.ShapeTags[shape.ShapeID] = shape;
            }
            else if (tag is DefineTextBaseTag text)
            {
                swf.TextTags[text.CharacterID] = text;
            }
            else if (tag is DefineEditTextTag editText)
            {
                swf.EditTextTags[editText.CharacterID] = editText;
            }
            else if (tag is BitmapBaseTag bitmap)
            {
                swf.BitmapTags[bitmap.CharacterID] = bitmap;
            }
        }

        return swf;
    }

    private static void PopulateSpriteADict(SwfFileData swf, CancellationToken ctoken = default)
    {
        DoABCTag? tag = swf.Swf.Tags.OfType<DoABCTag>().FirstOrDefault();
        if (tag is null) return;

        ctoken.ThrowIfCancellationRequested();
        AbcFile abc;
        using (MemoryStream ms = new(tag.ABCData))
            abc = AbcFile.Read(ms);
        ctoken.ThrowIfCancellationRequested();

        Dictionary<uint, MethodBodyInfo> bodyDict = [];
        foreach (MethodBodyInfo methodBody in abc.MethodBodies)
        {
            bodyDict[methodBody.Method] = methodBody;
        }

        for (int i = 0; i < abc.Instances.Count; ++i)
        {
            InstanceInfo instance = abc.Instances[i];
            IMultiname cmn = abc.ConstantPool.Multinames[(int)instance.Name];
            if (cmn is not INamedMultiname classMultiname) continue;
            string className = abc.ConstantPool.Strings[(int)classMultiname.Name];

            // find frame1 function
            foreach (TraitInfo trait in instance.Traits)
            {
                if (trait.Kind == TraitType.Method)
                {
                    IMultiname mmn = abc.ConstantPool.Multinames[(int)trait.Name];
                    if (mmn is not INamedMultiname methodMultiname) continue;
                    string methodName = abc.ConstantPool.Strings[(int)methodMultiname.Name];
                    if (methodName != "frame1") continue;

                    MethodTrait methodTrait = (MethodTrait)trait.Trait;
                    if (!bodyDict.TryGetValue(methodTrait.Method, out MethodBodyInfo? methodBody)) continue;

                    List<uint> a = [];
                    foreach (Instruction instruction in methodBody.Code)
                    {
                        if (instruction.Name == "pushint")
                        {
                            int value = (int)instruction.Args[0].Value;
                            a.Add((uint)value);
                        }
                        else if (instruction.Name == "pushuint")
                        {
                            uint value = (uint)instruction.Args[0].Value;
                            a.Add(value);
                        }
                        else if (instruction.Name == "pushshort")
                        {
                            // pushshort takes an int. epic.
                            int value = (int)instruction.Args[0].Value;
                            a.Add((uint)value);
                        }
                        else if (instruction.Name == "pushbyte")
                        {
                            byte value = (byte)instruction.Args[0].Value;
                            a.Add(value);
                        }
                        //tf
                        else if (instruction.Name == "pushdouble")
                        {
                            double value = (double)instruction.Args[0].Value;
                            a.Add((uint)value);
                        }
                    }
                    swf.SpriteA[className] = [.. a];
                }
            }
        }
    }
}

/*
Tags with a CharacterID:
* PlaceObjectBaseTag:
    * PlaceObjectTag
    * PlaceObject2Tag
    * PlaceObject3Tag
* RemoveObjectTag
* DefineScalingGridTag
* BitmapBaseTag:
  * DefineBitsTag
  * DefineBitsJPEG2Tag
  * DefineBitsJPEG3Tag
  * DefineBitsJPEG4Tag
  * DefineBitsLosslessTag
  * DefineBitsLossless2Tag
* DefineMorphShapeTag
* DefineMorphShape2Tag
* DefineTextBaseTag:
  * DefineTextTag
  * DefineText2Tag
* DefineEditTextTag

Tags with a ShapeID:
* ShapeBaseTag:
  * DefineShapeTag
  * DefineShape2Tag
  * DefineShape3Tag
  * DefineShape4Tag

Tags with a SpriteID:
* DefineSprite
* DoInitActionTag

So we are currently missing:
* DefineScalingGridTag
* DefineMorphShapeTag
* DefineMorphShape2Tag
* DoInitActionTag
*/