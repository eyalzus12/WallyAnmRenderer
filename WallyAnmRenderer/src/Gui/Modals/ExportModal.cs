using System.Collections.Generic;
using System.Xml.Linq;
using BrawlhallaAnimLib;
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

        GfxType gfxClone = new(gfx)
        {
            AnimScale = 1
        };

        BoneSpriteWithName[]? sprites = AnimationBuilder.BuildAnim(animator.Loader, gfxClone, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);
        if (sprites is null)
        {
            ImGui.Text("Loading...");
            ImGui.EndPopup();
            return;
        }

        List<(XDocument, Transform2D)> svgList = [];
        foreach (BoneSpriteWithName sprite in sprites)
        {
            BoneShape[]? shapes = SpriteToShapeConverter.ConvertToShapes(animator.Loader, sprite);
            if (shapes is null)
            {
                ImGui.Text("Loading...");
                ImGui.EndPopup();
                return;
            }

            SwfFileData? swf = animator.Loader.AssetLoader.LoadSwf(sprite.SwfFilePath);
            if (swf is null)
            {
                ImGui.Text("Loading...");
                ImGui.EndPopup();
                return;
            }

            foreach (BoneShape shape in shapes)
            {
                ShapeBaseTag swfShape = swf.ShapeTags[shape.ShapeId];
                swfShape = SwfUtils.DeepCloneShape(swfShape);
                ColorSwapUtils.ApplyColorSwaps(swfShape, sprite.ColorSwapDict);
                SwfShape compiledShape = new(new(swfShape));
                int shapeW = swfShape.ShapeBounds.XMax - swfShape.ShapeBounds.XMin;
                int shapeH = swfShape.ShapeBounds.YMax - swfShape.ShapeBounds.YMin;
                SvgShapeExporter svgExporter = new(new(shapeW, shapeH), new());
                compiledShape.Export(svgExporter);
                svgList.Add((svgExporter.Document, shape.Transform));
            }
        }

        // merge the svgs

        XNamespace xmlns = XNamespace.Get("http://www.w3.org/2000/svg");

        XElement svg = new(xmlns + "svg");
        svg.SetAttributeValue("width", 0);
        svg.SetAttributeValue("height", 0);
        int index = 0;
        foreach ((XDocument document, Transform2D transform) in svgList)
        {
            XElement main = document.Element("svg")!;

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

        ImGui.EndPopup();
    }
}