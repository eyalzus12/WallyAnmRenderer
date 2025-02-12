using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbcDisassembler;
using SwfLib;
using SwfLib.Data;
using SwfLib.Tags;
using SwfLib.Tags.ActionsTags;
using SwfLib.Tags.ControlTags;
using SwfLib.Tags.ShapeTags;

namespace WallyAnmRenderer;

public sealed class SwfFileData
{
    public SwfFile Swf { get; private init; } = null!;
    public Dictionary<string, ushort> SymbolClass { get; private init; } = [];
    public Dictionary<ushort, DefineSpriteTag> SpriteTags { get; private init; } = [];
    public Dictionary<ushort, ShapeBaseTag> ShapeTags { get; private init; } = [];
    public Dictionary<string, uint[]> SpriteA { get; private init; } = [];

    private SwfFileData() { }

    public static SwfFileData CreateFrom(Stream stream)
    {
        SwfFileData swf = new() { Swf = SwfFile.ReadFrom(stream) };
        PopulateSpriteADict(swf);

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
        }

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
        }

        return swf;
    }

    private static void PopulateSpriteADict(SwfFileData swf)
    {
        DoABCTag? tag = swf.Swf.Tags.OfType<DoABCTag>().FirstOrDefault();
        if (tag is null) return;

        AbcFile abc;
        using (MemoryStream ms = new(tag.ABCData))
            abc = AbcFile.Read(ms);

        Dictionary<uint, MethodBodyInfo> bodyDict = [];
        foreach (MethodBodyInfo methodBody in abc.MethodBodies)
        {
            bodyDict[methodBody.Method] = methodBody;
        }

        for (int i = 0; i < abc.Instances.Count; ++i)
        {
            InstanceInfo instance = abc.Instances[i];
            IBaseMultiname cmn = abc.ConstantPool.Multinames[(int)instance.Name];
            if (cmn is not INamedMultiname classMultiname) continue;
            string className = abc.ConstantPool.Strings[(int)classMultiname.Name];

            // find frame1 function
            foreach (TraitInfo trait in instance.Traits)
            {
                if (trait.Kind == TraitType.Method)
                {
                    IBaseMultiname mmn = abc.ConstantPool.Multinames[(int)trait.Name];
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