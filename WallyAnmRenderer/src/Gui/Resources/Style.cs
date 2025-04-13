using System.IO;
using System.Reflection;
using ImGuiNET;

namespace WallyAnmRenderer;

public static class Style
{
    public static void Apply()
    {
        if (!File.Exists("imgui.ini"))
        {
            Rl.TraceLog(Raylib_cs.TraceLogLevel.Info, "Loading default imgui.ini");
            LoadDefaultImGuiIni("WallyAnmRenderer.res.imgui.default_imgui.ini");
        }

        // stolen from https://github.com/ocornut/imgui/issues/707#issuecomment-917151020
        ImGuiStylePtr style = ImGui.GetStyle();
        style.WindowPadding = new(8.00f, 8.00f);
        style.FramePadding = new(5.00f, 2.00f);
        style.CellPadding = new(6.00f, 6.00f);
        style.ItemSpacing = new(6.00f, 6.00f);
        style.ItemInnerSpacing = new(6.00f, 6.00f);
        style.TouchExtraPadding = new(0.00f, 0.00f);
        style.IndentSpacing = 25;
        style.ScrollbarSize = 15;
        style.GrabMinSize = 10;
        style.WindowBorderSize = 1;
        style.ChildBorderSize = 1;
        style.PopupBorderSize = 1;
        style.FrameBorderSize = 0;
        style.TabBorderSize = 1;
        style.WindowRounding = 7;
        style.ChildRounding = 7;
        style.FrameRounding = 7;
        style.PopupRounding = 7;
        style.ScrollbarRounding = 7;
        style.GrabRounding = 7;
        style.TabRounding = 7;
        style.LogSliderDeadzone = 4;

        style.Colors[(int)ImGuiCol.Text] = new(1.00f, 1.00f, 1.00f, 1.00f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new(0.50f, 0.50f, 0.50f, 1.00f);
        style.Colors[(int)ImGuiCol.WindowBg] = new(0.10f, 0.10f, 0.10f, 1.00f);
        style.Colors[(int)ImGuiCol.ChildBg] = new(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.PopupBg] = new(0.19f, 0.19f, 0.19f, 0.92f);
        style.Colors[(int)ImGuiCol.Border] = new(0.19f, 0.19f, 0.19f, 0.29f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new(0.00f, 0.00f, 0.00f, 0.24f);
        style.Colors[(int)ImGuiCol.FrameBg] = new(0.05f, 0.05f, 0.05f, 0.54f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new(0.19f, 0.19f, 0.19f, 0.54f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new(0.20f, 0.22f, 0.23f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBg] = new(0.00f, 0.00f, 0.00f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new(0.06f, 0.06f, 0.06f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new(0.00f, 0.00f, 0.00f, 1.00f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new(0.14f, 0.14f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new(0.05f, 0.05f, 0.05f, 0.54f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new(0.34f, 0.34f, 0.34f, 0.54f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new(0.40f, 0.40f, 0.40f, 0.54f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new(0.56f, 0.56f, 0.56f, 0.54f);
        style.Colors[(int)ImGuiCol.CheckMark] = new(0.33f, 0.67f, 0.86f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new(0.34f, 0.34f, 0.34f, 0.54f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new(0.56f, 0.56f, 0.56f, 0.54f);
        style.Colors[(int)ImGuiCol.Button] = new(0.05f, 0.05f, 0.05f, 0.54f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new(0.19f, 0.19f, 0.19f, 0.54f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new(0.20f, 0.22f, 0.23f, 1.00f);
        style.Colors[(int)ImGuiCol.Header] = new(0.00f, 0.00f, 0.00f, 0.52f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new(0.00f, 0.00f, 0.00f, 0.36f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new(0.20f, 0.22f, 0.23f, 0.33f);
        style.Colors[(int)ImGuiCol.Separator] = new(0.28f, 0.28f, 0.28f, 0.29f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new(0.44f, 0.44f, 0.44f, 0.29f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new(0.40f, 0.44f, 0.47f, 1.00f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new(0.28f, 0.28f, 0.28f, 0.29f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new(0.44f, 0.44f, 0.44f, 0.29f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new(0.40f, 0.44f, 0.47f, 1.00f);
        style.Colors[(int)ImGuiCol.Tab] = new(0.00f, 0.00f, 0.00f, 0.52f);
        style.Colors[(int)ImGuiCol.TabHovered] = new(0.14f, 0.14f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.TabSelected] = new(0.20f, 0.20f, 0.20f, 0.36f);
        style.Colors[(int)ImGuiCol.TabDimmed] = new(0.00f, 0.00f, 0.00f, 0.52f);
        style.Colors[(int)ImGuiCol.TabDimmedSelected] = new(0.14f, 0.14f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.DockingPreview] = new(0.33f, 0.67f, 0.86f, 1.00f);

        style.Colors[(int)ImGuiCol.TableHeaderBg] = new(0.00f, 0.00f, 0.00f, 0.52f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new(0.00f, 0.00f, 0.00f, 0.52f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new(0.28f, 0.28f, 0.28f, 0.29f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new(0.20f, 0.22f, 0.23f, 1.00f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new(0.33f, 0.67f, 0.86f, 1.00f);

        // style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        // style.Colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        // style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 0.00f, 0.00f, 0.70f);
        // style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(1.00f, 0.00f, 0.00f, 0.20f);
        // style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(1.00f, 0.00f, 0.00f, 0.35f);
    }

    private static void LoadDefaultImGuiIni(string resourceName)
    {
        string iniFile;
        using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (stream is null) return;
            using StreamReader reader = new(stream);
            iniFile = reader.ReadToEnd();
        }

        ImGui.LoadIniSettingsFromMemory(iniFile);
    }
}