using System.Runtime.InteropServices;
using Raylib_cs;

namespace WallyAnmRenderer;

// re-implements a bunch of raylib functions
public static class RaylibEx
{
    // modified from raylib to properly supported bigger sizes
    public static ulong GetPixelDataSize(int width, int height, PixelFormat format)
    {
        ulong bpp = 0;            // Bits per pixel

        switch (format)
        {
            case PixelFormat.CompressedAstc8X8Rgba:
                bpp = 2;
                break;
            case PixelFormat.CompressedDxt1Rgb:
            case PixelFormat.CompressedDxt1Rgba:
            case PixelFormat.CompressedEtc1Rgb:
            case PixelFormat.CompressedEtc2Rgb:
            case PixelFormat.CompressedPvrtRgb:
            case PixelFormat.CompressedPvrtRgba:
                bpp = 4;
                break;
            case PixelFormat.UncompressedGrayscale:
            case PixelFormat.CompressedDxt3Rgba:
            case PixelFormat.CompressedDxt5Rgba:
            case PixelFormat.CompressedEtc2EacRgba:
            case PixelFormat.CompressedAstc4X4Rgba:
                bpp = 8;
                break;
            case PixelFormat.UncompressedR16:
            case PixelFormat.UncompressedGrayAlpha:
            case PixelFormat.UncompressedR5G6B5:
            case PixelFormat.UncompressedR5G5B5A1:
            case PixelFormat.UncompressedR4G4B4A4:
                bpp = 16;
                break;
            case PixelFormat.UncompressedR8G8B8:
                bpp = 24;
                break;
            case PixelFormat.UncompressedR8G8B8A8:
            case PixelFormat.UncompressedR32:
                bpp = 32;
                break;
            case PixelFormat.UncompressedR16G16B16:
                bpp = 48;
                break;
            case PixelFormat.UncompressedR16G16B16A16:
                bpp = 64;
                break;
            case PixelFormat.UncompressedR32G32B32:
                bpp = 96;
                break;
            case PixelFormat.UncompressedR32G32B32A32:
                bpp = 128;
                break;
        }

        ulong dataSize = bpp * (ulong)width * (ulong)height / 8;

        // Most compressed formats works on 4x4 blocks,
        // if texture is smaller, minimum dataSize is 8 or 16
        if ((width < 4) && (height < 4))
        {
            if ((format >= PixelFormat.CompressedDxt1Rgb) && (format < PixelFormat.CompressedDxt3Rgba)) dataSize = 8;
            else if ((format >= PixelFormat.CompressedDxt3Rgba) && (format < PixelFormat.CompressedAstc8X8Rgba)) dataSize = 16;
        }

        return dataSize;
    }

    // taken from raylib: https://github.com/raysan5/raylib/blob/e4dcdfa1f23ec2deae16c263d5b66ecc3326514a/src/rtextures.c#L2371
    // modified to copy the image instead of reallocating it
    public static unsafe RlImage ImageCopyWithMipmaps(RlImage image)
    {
        // Security check to avoid program crash
        if ((image.Data == null) || (image.Width == 0) || (image.Height == 0)) return image;

        int width = image.Width;
        int height = image.Height;
        ulong imageSize = GetPixelDataSize(width, height, image.Format);

        // Required mipmap levels count (including base level)
        int mipCount = 1;
        // Base image width
        int mipWidth = width;
        // Base image height
        int mipHeight = height;
        // Image data size (in bytes)
        ulong mipSize = imageSize;

        // Count mipmap levels required
        while ((mipWidth != 1) || (mipHeight != 1))
        {
            if (mipWidth != 1) mipWidth /= 2;
            if (mipHeight != 1) mipHeight /= 2;

            // Security check for NPOT textures
            if (mipWidth < 1) mipWidth = 1;
            if (mipHeight < 1) mipHeight = 1;

            mipCount++;
            mipSize += GetPixelDataSize(mipWidth, mipHeight, image.Format);       // Add mipmap size (in bytes)
        }

        if (image.Mipmaps < mipCount)
        {
            // this check ensures that any future cast to nuint is valid
            if (mipSize > nuint.MaxValue)
            {
                Rl.TraceLog(TraceLogLevel.Warning, "IMAGE: Reqreuid mipmap memory exceeds addressable range");
            }

            // modification is here: copy instead of realloc
            void* temp = NativeMemory.Alloc((nuint)mipSize);
            if (temp == null)
            {
                Rl.TraceLog(TraceLogLevel.Warning, "IMAGE: Mipmaps required memory could not be allocated");
            }
            else
            {
                try
                {
                    NativeMemory.Copy(image.Data, temp, (nuint)imageSize);
                    image.Data = temp;
                }
                catch
                {
                    NativeMemory.Free(image.Data);
                    throw;
                }
            }

            // Pointer to allocated memory point where store next mipmap level data
            byte* nextmip = (byte*)image.Data;

            mipWidth = image.Width;
            mipHeight = image.Height;
            mipSize = GetPixelDataSize(mipWidth, mipHeight, image.Format);

            RlImage imCopy = Rl.ImageCopy(image);
            for (int i = 1; i < mipCount; i++)
            {
                nextmip += mipSize;

                mipWidth /= 2;
                mipHeight /= 2;

                // Security check for NPOT textures
                if (mipWidth < 1) mipWidth = 1;
                if (mipHeight < 1) mipHeight = 1;

                mipSize = GetPixelDataSize(mipWidth, mipHeight, image.Format);

                if (i < image.Mipmaps) continue;

                Rl.ImageResize(ref imCopy, mipWidth, mipHeight); // Uses internally Mitchell cubic downscale filter

                NativeMemory.Copy(nextmip, imCopy.Data, (nuint)mipSize);
            }
            Rl.UnloadImage(imCopy);

            image.Mipmaps = mipCount;
        }
        else
        {
            Rl.TraceLog(TraceLogLevel.Warning, "IMAGE: Mipmaps already available");
        }

        return image;
    }
}