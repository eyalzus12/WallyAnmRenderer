using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using ImGuiNET;
using NativeFileDialogSharp;
using SkiaSharp;
using Svg.Skia;
using SwfLib.Tags.ShapeTags;
using SwiffCheese.Exporting.Svg;
using SwiffCheese.Shapes;

namespace WallyAnmRenderer;

public sealed partial class ExportModal(string? id = null)
{
    private static readonly XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");

    public const string NAME = "Export animation";
    private string PopupName => $"{NAME}{(id is null ? "" : $"##{id}")}";

    private enum ExportAsEnum
    {
        Svg = 0,
        Png = 1,
    }
    private static readonly string[] EXPORT_AS = [".svg", ".png"];
    private ExportAsEnum _exportAs = ExportAsEnum.Svg;

    private bool _shouldOpen;
    private bool _open = false;
    public void Open() => _shouldOpen = true;

    private readonly List<string> _errors = [];

    private static void NamespaceDefsGradients(XElement defs, string ns)
    {
        IEnumerable<XElement> linearGradients = defs.Elements(xmlns + "linearGradient");
        IEnumerable<XElement> radialGradients = defs.Elements(xmlns + "radialGradient");
        foreach (XElement gradient in linearGradients.Concat(radialGradients))
        {
            string? id = gradient.Attribute("id")?.Value;
            if (id is null) continue;
            gradient.SetAttributeValue("id", $"{ns}::{id}");
        }
    }

    private static void NamespacePathsGradients(XElement g, string ns)
    {
        IEnumerable<XElement> paths = g.Elements(xmlns + "path");
        foreach (XElement path in paths)
        {
            string? fill = path.Attribute("fill")?.Value;
            if (fill is null) continue;

            Regex re = PathFillUrlRegex();
            Match match = re.Match(fill);
            if (!match.Success) continue;

            path.SetAttributeValue("fill", $"url(#{ns}::{match.Groups[1]})");
        }
    }

    private record struct ViewBox(double MinX, double MinY, double MaxX, double MaxY)
    {
        public readonly double Width => MaxX - MinX;
        public readonly double Height => MaxY - MinY;

        public void ExtendWith(double x, double y)
        {
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
        }

        public void ExtendWith(ViewBox viewBox)
        {
            if (viewBox.MinX < MinX) MinX = viewBox.MinX;
            if (viewBox.MaxX > MaxX) MaxX = viewBox.MaxX;
            if (viewBox.MinY < MinY) MinY = viewBox.MinY;
            if (viewBox.MaxY > MaxY) MaxY = viewBox.MaxY;
        }
    }

    private static (XDocument, Transform2D, ViewBox) ShapeToDocument(SwfFileData swf, SwfBoneSprite sprite, BoneShape shape)
    {
        Transform2D transform = shape.Transform;

        ShapeBaseTag swfShape = swf.ShapeTags[shape.ShapeId];
        swfShape = SwfUtils.DeepCloneShape(swfShape);
        ColorSwapUtils.ApplyColorSwaps(swfShape, sprite.ColorSwapDict);
        SwfShape compiledShape = new(new(swfShape));
        double shapeX = swfShape.ShapeBounds.XMin / 20.0;
        double shapeY = swfShape.ShapeBounds.YMin / 20.0;
        double shapeW = (swfShape.ShapeBounds.XMax - swfShape.ShapeBounds.XMin) / 20.0;
        double shapeH = (swfShape.ShapeBounds.YMax - swfShape.ShapeBounds.YMin) / 20.0;

        SvgShapeExporter svgExporter = new(new(shapeW, shapeH), new(1, 0, 0, 1, 0, 0));
        compiledShape.Export(svgExporter);

        ViewBox viewBox = new(0, 0, 0, 0);
        (double, double)[] points = [(shapeX, shapeY), (shapeX + shapeW, shapeY), (shapeX, shapeY + shapeH), (shapeX + shapeW, shapeY + shapeH)];
        foreach ((double x, double y) in points)
        {
            (double nx, double ny) = transform * (x, y);
            viewBox.ExtendWith(nx, ny);
        }

        return (svgExporter.Document, transform, viewBox);
    }

    private static async IAsyncEnumerable<(XDocument, Transform2D, ViewBox)> SpriteToDocuments(Loader loader, BoneSprite sprite)
    {
        if (sprite is SwfBoneSprite swfSprite)
        {
            SwfFileData swf = await loader.AssetLoader.LoadSwf(swfSprite.SwfFilePath);
            BoneShape[] shapes = await SpriteToShapeConverter.ConvertToShapes(loader, swfSprite);
            foreach (BoneShape shape in shapes)
                yield return ShapeToDocument(swf, swfSprite, shape);
        }
        else if (sprite is BitmapBoneSprite bitmapSprite)
        {
            double width = bitmapSprite.SpriteData.Width;
            double height = bitmapSprite.SpriteData.Height;
            double offX = bitmapSprite.SpriteData.XOffset;
            double offY = bitmapSprite.SpriteData.YOffset;

            XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");
            XElement imageElement = new(xmlns + "image");
            imageElement.SetAttributeValue("x", bitmapSprite.SpriteData.XOffset);
            imageElement.SetAttributeValue("y", bitmapSprite.SpriteData.YOffset);
            imageElement.SetAttributeValue("width", bitmapSprite.SpriteData.Width);
            imageElement.SetAttributeValue("height", bitmapSprite.SpriteData.Height);
            XElement svg = new(xmlns + "svg", imageElement);
            svg.SetAttributeValue("width", bitmapSprite.SpriteData.Width);
            svg.SetAttributeValue("height", bitmapSprite.SpriteData.Height);
            XDocument document = new(new XDeclaration("1.0", "UTF-8", "no"), svg);

            string path = Path.Combine(loader.AssetLoader.BrawlPath, bitmapSprite.SpriteData.File);
            SKEncodedImageFormat format;
            using (SKCodec codec = SKCodec.Create(path))
                format = codec.EncodedFormat;

            ViewBox viewBox = new(0, 0, 0, 0);
            (double, double)[] points = [(offX, offY), (offX + width, offY), (offX, offY + height), (offX + width, offY + height)];
            foreach ((double x, double y) in points)
            {
                (double nx, double ny) = bitmapSprite.Transform * (x, y);
                viewBox.ExtendWith(nx, ny);
            }

            string mimeType = format switch
            {
                SKEncodedImageFormat.Bmp => "image/bmp",
                SKEncodedImageFormat.Gif => "image/gif",
                SKEncodedImageFormat.Ico => "image/vnd.microsoft.icon",
                SKEncodedImageFormat.Jpeg => "image/jpeg",
                SKEncodedImageFormat.Png => "image/png",
                //SKEncodedImageFormat.Wbmp => throw new NotSupportedException(),
                SKEncodedImageFormat.Webp => "image/webp",
                //SKEncodedImageFormat.Pkm => throw new NotSupportedException(),
                //SKEncodedImageFormat.Ktx => throw new NotSupportedException(),
                //SKEncodedImageFormat.Astc => throw new NotSupportedException(),
                //SKEncodedImageFormat.Dng => throw new NotSupportedException(),
                //SKEncodedImageFormat.Heif => throw new NotSupportedException(),
                SKEncodedImageFormat.Avif => "image/avif",
                //SKEncodedImageFormat.Jpegxl => throw new NotSupportedException(),
                _ => throw new NotSupportedException(format.ToString()),
            };

            byte[] bytes = await File.ReadAllBytesAsync(path);
            string base64 = Convert.ToBase64String(bytes);
            imageElement.SetAttributeValue("href", $"data:{mimeType};base64,{base64}");

            yield return (document, sprite.Transform, viewBox);
        }
    }

    private static async IAsyncEnumerable<(XDocument, Transform2D, ViewBox)> SpritesToDocuments(Loader loader, IAsyncEnumerable<BoneSprite> sprites)
    {
        await foreach (BoneSprite sprite in sprites)
            await foreach (var result in SpriteToDocuments(loader, sprite))
                yield return result;
    }

    public static async ValueTask<XDocument> ExportAnimation(Loader loader, IGfxType gfx, string animation, long frame, bool flip)
    {
        GfxType gfxClone = new(gfx)
        {
            AnimScale = 1
        };

        IAsyncEnumerable<BoneSprite> sprites = AnimationBuilder.BuildAnim(loader, gfxClone, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);

        ViewBox viewBox = new(0, 0, 0, 0);
        List<(XDocument, Transform2D)> svgList = [];
        await foreach (var shape in SpritesToDocuments(loader, sprites))
        {
            (XDocument document, Transform2D transform, ViewBox viewBox2) = shape;
            viewBox.ExtendWith(viewBox2);

            svgList.Add((document, transform));
        }

        // merge the svgs

        XElement svg = new(xmlns + "svg");
        svg.SetAttributeValue("width", viewBox.Width);
        svg.SetAttributeValue("height", viewBox.Height);
        svg.SetAttributeValue("viewBox", $"{viewBox.MinX} {viewBox.MinY} {viewBox.Width} {viewBox.Height}");

        XElement? mainDefs = null;

        int index = 0;
        foreach ((XDocument document, Transform2D transform) in svgList)
        {
            XElement main = document.Root!;

            string shapeId = $"shape{index++}";

            XElement? g = main.Element(xmlns + "g");
            if (g is not null)
            {
                NamespacePathsGradients(g, shapeId);

                // the 'g' element will always have an identity matrix as its transform
                // so we can just override the transform matrix with the true one
                XElement newG = new(g);
                string transformString = SvgUtils.SvgMatrixString(
                    transform.ScaleX, transform.SkewY, transform.SkewX, transform.ScaleY,
                    transform.TranslateX, transform.TranslateY
                );
                newG.SetAttributeValue("transform", transformString);

                svg.Add(newG);
            }

            XElement? image = main.Element(xmlns + "image");
            if (image is not null)
            {
                XElement newImage = new(image);
                string transformString = SvgUtils.SvgMatrixString(
                    transform.ScaleX, transform.SkewY, transform.SkewX, transform.ScaleY,
                    transform.TranslateX, transform.TranslateY
                );
                newImage.SetAttributeValue("transform", transformString);

                svg.Add(newImage);
            }

            XElement? defs = main.Element(xmlns + "defs");
            if (defs is not null)
            {
                NamespaceDefsGradients(defs, shapeId);
                mainDefs ??= new(xmlns + "defs");
                mainDefs.Add(defs.Nodes());
            }
        }

        if (mainDefs is not null) svg.Add(mainDefs);

        XDocument result = new(new XDeclaration("1.0", "UTF-8", "no"), svg);

        return result;
    }

    public void Update(PathPreferences prefs, Animator? animator, IGfxType gfx, string animation, long frame, bool flip)
    {
        if (_shouldOpen)
        {
            ImGui.OpenPopup(PopupName);
            _shouldOpen = false;
            _open = true;
            _errors.Clear();
        }

        if (!ImGui.BeginPopupModal(PopupName, ref _open, ImGuiWindowFlags.AlwaysAutoResize)) return;

        if (animator is null)
        {
            ImGui.Text("Animation is not loaded");
            ImGui.EndPopup();
            return;
        }

        async Task export(string path)
        {
            try
            {
                XDocument document = await ExportAnimation(animator.Loader, gfx, animation, frame, flip);
                switch (_exportAs)
                {
                    case ExportAsEnum.Svg:
                        await SaveToPathSvg(document, path);
                        break;
                    case ExportAsEnum.Png:
                        SaveToPathPng(document, path);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                _errors.Add(e.Message);
            }
        }

        int currentItem = (int)_exportAs;
        ImGui.Combo("Export as", ref currentItem, EXPORT_AS, EXPORT_AS.Length);
        _exportAs = (ExportAsEnum)currentItem;

        if (ImGui.Button("Export"))
        {
            string extension = _exportAs switch
            {
                ExportAsEnum.Svg => "svg",
                ExportAsEnum.Png => "png",
                _ => "*",
            };

            Task.Run(() => Dialog.FileSave(extension, prefs.ExportPath)).ContinueWith(async (task) =>
            {
                DialogResult result = task.Result;
                if (result.IsError) _errors.Add(result.ErrorMessage);
                if (!result.IsOk) return;

                string? newExportPath = Path.GetDirectoryName(result.Path);
                if (newExportPath is not null)
                    prefs.ExportPath = newExportPath;

                string path = result.Path;
                if (Path.GetExtension(path) != extension)
                    path = Path.ChangeExtension(path, extension);

                await export(path);
            });
        }

        if (_errors.Count > 0)
        {
            ImGui.SeparatorText("Errors");
            if (ImGui.Button("Clear"))
                _errors.Clear();
            foreach (string error in _errors)
            {
                ImGui.TextWrapped($"[Error]: {error}");
            }
        }

        ImGui.EndPopup();
    }

    [GeneratedRegex(@"url\(#(gradient[0-9]+)\)")]
    private static partial Regex PathFillUrlRegex();

    private static async Task SaveToPathSvg(XDocument document, string path)
    {
        using FileStream file = FileUtils.CreateWriteAsync(path);
        await document.SaveAsync(file, SaveOptions.None, default);
    }

    private static void SaveToPathPng(XDocument document, string path)
    {
        using XmlReader reader = document.CreateReader();
        using SKSvg svg = SKSvg.CreateFromXmlReader(reader);
        reader.Dispose();
        if (svg.Picture is null)
            throw new Exception("Loading svg failed");
        using FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        svg.Picture.ToImage(file, SKColor.Empty, SKEncodedImageFormat.Png, int.MaxValue, 4, 4, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
    }
}