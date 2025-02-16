using System.Numerics;

namespace WallyAnmRenderer;

public class ViewportBounds
{
    public Vector2 P1 { get; set; }
    public Vector2 P2 { get; set; }
    public float Width => P2.X - P1.X;
    public float Height => P2.Y - P1.Y;
    public Vector2 Size => new(Width, Height);
}