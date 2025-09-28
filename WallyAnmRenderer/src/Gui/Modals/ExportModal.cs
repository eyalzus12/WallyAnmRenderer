using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

public sealed partial class ExportModal
{
    private const char FILENAME_FORMAT_FRAME_CHAR = '@';
    private static readonly XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");

    public const string NAME = "Export animation";

    private enum ExportAsEnum
    {
        Svg = 0,
        Png = 1,
    }
    private static readonly string[] EXPORT_AS = [".svg", ".png"];
    private ExportAsEnum _exportAs = ExportAsEnum.Svg;

    private enum ExportModeEnum
    {
        SingleFrame = 0,
        MultiFrame = 1,
    };
    private static readonly string[] EXPORT_MODE = ["Single frame", "Frame sequence"];
    private ExportModeEnum _exportMode = ExportModeEnum.SingleFrame;

    private bool _canvasContain = false;

    private string _fileNameFormat = $"Exported animation {FILENAME_FORMAT_FRAME_CHAR}";

    private long _startFrame = -1;
    private long _endFrame = -1;

    private double _animScale = 4;

    private bool _flip = false;

    private bool _shouldOpen;
    private bool _open = false;
    public void Open() => _shouldOpen = true;

    private CancellationTokenSource _cancellationSource = new();

    private readonly List<string> _errors = [];
    private string? _status = null;

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
            if (double.IsNaN(MinX) || x < MinX) MinX = x;
            if (double.IsNaN(MaxX) || x > MaxX) MaxX = x;
            if (double.IsNaN(MinY) || y < MinY) MinY = y;
            if (double.IsNaN(MaxY) || y > MaxY) MaxY = y;
        }

        public void ExtendWith(ViewBox viewBox)
        {
            if (double.IsNaN(MinX) || viewBox.MinX < MinX) MinX = viewBox.MinX;
            if (double.IsNaN(MaxX) || viewBox.MaxX > MaxX) MaxX = viewBox.MaxX;
            if (double.IsNaN(MinY) || viewBox.MinY < MinY) MinY = viewBox.MinY;
            if (double.IsNaN(MaxY) || viewBox.MaxY > MaxY) MaxY = viewBox.MaxY;
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
        if (sprite.Opacity != 1)
        {
            svgExporter.Document.Root!.SetAttributeValue("opacity", sprite.Opacity);
        }
        // TODO: Tint

        ViewBox viewBox = new(double.NaN, double.NaN, double.NaN, double.NaN);
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

            ViewBox viewBox = new(double.NaN, double.NaN, double.NaN, double.NaN);
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
                SKEncodedImageFormat.Jpegxl => "image/jxl",
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

    private static async ValueTask<(XDocument, ViewBox)> ExportAnimation(Loader loader, IGfxType gfx, string animation, double animScale, long frame, bool flip)
    {
        GfxType gfxClone = new(gfx)
        {
            AnimScale = animScale
        };

        IAsyncEnumerable<BoneSprite> sprites = AnimationBuilder.BuildAnim(loader, gfxClone, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);

        ViewBox viewBox = new(double.NaN, double.NaN, double.NaN, double.NaN);
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

                string? opacity = main.Attribute("opacity")?.Value;
                if (opacity is not null)
                    newG.SetAttributeValue("opacity", opacity);

                // TODO: tint

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

                // No opacity/tint for bitmap bone

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

        return (result, viewBox);
    }

    public void Update(PathPreferences prefs, Animator? animator, IGfxType gfx, string animation, long frame, bool flip)
    {
        if (_shouldOpen)
        {
            ImGui.OpenPopup(NAME);
            _shouldOpen = false;
            _open = true;
            _errors.Clear();

            _startFrame = frame;
            _endFrame = frame;
        }

        if (!ImGui.BeginPopupModal(NAME, ref _open, ImGuiWindowFlags.AlwaysAutoResize)) return;

        if (animator is null)
        {
            ImGui.Text("Animation is not loaded");
            ImGui.EndPopup();
            return;
        }

        int currentExportAs = (int)_exportAs;
        ImGui.Combo("Export as", ref currentExportAs, EXPORT_AS, EXPORT_AS.Length);
        _exportAs = (ExportAsEnum)currentExportAs;

        int currentExportMode = (int)_exportMode;
        ImGui.Combo("Export mode", ref currentExportMode, EXPORT_MODE, EXPORT_MODE.Length);
        _exportMode = (ExportModeEnum)currentExportMode;

        ImGui.Text("Frames start at 0");
        switch (_exportMode)
        {
            case ExportModeEnum.SingleFrame:
                if (ImGuiEx.InputLong("Frame", ref _startFrame))
                    _endFrame = _startFrame;
                break;
            case ExportModeEnum.MultiFrame:
                ImGuiEx.InputLong("Start Frame", ref _startFrame);
                ImGuiEx.InputLong("End Frame", ref _endFrame);
                ImGui.Text("@ Will be replaced with the frame number.");
                ImGui.InputText("File name format", ref _fileNameFormat, 256);
                ImGui.Checkbox("Size canvas such that all frames fit", ref _canvasContain);
                break;
        }

        ImGui.InputDouble("Anim scale", ref _animScale);

        if (_cancellationSource.IsCancellationRequested)
        {
            _cancellationSource.Dispose();
            _cancellationSource = new();
        }

        if (ImGui.Button("Export"))
        {
            _status = "Exporting...";
            Task.Run(async () =>
            {
                try
                {
                    switch (_exportMode)
                    {
                        case ExportModeEnum.SingleFrame:
                            await SaveSingleFrame(prefs, animator.Loader, gfx, animation);
                            break;
                        case ExportModeEnum.MultiFrame:
                            await SaveMultiFrame(prefs, animator.Loader, gfx, animation, _cancellationSource.Token);
                            break;
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                    _errors.Add(e.Message);
                }
                _status = null;
            });
        }

        if (ImGui.Button("Cancel"))
        {
            _cancellationSource.CancelAsync();
            _status = null;
        }

        if (_status is not null)
        {
            ImGui.TextWrapped($"[Status]: {_status}");
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

    private async Task SaveSingleFrame(PathPreferences prefs, Loader loader, IGfxType gfx, string animation)
    {
        string extension = _exportAs switch
        {
            ExportAsEnum.Svg => "svg",
            ExportAsEnum.Png => "png",
            _ => "*",
        };
        DialogResult result = Dialog.FileSave(extension, prefs.ExportPath);
        if (result.IsError) _errors.Add(result.ErrorMessage);
        if (!result.IsOk) return;

        string? newExportPath = Path.GetDirectoryName(result.Path);
        if (newExportPath is not null)
            prefs.ExportPath = newExportPath;

        string path = result.Path;
        if (Path.GetExtension(path) != extension)
            path = Path.ChangeExtension(path, extension);

        (XDocument document, _) = await ExportAnimation(loader, gfx, animation, _animScale, _startFrame, _flip);
        await ExportDocument(path, document);
    }

    private async Task SaveMultiFrame(PathPreferences prefs, Loader loader, IGfxType gfx, string animation, CancellationToken cancellationToken = default)
    {
        if (!_fileNameFormat.Contains(FILENAME_FORMAT_FRAME_CHAR))
        {
            _errors.Add($"File name format does not contain {FILENAME_FORMAT_FRAME_CHAR}");
            return;
        }

        DialogResult result = Dialog.FolderPicker(prefs.ExportPath);
        if (result.IsError) _errors.Add(result.ErrorMessage);
        if (!result.IsOk) return;
        prefs.ExportPath = result.Path;

        int direction = Math.Sign(_endFrame - _startFrame);
        if (direction == 0) direction = 1;

        string extension = _exportAs switch
        {
            ExportAsEnum.Svg => "svg",
            ExportAsEnum.Png => "png",
            _ => "*",
        };

        int digitCount = (int)Math.Ceiling(Math.Log10(Math.Max(_startFrame, _endFrame)));

        List<(string, Task<(XDocument, ViewBox)>)> animationTasks = [];
        for (long frame = _startFrame; frame <= _endFrame; frame += direction)
        {
            string filename = _fileNameFormat.Replace(FILENAME_FORMAT_FRAME_CHAR.ToString(), frame.ToString().PadLeft(digitCount, '0'));
            string path = Path.Combine(result.Path, filename);
            if (Path.GetExtension(path) != extension)
                path = Path.ChangeExtension(path, extension);

            animationTasks.Add((path, ExportAnimation(loader, gfx, animation, _animScale, frame, _flip).AsTask()));
        }

        if (_canvasContain)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string, XDocument, ViewBox)[] documents = await Task.WhenAll(animationTasks.Select(async x =>
            {
                (string path, Task<(XDocument, ViewBox)> task) = x;
                cancellationToken.ThrowIfCancellationRequested();
                (XDocument document, ViewBox viewBox) = await task;
                cancellationToken.ThrowIfCancellationRequested();
                return (path, document, viewBox);
            }));
            cancellationToken.ThrowIfCancellationRequested();

            ViewBox viewBox = new(double.NaN, double.NaN, double.NaN, double.NaN);
            foreach ((_, _, ViewBox viewBox2) in documents)
                viewBox.ExtendWith(viewBox2);
            foreach ((_, XDocument document, _) in documents)
            {
                XElement svg = document.Element(xmlns + "svg")!;
                svg.SetAttributeValue("width", viewBox.Width);
                svg.SetAttributeValue("height", viewBox.Height);
                svg.SetAttributeValue("viewBox", $"{viewBox.MinX} {viewBox.MinY} {viewBox.Width} {viewBox.Height}");
            }

            await Task.WhenAll(documents.Select(async x =>
            {
                (string path, XDocument document, _) = x;
                await ExportDocument(path, document, cancellationToken);
                _status = "Exported " + path;
            }));
        }
        else
        {
            await Task.WhenAll(animationTasks.Select(async x =>
            {
                (string path, Task<(XDocument, ViewBox)> task) = x;
                cancellationToken.ThrowIfCancellationRequested();
                (XDocument document, _) = await task;
                cancellationToken.ThrowIfCancellationRequested();
                await ExportDocument(path, document, cancellationToken);
                _status = "Exported " + path;
                cancellationToken.ThrowIfCancellationRequested();
            }));
        }
    }

    private async Task ExportDocument(string path, XDocument document, CancellationToken cancellationToken = default)
    {
        switch (_exportAs)
        {
            case ExportAsEnum.Svg:
                await SaveToPathSvg(document, path, cancellationToken);
                break;
            case ExportAsEnum.Png:
                SaveToPathPng(document, path);
                break;
        }
    }

    [GeneratedRegex(@"^url\(#(gradient[0-9]+)\)$")]
    private static partial Regex PathFillUrlRegex();

    private static readonly XmlWriterSettings XML_WRITER_SETTINGS = new()
    {
        Encoding = new UTF8Encoding(false),
        Async = true,
        NewLineChars = "\n",
        Indent = true,
    };

    private static async Task SaveToPathSvg(XDocument document, string path, CancellationToken cancellationToken = default)
    {
        using FileStream file = FileUtils.CreateWriteAsync(path);
        using XmlWriter writer = XmlWriter.Create(file, XML_WRITER_SETTINGS);
        await document.SaveAsync(writer, cancellationToken);
    }

    private static void SaveToPathPng(XDocument document, string path)
    {
        using XmlReader reader = document.CreateReader();
        using SKSvg svg = SKSvg.CreateFromXmlReader(reader);
        reader.Dispose();
        if (svg.Picture is null)
            throw new Exception("Loading svg failed");
        using FileStream file = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        svg.Picture.ToImage(file, SKColor.Empty, SKEncodedImageFormat.Png, int.MaxValue, 1, 1, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
    }
}