using System;
using BrawlhallaAnimLib.Math;
using Raylib_cs;

namespace WallyAnmRenderer;

public sealed class Texture2DWrapper : IDisposable
{
    private bool _disposedValue = false;

    public Texture2D Texture { get; internal set; }
    public Transform2D Transform { get; set; }
    public bool OwnTexture { get; }

    public int Width => Texture.Width;
    public int Height => Texture.Height;

    public Texture2DWrapper()
    {
        Texture = new();
        Transform = Transform2D.ZERO;
        OwnTexture = false;
    }

    public Texture2DWrapper(Texture2D texture, Transform2D transform, bool ownTexture = true)
    {
        Texture = texture;
        Transform = transform;
        OwnTexture = ownTexture;
    }

    ~Texture2DWrapper()
    {
        if (OwnTexture && Texture.Id != 0)
        {
            Rl.TraceLog(TraceLogLevel.Fatal, "Finalizer called on an unfreed texture. You have a memory leak!");
        }
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            if (OwnTexture && Texture.Id != 0)
                Rl.UnloadTexture(Texture);
            _disposedValue = true;
        }

        GC.SuppressFinalize(this);
    }
}