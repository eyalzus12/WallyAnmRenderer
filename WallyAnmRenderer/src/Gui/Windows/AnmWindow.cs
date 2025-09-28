using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using SwfLib.Tags;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AnmWindow
{
    private static readonly Vector4 SELECTED_COLOR = ImGuiEx.RGBHexToVec4(0xFF7F00);
    private static readonly Vector4 YELLOW = ImGuiEx.RGBHexToVec4(0xFFFF00);
    private static readonly string[] ANM_FORMAT_OPTIONS = ["Current", "?.??-9.04"];

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<string>? FileUnloaded;

    private string _anmFileFilter = "";
    private readonly Dictionary<string, string> _animationFilterState = [];

    private string _swfFileFilter = "";
    private readonly Dictionary<string, string> _swfFilterState = [];

    public void Show(string? brawlPath, AssetLoader? assetLoader, GfxInfo info)
    {
        ImGui.Begin("Animations", ref _open);

        if (brawlPath is null)
        {
            ImGui.TextWrapped("You must select your brawlhalla path to choose an animation");
            ImGui.End();
            return;
        }

        if (assetLoader is null)
        {
            ImGui.End();
            return;
        }

        if (ImGui.BeginTabBar("options", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton))
        {
            if (ImGui.BeginTabItem(".ANM"))
            {
                AnmTab(brawlPath, assetLoader, info);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(".SWF"))
            {
                SwfTab(brawlPath, assetLoader, info);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void AnmTab(string brawlPath, AssetLoader assetLoader, GfxInfo info)
    {
        assetLoader.AnmFormat = ImGuiEx.EnumCombo("Anm format", assetLoader.AnmFormat, ANM_FORMAT_OPTIONS);

        if (assetLoader.AnmFormat != AnmFormatEnum.Current)
        {
            ImGui.PushTextWrapPos();
            ImGui.TextColored(YELLOW, "You selected an older anm format. Unless you are working with older patches, loading anm files will fail.");
            ImGui.PopTextWrapPos();
        }

        string brawlAnimPath = Path.Join(brawlPath, "anims");
        string[] files = Directory.Exists(brawlAnimPath) ? Directory.GetFiles(brawlAnimPath, "*.anm") : [];

        ImGui.InputText("Filter files", ref _anmFileFilter, 256);
        foreach (string absolutePath in files)
        {
            string fileName = Path.GetRelativePath(brawlAnimPath, absolutePath);
            if (!fileName.Contains(_anmFileFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;
            string relativePath = Path.GetRelativePath(brawlPath, absolutePath);

            ImGui.PushID(fileName);
            if (assetLoader.IsAnmLoading(relativePath))
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    assetLoader.AnmFileCache.RemoveCached(absolutePath);
                }
                ImGui.SameLine();
                ImGui.TextDisabled("Loading...");
            }
            else if (assetLoader.TryGetAnm(relativePath, out AnmFile? anm))
            {
                void unloadButton()
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Unload"))
                    {
                        FileUnloaded?.Invoke(this, relativePath);
                        assetLoader.AnmFileCache.RemoveCached(absolutePath);
                    }
                }

                ImGui.SetNextItemAllowOverlap();
                if (ImGui.TreeNode(fileName))
                {
                    unloadButton();

                    foreach ((string anmClassName, AnmClass anmClass) in anm.Classes)
                    {
                        string[] parts = anmClassName.Split('/', 2);
                        string animFile = parts[0];
                        string animClass = parts[1];

                        if (ImGui.TreeNode(animClass))
                        {
                            string filter = _animationFilterState.GetValueOrDefault(anmClassName, "");
                            if (ImGui.InputText("Filter animations", ref filter, 256))
                            {
                                _animationFilterState[anmClassName] = filter;
                            }

                            if (ImGui.BeginListBox(""))
                            {
                                foreach (string animation in anmClass.Animations.Keys)
                                {
                                    if (!animation.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                                        continue;

                                    bool selected =
                                        animFile == info.AnimFile &&
                                        animClass == info.AnimClass &&
                                        animation == info.Animation;

                                    if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                                    if (ImGui.Selectable(animation, selected))
                                    {
                                        info.SourceFilePath = relativePath;
                                        info.AnimFile = animFile;
                                        info.AnimClass = animClass;
                                        info.Animation = animation;
                                    }
                                    if (selected) ImGui.PopStyleColor();
                                }
                                ImGui.EndListBox();
                            }

                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }
                else
                {
                    unloadButton();
                }
            }
            else
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    assetLoader.LoadAnm(relativePath);
                }
            }
            ImGui.PopID();
        }
    }

    private void SwfTab(string brawlPath, AssetLoader assetLoader, GfxInfo info)
    {
        string[] baseFiles = Directory.Exists(brawlPath) ? Directory.GetFiles(brawlPath, "*.swf") : [];
        string bonesSwfPath = Path.Join(brawlPath, "bones");
        string[] bonesFiles = Directory.Exists(bonesSwfPath) ? Directory.GetFiles(bonesSwfPath, "*.swf") : [];

        ImGui.InputText("Filter files", ref _swfFileFilter, 256);
        foreach (string absolutePath in baseFiles.Concat(bonesFiles))
        {
            string fileName = Path.GetRelativePath(brawlPath, absolutePath);
            string displayName = Path.GetFileName(fileName);
            if (!displayName.Contains(_swfFileFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;
            string relativePath = Path.GetRelativePath(brawlPath, absolutePath);

            ImGui.PushID(fileName);
            if (assetLoader.IsSwfLoading(relativePath))
            {
                ImGui.Text(displayName);
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    assetLoader.SwfFileCache.RemoveCached(absolutePath);
                }
                ImGui.SameLine();
                ImGui.TextDisabled("Loading...");
            }
            else if (assetLoader.TryGetSwf(relativePath, out SwfFileData? swf))
            {
                void unloadButton()
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Unload"))
                    {
                        FileUnloaded?.Invoke(this, relativePath);
                        assetLoader.SwfFileCache.RemoveCached(absolutePath);
                    }
                }

                ImGui.SetNextItemAllowOverlap();
                if (ImGui.TreeNode(displayName))
                {
                    unloadButton();

                    string filter = _swfFilterState.GetValueOrDefault(fileName, "");
                    if (ImGui.InputText("Filter sprites", ref filter, 256))
                    {
                        _swfFilterState[fileName] = filter;
                    }

                    if (ImGui.BeginListBox(""))
                    {
                        foreach ((_, DefineSpriteTag spriteTag) in swf.SpriteTags)
                        {
                            if (!swf.ReverseSymbolClass.TryGetValue(spriteTag.SpriteID, out string? spriteName))
                                continue;
                            if (!spriteName.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                                continue;

                            bool selected =
                                fileName == info.AnimFile &&
                                spriteName == info.AnimClass &&
                                string.IsNullOrEmpty(info.Animation);

                            if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                            if (ImGui.Selectable(spriteName, selected))
                            {
                                info.SourceFilePath = relativePath;
                                info.AnimFile = fileName;
                                info.AnimClass = spriteName;
                                info.Animation = "";
                            }
                            if (selected) ImGui.PopStyleColor();
                        }
                        ImGui.EndListBox();
                    }

                    ImGui.TreePop();
                }
                else
                {
                    unloadButton();
                }
            }
            else
            {
                ImGui.Text(displayName);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    async Task load()
                    {
                        await assetLoader.LoadSwf(relativePath);
                    }
                    _ = load();
                }
            }
            ImGui.PopID();
        }
    }
}