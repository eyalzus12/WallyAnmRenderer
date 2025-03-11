using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using ImGuiNET;
using SwfLib.Tags.ShapeTags;
using SwiffCheese.Exporting.Svg;
using SwiffCheese.Shapes;

namespace WallyAnmRenderer;

public sealed partial class ExportModal(string? id = null)
{
    private static readonly XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");

    public const string NAME = "Edit color";
    private string PopupName => $"{NAME}{(id is null ? "" : $"##{id}")}";

    private bool _shouldOpen;
    private bool _open = false;
    public void Open() => _shouldOpen = true;

    private readonly List<string> _errors = [];
    private string _path = "";

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

    private static void NamespaceGradients(XElement element, string ns)
    {
        XElement? defs = element.Element(xmlns + "defs");
        if (defs is not null) NamespaceDefsGradients(defs, ns);
        XElement? g = element.Element(xmlns + "g");
        if (g is not null) NamespacePathsGradients(g, ns);
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

    private static (XDocument, Transform2D, ViewBox) ShapeToDocument(SwfFileData swf, BoneSpriteWithName sprite, BoneShape shape, bool flip)
    {
        Transform2D transform = (flip ? Transform2D.FLIP_X : Transform2D.IDENTITY) * shape.Transform;

        ShapeBaseTag swfShape = swf.ShapeTags[shape.ShapeId];
        swfShape = SwfUtils.DeepCloneShape(swfShape);
        ColorSwapUtils.ApplyColorSwaps(swfShape, sprite.ColorSwapDict);
        SwfShape compiledShape = new(new(swfShape));
        double shapeX = swfShape.ShapeBounds.XMin / 20.0;
        double shapeY = swfShape.ShapeBounds.YMin / 20.0;
        double shapeW = (swfShape.ShapeBounds.XMax - swfShape.ShapeBounds.XMin) / 20.0;
        double shapeH = (swfShape.ShapeBounds.YMax - swfShape.ShapeBounds.YMin) / 20.0;
        SvgShapeExporter svgExporter = new(new(shapeW, shapeH), new(1, 0, 0, 1, -shapeX, -shapeY));
        compiledShape.Export(svgExporter);

        ViewBox viewBox = new(0, 0, 0, 0);
        (double, double)[] points = [(shapeX, shapeY), (shapeX + shapeW, shapeY), (shapeX, shapeY + shapeH), (shapeX + shapeW, shapeY + shapeH)];
        foreach ((double x, double y) in points)
        {
            (double nx, double ny) = transform * (x, y);
            viewBox.ExtendWith(nx, ny);
        }

        Transform2D realTransform = transform * Transform2D.CreateTranslate(shapeX, shapeY);
        return (svgExporter.Document, realTransform, viewBox);
    }

    private static async IAsyncEnumerable<(XDocument, Transform2D, ViewBox)> SpriteToDocuments(Animator animator, BoneSpriteWithName sprite, bool flip)
    {
        SwfFileData swf = await animator.Loader.AssetLoader.LoadSwf(sprite.SwfFilePath);
        BoneShape[] shapes = await animator.SpriteToShapes(sprite);
        foreach (BoneShape shape in shapes)
            yield return ShapeToDocument(swf, sprite, shape, flip);
    }

    private static async IAsyncEnumerable<(XDocument, Transform2D, ViewBox)> SpritesToDocuments(Animator animator, BoneSpriteWithName[] sprites, bool flip)
    {
        foreach (BoneSpriteWithName sprite in sprites)
            await foreach (var result in SpriteToDocuments(animator, sprite, flip))
                yield return result;
    }

    public static async Task<XDocument> ExportAnimation(Animator animator, IGfxType gfx, string animation, long frame, bool flip)
    {
        GfxType gfxClone = new(gfx)
        {
            AnimScale = 1
        };

        BoneSpriteWithName[] sprites = await animator.GetAnimationInfo(gfxClone, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);

        ViewBox viewBox = new(0, 0, 0, 0);
        List<(XDocument, Transform2D)> svgList = [];
        await foreach (var shape in SpritesToDocuments(animator, sprites, flip))
        {
            (XDocument document, Transform2D transform, ViewBox viewBox2) = shape;
            viewBox.ExtendWith(viewBox2);

            svgList.Add((document, transform));
        }

        // merge the svgs

        XElement svg = new(xmlns + "svg");
        svg.SetAttributeValue("viewBox", $"{viewBox.MinX} {viewBox.MinY} {viewBox.Width} {viewBox.Height}");

        int index = 0;
        foreach ((XDocument document, Transform2D transform) in svgList)
        {
            XElement main = document.Root!;

            // create symbol
            XElement symbol = new(main) { Name = xmlns + "symbol" };
            string symbolId = $"shape{index++}";
            symbol.SetAttributeValue("id", symbolId);
            NamespaceGradients(symbol, symbolId);
            svg.Add(symbol);

            // create use
            XElement use = new(xmlns + "use");
            use.SetAttributeValue("href", $"#{symbolId}");

            string transformString = SvgUtils.SvgMatrixString(
                transform.ScaleX, transform.SkewY, transform.SkewX, transform.ScaleY,
                transform.TranslateX, transform.TranslateY
            );
            use.SetAttributeValue("transform", transformString);
            svg.Add(use);
        }

        XDocument result = new(new XDeclaration("1.0", "UTF-8", "no"), svg);

        return result;
    }

    public void Update(Animator? animator, IGfxType gfx, string animation, long frame, bool flip)
    {
        if (_shouldOpen)
        {
            ImGui.OpenPopup(PopupName);
            _shouldOpen = false;
            _open = true;
            _errors.Clear();
        }

        if (!ImGui.BeginPopupModal(PopupName, ref _open)) return;

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
                XDocument document = await ExportAnimation(animator, gfx, animation, frame, flip);
                using FileStream file = new(path, FileMode.Create, FileAccess.Write);
                await document.SaveAsync(file, SaveOptions.None, new());
            }
            catch (Exception e)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                _errors.Add(e.Message);
            }
        }

        ImGui.InputText("Output path", ref _path, 256);

        if (ImGuiEx.DisabledButton("Export!", _path.Trim() == ""))
        {
            _ = export(_path);
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

    [GeneratedRegex(@"url\(#(gradient\d+)\)")]
    private static partial Regex PathFillUrlRegex();
}