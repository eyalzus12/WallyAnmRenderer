using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AnmWindow
{
    private static readonly Vector4 SELECTED_COLOR = ImGuiEx.RGBHexToVec4(0xFF7F00);

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<string>? AnmUnloadRequested;

    private string _fileFilter = "";
    private readonly Dictionary<string, string> _animationFilterState = [];

    public void Show(string? brawlPath, AssetLoader? assetLoader, GfxInfo info)
    {
        ImGui.Begin("Animations", ref _open);

        if (brawlPath is null)
        {
            ImGui.Text("You must select your brawlhalla path to choose an animation");
            ImGui.End();
            return;
        }

        if (assetLoader is null)
        {
            ImGui.End();
            return;
        }

        string brawlAnimPath = Path.Join(brawlPath, "anims");
        string[] files = Directory.GetFiles(brawlAnimPath);

        ImGui.InputText("Filter files", ref _fileFilter, 256);
        foreach (string absolutePath in files)
        {
            if (!absolutePath.EndsWith(".anm")) continue;

            string fileName = Path.GetRelativePath(brawlAnimPath, absolutePath);
            if (!fileName.Contains(_fileFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;
            string relativePath = Path.GetRelativePath(brawlPath, absolutePath);

            if (assetLoader.IsAnmLoading(relativePath))
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                ImGui.TextDisabled(" Loading...");
            }
            else if (assetLoader.TryGetAnm(relativePath, out AnmFile? anm))
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button($"Unload##{fileName}"))
                {
                    AnmUnloadRequested?.Invoke(this, relativePath);
                    assetLoader.AnmFileCache.RemoveCached(absolutePath);
                }

                if (ImGui.TreeNode($"Animations##{anm.GetHashCode()}"))
                {
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
            }
            else
            {
                ImGui.Text(fileName);
                ImGui.SameLine();
                if (ImGui.Button($"Load##{fileName}"))
                {
                    assetLoader.LoadAnm(relativePath);
                }
            }
        }

        ImGui.End();
    }
}