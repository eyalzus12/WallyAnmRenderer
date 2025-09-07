using BrawlhallaAnimLib.Gfx;

namespace WallyAnmRenderer;

public interface IGfxInfo
{
    string? Animation { get; }
    bool AnimationPicked { get; }
    (IGfxType gfx, bool flip)? ToGfxType(SwzGameFile gameFiles);
}