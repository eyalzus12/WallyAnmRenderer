using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BrawlhallaAnimLib.Anm;
using WallyAnmSpinzor;

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