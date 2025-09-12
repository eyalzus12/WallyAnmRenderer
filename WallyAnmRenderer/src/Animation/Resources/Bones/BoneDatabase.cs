using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Raylib_cs;
using AbcDisassembler;
using AbcDisassembler.Swf.Tags;
using AbcDisassembler.Instructions;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Bones;

namespace WallyAnmRenderer;

public class BoneDatabase : IBoneDatabase
{
    private BoneDatabase() { }
    public static async ValueTask<BoneDatabase> NewAsync(string brawlPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                return ReadBoneDB(new BoneDatabase(), brawlPath);
            }
            catch (Exception e)
            {
                Rl.TraceLog(TraceLogLevel.Error, "Failed to load bone database: " + e.Message);
                return new BoneDatabase(); // empty db
            }
        });
    }

    private interface IStackValue;
    private readonly record struct ByteValue(byte Val) : IStackValue;
    private readonly record struct UintValue(uint Val) : IStackValue;
    private readonly record struct StringValue(string Val) : IStackValue;
    private readonly record struct BoolValue(bool Val) : IStackValue;

    private static BoneDatabase ReadBoneDB(BoneDatabase db, string brawlPath)
    {
        DoAbcTag tag = AbcUtils.GetDoAbcTag(Path.Combine(brawlPath, "BrawlhallaAir.swf")) ?? throw new Exception("Failed to find abc tag in BrawlhallaAir.swf");
        MethodBodyInfo mb = FindBoneDBMethod(tag.AbcFile) ?? throw new Exception("Failed to identify bone database method in ABC tag");

        int pc = 0;
        Stack<IStackValue> stack = [];

        // interpret the relevant pcode instructions, ignore unimportant ones
        while (pc < mb.Code.Count)
        {
            Instruction ins = mb.Code[pc];
            switch (ins.Name)
            {
                case "pushstring":
                    stack.Push(new StringValue((string)ins.Args[0].Value));
                    break;
                case "pushbyte":
                    stack.Push(new ByteValue((byte)ins.Args[0].Value));
                    break;
                case "convert_u":
                    ByteValue val = (ByteValue)stack.Pop();
                    stack.Push(new UintValue(val.Val));
                    break;
                case "pushtrue":
                    stack.Push(new BoolValue(true));
                    break;
                case "pushfalse":
                    stack.Push(new BoolValue(false));
                    break;
                case "jump":
                    HandleSpecialsLoop(db, mb, ref pc);
                    break;
                case "callpropvoid":
                    uint numArgs = (uint)ins.Args[1].Value;
                    if (numArgs == 2)
                    {
                        UintValue artType = (UintValue)stack.Pop();
                        StringValue name = (StringValue)stack.Pop();
                        db.RegisterBone(name.Val, artType.Val);
                    }
                    else if (numArgs == 4)
                    {
                        BoolValue dir = (BoolValue)stack.Pop();
                        ByteValue boneType = (ByteValue)stack.Pop();
                        UintValue artType = (UintValue)stack.Pop();
                        StringValue name = (StringValue)stack.Pop();
                        db.RegisterBoneWithType(name.Val, artType.Val, boneType.Val, dir.Val);
                    }
                    else if (numArgs == 5)
                    {
                        BoolValue hasRVar = (BoolValue)stack.Pop();
                        BoolValue dir = (BoolValue)stack.Pop();
                        ByteValue boneType = (ByteValue)stack.Pop();
                        UintValue artType = (UintValue)stack.Pop();
                        StringValue name = (StringValue)stack.Pop();
                        db.RegisterBoneWithType(name.Val, artType.Val, boneType.Val, dir.Val, hasRVar.Val);
                    }
                    break;
                default:
                    break;
            }
            pc++;
        }

        return db;
    }

    private static void HandleSpecialsLoop(BoneDatabase db, MethodBodyInfo mb, ref int pc)
    {
        // skip the entire loop and at the end just look at how many specials there would have been
        // by checking the value that is being compared
        while (pc < mb.Code.Count && mb.Code[pc].Name != "iflt") pc++;
        if (pc == mb.Code.Count) throw new Exception("Unexpectedly reached end of code while skipping specials loop");

        Instruction ins = mb.Code[pc - 1];
        if (ins.Name != "pushbyte") throw new Exception($"Unexpected instruction before end of loop ({ins.Name})");
        byte iterations = (byte)ins.Args[0].Value;

        // loop taken from game
        for (int i = 1; i < iterations; ++i)
        {
            string name = i.ToString();
            if (name.Length < 2)
            {
                name = "0" + name;
            }
            db.RegisterBone("a_Special" + name, 2);
        }
        pc++;
    }

    private static MethodBodyInfo? FindBoneDBMethod(AbcFile abc) =>
        abc.MethodBodies.Find(mb => mb.Code.Exists(ins => ins.Name == "pushstring" && (string)ins.Args[0].Value == "a_WeaponCrateReady"));

    private readonly Dictionary<string, ArtTypeEnum> _artTypeDict = [];
    private readonly Dictionary<string, (BoneTypeEnum Type, bool Dir)> _boneTypeDict = [];
    private readonly Dictionary<string, string> _asymSwapDict = [];

    // the game stores each pair as a dictionary, then checks ContainsKey and ContainsValue
    // ContainsValue is slow, so this is better
    private readonly HashSet<string> _hasForearmVariant = [];
    private readonly HashSet<string> _isForearmVariant = [];
    private readonly HashSet<string> _hasShinVariant = [];
    private readonly HashSet<string> _isShinVariant = [];
    private readonly HashSet<string> _hasKatarVariant = [];
    private readonly HashSet<string> _isKatarVariant = [];

    private void RegisterBone(string name, uint artType)
    {
        _artTypeDict[name] = (ArtTypeEnum)artType;
    }

    private void RegisterBoneWithType(string name, uint artType, int boneType, bool dir, bool hasRVar = false)
    {
        _boneTypeDict[name] = ((BoneTypeEnum)boneType, dir);
        if (hasRVar)
        {
            string rVar = name + "R";
            _boneTypeDict[rVar] = ((BoneTypeEnum)boneType, dir);
            if (boneType == 2)
            {
                _hasForearmVariant.Add(name);
                _isForearmVariant.Add(rVar);
            }
            else if (boneType == 6)
            {
                _hasShinVariant.Add(name);
                _isShinVariant.Add(rVar);
            }
            else if (boneType == 12)
            {
                _hasKatarVariant.Add(name);
                _isKatarVariant.Add(rVar);
            }
            RegisterBone(rVar, artType);
        }
        if (name.EndsWith("Right"))
            _asymSwapDict[name] = name[..^"Right".Length];
        else if (name.EndsWith("Left"))
            _asymSwapDict[name] = name[..^"Left".Length];
        RegisterBone(name, artType);
    }

    public bool HasVariantFor(string boneName, BoneTypeEnum boneType) => boneType switch
    {
        BoneTypeEnum.FOREARM => _hasForearmVariant.Contains(boneName),
        BoneTypeEnum.SHIN => _hasShinVariant.Contains(boneName),
        BoneTypeEnum.KATAR => _hasKatarVariant.Contains(boneName),
        _ => false,
    };

    public bool IsVariantFor(string boneName, BoneTypeEnum boneType) => boneType switch
    {
        BoneTypeEnum.FOREARM => _isForearmVariant.Contains(boneName),
        BoneTypeEnum.SHIN => _isShinVariant.Contains(boneName),
        BoneTypeEnum.KATAR => _isKatarVariant.Contains(boneName),
        _ => false,
    };

    public bool TryGetArtType(string boneName, out ArtTypeEnum artType)
    {
        return _artTypeDict.TryGetValue(boneName, out artType);
    }

    public bool TryGetAsymSwap(string boneName, [MaybeNullWhen(false)] out string asymBoneName)
    {
        return _asymSwapDict.TryGetValue(boneName, out asymBoneName);
    }

    public bool TryGetBoneType(string boneName, out BoneTypeEnum boneType, out bool boneDir)
    {
        if (_boneTypeDict.TryGetValue(boneName, out (BoneTypeEnum Type, bool Dir) info))
        {
            boneType = info.Type;
            boneDir = info.Dir;
            return true;
        }
        else
        {
            boneType = 0;
            boneDir = false;
            return false;
        }
    }
}