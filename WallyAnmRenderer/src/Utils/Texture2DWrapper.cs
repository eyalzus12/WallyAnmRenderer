using System;
using BrawlhallaAnimLib.Math;
using Raylib_cs;

namespace WallyAnmRenderer;

public class Texture2DWrapper : IDisposable
{
    private bool _disposedValue = false;

    public Texture2D Texture { get; }
    public Transform2D Transform { get; }

    public Texture2DWrapper(Texture2D texture, Transform2D transform)
    {
        Texture = texture;
        Rl.SetTextureWrap(texture, TextureWrap.Clamp);
        Transform = transform;
    }

    ~Texture2DWrapper()
    {
        if (Texture.Id != 0)
        {
            Rl.TraceLog(TraceLogLevel.Fatal, "Finalizer called on an unfreed texture. You have a memory leak!");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (Texture.Id != 0)
            {
                Rl.UnloadTexture(Texture);
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}