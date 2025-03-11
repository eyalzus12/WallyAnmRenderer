using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Swf;
using ImGuiNET;
using SwfLib.Tags.ShapeTags;
using SwiffCheese.Exporting.Svg;
using SwiffCheese.Shapes;

namespace WallyAnmRenderer;

public sealed class ExportModal(string? id = null)
{
    public const string NAME = "Edit color";
    private string PopupName => $"{NAME}{(id is null ? "" : $"##{id}")}";

    private bool _shouldOpen;
    private bool _open = false;
    public void Open() => _shouldOpen = true;

    public async Task<XDocument> ExportAnimation(Animator animator, IGfxType gfx, string animation, long frame, bool flip)
    {
        GfxType gfxClone = new(gfx)
        {
            AnimScale = 1
        };

        BoneSpriteWithName[] sprites = await animator.GetAnimationInfo(gfxClone, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);

        double minX = 0;
        double minY = 0;
        double maxX = 0;
        double maxY = 0;
        List<(XDocument, Transform2D)> svgList = [];
        foreach (BoneSpriteWithName sprite in sprites)
        {
            SwfFileData swf = await animator.Loader.AssetLoader.LoadSwf(sprite.SwfFilePath);
            BoneShape[] shapes = await SpriteToShapeConverter.ConvertToShapes(animator.Loader, sprite);

            foreach (BoneShape shape in shapes)
            {
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

                (double, double)[] points = [(shapeX, shapeY), (shapeX + shapeW, shapeY), (shapeX, shapeY + shapeH), (shapeX + shapeW, shapeY + shapeH)];
                foreach ((double x, double y) in points)
                {
                    (double nx, double ny) = shape.Transform * (x, y);
                    if (nx < minX) minX = nx;
                    if (nx > maxX) maxX = nx;
                    if (ny < minY) minY = ny;
                    if (ny > maxY) maxY = ny;
                }

                svgList.Add((svgExporter.Document, shape.Transform * Transform2D.CreateTranslate(shapeX, shapeY)));
            }
        }

        // merge the svgs

        XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");

        XElement svg = new(xmlns + "svg");
        svg.SetAttributeValue("viewBox", $"{minX} {minY} {maxX - minX} {maxY - minY}");

        int index = 0;
        foreach ((XDocument document, Transform2D transform) in svgList)
        {
            XElement main = document.Root!;

            // create symbol
            XElement symbol = new(main) { Name = xmlns + "symbol" };
            string symbolId = $"shape{index++}";
            symbol.SetAttributeValue("id", symbolId);
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
        }

        if (!ImGui.BeginPopupModal(PopupName, ref _open)) return;

        if (animator is null)
        {
            ImGui.Text("Animation is not loaded");
            ImGui.EndPopup();
            return;
        }

        async Task export()
        {
            XDocument document = await ExportAnimation(animator, gfx, animation, frame, flip);

            using FileStream file = new("test.svg", FileMode.Open, FileAccess.Write);
            document.Save(file);
        }

        if (ImGui.Button("Export!"))
        {
            _ = export();
        }

        ImGui.EndPopup();
    }
}