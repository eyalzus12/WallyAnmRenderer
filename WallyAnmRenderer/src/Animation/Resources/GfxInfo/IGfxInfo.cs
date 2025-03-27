using BrawlhallaAnimLib.Gfx;

namespace WallyAnmRenderer;

public interface IGfxInfo
{
    bool AnimationPicked { get; }
    (IGfxType gfx, string animation, bool flip)? ToGfxType(SwzGameFile gameFiles);
}