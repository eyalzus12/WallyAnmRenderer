using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BrawlhallaAnimLib.Anm;
using WallyAnmSpinzor;
using WallyAnmSpinzor.Version_904;

namespace WallyAnmRenderer;

public readonly struct AnmClassAdapter(AnmClass anmClass) : IAnmClass
{
    public bool TryGetAnimation(string animationName, [NotNullWhen(true)] out IAnmAnimation? animation)
    {
        if (anmClass.Animations.TryGetValue(animationName, out AnmAnimation? anmAnimation))
        {
            animation = new AnmAnimationAdapter(anmAnimation);
            return true;
        }
        animation = null;
        return false;
    }
}

public readonly struct AnmAnimationAdapter(AnmAnimation anmAnimation) : IAnmAnimation
{
    public uint LoopStart => anmAnimation.LoopStart;
    public uint RecoveryStart => anmAnimation.RecoveryStart;
    public uint FreeStart => anmAnimation.FreeStart;
    public uint PreviewFrame => anmAnimation.PreviewFrame;
    public uint BaseStart => anmAnimation.BaseStart;
    public IAnmFrame[] Frames { get; } = [.. anmAnimation.Frames.Select((frame) => new AnmFrameAdapter(frame))];
}

public readonly struct AnmFrameAdapter(AnmFrame frame) : IAnmFrame
{
    public IEnumerable<IAnmBone> Bones => frame.Bones.Select((bone) => (IAnmBone)new AnmBoneAdapter(bone));
}

public readonly struct AnmBoneAdapter(AnmBone bone) : IAnmBone
{
    public short Id => bone.Id;
    public float ScaleX => bone.ScaleX;
    public float RotateSkew0 => bone.RotateSkew0;
    public float RotateSkew1 => bone.RotateSkew1;
    public float ScaleY => bone.ScaleY;
    public float X => bone.X;
    public float Y => bone.Y;
    public double Opacity => bone.Opacity;
    public short Frame => bone.Frame;
}

public static class Anm904Migrator
{
    public static AnmFile MigrateFile(AnmFile_904 file) => new()
    {
        Header = file.Header,
        Classes = file.Classes.ToDictionary((entry) => entry.Key, (entry) => MigrateClass(entry.Value)),
    };

    public static AnmClass MigrateClass(AnmClass_904 @class) => new()
    {
        Index = @class.Index,
        FileName = @class.FileName,
        Animations = @class.Animations.ToDictionary((entry) => entry.Key, (entry) => MigrateAnimation(entry.Value)),
    };

    public static AnmAnimation MigrateAnimation(AnmAnimation_904 animation) => new()
    {
        Name = animation.Name,
        LoopStart = animation.LoopStart,
        RecoveryStart = animation.RecoveryStart,
        FreeStart = animation.FreeStart,
        PreviewFrame = animation.PreviewFrame,
        BaseStart = animation.BaseStart,
        Data = [.. animation.Data],
        Frames = [.. animation.Frames.Select(MigrateFrame)],
    };

    public static AnmFrame MigrateFrame(AnmFrame_904 frame) => new()
    {
        Id = frame.Id,
        FireSocket = frame.FireSocket,
        EBPlatformPos = frame.EBPlatformPos,
        Bones = [.. frame.Bones.Select(MigrateBone)],
    };

    public static AnmBone MigrateBone(AnmBone_904 bone) => new()
    {
        Id = bone.Id,
        ScaleX = bone.ScaleX,
        RotateSkew0 = bone.RotateSkew0,
        RotateSkew1 = bone.RotateSkew1,
        ScaleY = bone.ScaleY,
        X = bone.X,
        Y = bone.Y,
        Opacity = bone.Opacity,
        Frame = (sbyte)bone.Frame
    };
}