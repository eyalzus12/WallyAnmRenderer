using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using WallyAnmSpinzor;

namespace WallyAnmRenderer;

public sealed class AnmWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<string>? AnmUnloadRequested;

    private string _fileFilter = "";
    private readonly Dictionary<string, string> _animationFilterState = [];

    public void Show(string? brawlPath, AssetLoader assetLoader, GfxInfo info)
    {
        ImGui.Begin("Animations", ref _open);

        if (brawlPath is null)
        {
            ImGui.Text("You must select your brawlhalla path to choose an animation");
            return;
        }

        string brawlAnimPath = Path.Join(brawlPath, "anims");
        string[] files = Directory.GetFiles(brawlAnimPath);

        ImGui.InputText("Filter files", ref _fileFilter, 256);
        IEnumerable<string> filteredFiles = files.Where((file) => file.Contains(_fileFilter, StringComparison.InvariantCultureIgnoreCase));

        foreach (string absolutePath in filteredFiles)
        {
            if (!absolutePath.EndsWith(".anm")) continue;

            string fileName = Path.GetRelativePath(brawlAnimPath, absolutePath);
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
                                IEnumerable<string> filteredAnimations = anmClass.Animations.Keys.Where(a => a.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
                                foreach (string animation in filteredAnimations)
                                {
                                    if (ImGui.Selectable(animation, animation == info.Animation))
                                    {
                                        info.SourceFilePath = relativePath;
                                        info.AnimFile = animFile;
                                        info.AnimClass = animClass;
                                        info.Animation = animation;
                                    }
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