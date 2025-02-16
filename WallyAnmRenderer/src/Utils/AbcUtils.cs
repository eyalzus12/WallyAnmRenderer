using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbcDisassembler;
using SwfLib;
using SwfLib.Tags.ActionsTags;

namespace WallyAnmRenderer;

public static class AbcUtils
{
    public static uint? FindDecryptionKeyFromPath(string bhairPath)
    {
        DoABCDefineTag? tag = GetDoABCDefineTag(bhairPath);
        if (tag is null) return null;

        AbcFile abc;
        using (MemoryStream ms = new(tag.ABCData))
            abc = AbcFile.Read(ms);

        return FindDecryptionKey(abc);
    }

    private static DoABCDefineTag? GetDoABCDefineTag(string swfPath)
    {
        SwfFile swf;
        using (FileStream stream = File.OpenRead(swfPath))
            swf = SwfFile.ReadFrom(stream);
        return swf.Tags.OfType<DoABCDefineTag>().FirstOrDefault();
    }

    private static uint? FindDecryptionKey(AbcFile abc)
    {
        foreach (MethodBodyInfo mb in abc.MethodBodies)
        {
            List<int> getlexPos = FindGetlexPositions(abc.ConstantPool, "ANE_RawData", mb.Code);

            for (int i = 0; i < getlexPos.Count; i++)
            {
                int callpropvoidPos = getlexPos[i] == getlexPos[^1]
                    ? FindCallpropvoidPos(abc.ConstantPool, "Init", mb.Code[getlexPos[i]..])
                    : FindCallpropvoidPos(abc.ConstantPool, "Init", mb.Code[getlexPos[i]..getlexPos[i + 1]]);

                if (callpropvoidPos != -1)
                    return FindLastPushuintArg(mb.Code[0..callpropvoidPos]);
            }
        }

        return null;
    }

    private static List<int> FindGetlexPositions(CPoolInfo cpool, string lexName, List<Instruction> code) => code
        .Select((o, i) => new { Item = o, Index = i })
        .Where(o => o.Item.Name == "getlex" && o.Item.Args[0].Value is INamedMultiname name && cpool.Strings[(int)name.Name] == lexName)
        .Select(o => o.Index)
        .ToList();

    private static int FindCallpropvoidPos(CPoolInfo cpool, string methodName, List<Instruction> code) => code
        .FindIndex(i => i.Name == "callpropvoid" && i.Args[0].Value is INamedMultiname named && cpool.Strings[(int)named.Name] == methodName);

    private static uint? FindLastPushuintArg(List<Instruction> ins) => (uint?)ins
        .LastOrDefault(ins => ins.Name == "pushuint")?.Args[0].Value;
}