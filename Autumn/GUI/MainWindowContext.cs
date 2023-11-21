﻿using Autumn.Background;
using Autumn.Commands;
using Autumn.GUI.Editors;
using Autumn.IO;
using Autumn.Scene;
using Autumn.Storage;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using TinyFileDialogsSharp;

namespace Autumn.GUI;

internal class MainWindowContext : WindowContext
{
    public List<Scene.Scene> Scenes { get; } = new();
    public Scene.Scene? CurrentScene { get; set; }

    public SceneGL.GLWrappers.Framebuffer SceneFramebuffer { get; }

    public BackgroundManager BackgroundManager { get; } = new();

    private bool _isFirstFrame = true;

    private bool _closingDialogOpened = false;

    private bool _stageSelectOpened = false;
    private string _stageSearchInput = string.Empty;

#if DEBUG
    private bool _showDemoWindow = false;
#endif

    public MainWindowContext()
        : base()
    {
        Window.Title = "Autumn: Stage Editor";

        SceneFramebuffer = new(
            null,
            SceneGL.PixelFormat.D24_UNorm_S8_UInt,
            SceneGL.PixelFormat.R8_G8_B8_A8_UNorm,
            SceneGL.PixelFormat.R32_UInt
        );

        Window.Load += () =>
        {
            InfiniteGrid.Initialize(GL!);
            ModelRenderer.Initialize(GL!);

            ImGuiIOPtr io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigWindowsMoveFromTitleBarOnly = true;
        };

        Window.Closing += () =>
        {
            if (BackgroundManager.IsBusy)
            {
                _closingDialogOpened = true;
                Window.IsClosing = false;
            }
        };

        Window.Render += (deltaSeconds) =>
        {
            if (ImGuiController is null)
                return;

            ImGuiController.MakeCurrent();

            if (_isFirstFrame)
            {
                ImGui.LoadIniSettingsFromDisk(ImguiSettingsFile);
                _isFirstFrame = false;
            }

            #region DockSpace

            float barHeight = 17;

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

            ImGui.SetNextWindowPos(viewport.Pos + new Vector2(0, barHeight));
            ImGui.SetNextWindowSize(viewport.Size - new Vector2(0, barHeight * 2));
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.Begin(
                "mainDockSpaceWindow",
                ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoSavedSettings
            );
            ImGui.PopStyleVar(2);

            RenderMainMenuBar(barHeight);

            ImGui.DockSpace(ImGui.GetID("mainDockSpace"));
            ImGui.End();

            #endregion

            RenderStatusBar(barHeight, viewport.Size);

            if (!ProjectHandler.ProjectLoaded)
                RenderNoProjectScreen();
            else
                RenderEditors(deltaSeconds);

#if DEBUG
            if (_showDemoWindow)
                ImGui.ShowDemoWindow(ref _showDemoWindow);
#endif

            if (_stageSelectOpened)
                RenderStageSelectPopup();

            if (_closingDialogOpened)
                RenderClosingDialog();

            GL!.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL!.Clear(ClearBufferMask.ColorBufferBit);
            GL!.Viewport(Window.FramebufferSize);
            ImGuiController.Render();
        };
    }

    private void RenderMainMenuBar(float height)
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        Debug.Assert(height != ImGui.GetItemRectSize().Y);

        if (ImGui.BeginMenu("Project"))
        {
            ImGuiWidgets.CommandMenuItem(CommandID.NewProject);
            ImGuiWidgets.CommandMenuItem(CommandID.OpenProject);

            if (ImGui.BeginMenu("Recent"))
            {
                if (RecentHandler.RecentOpenedPaths.Count <= 0)
                    ImGui.TextDisabled("There are no recent entries.");
                else
                {
                    foreach (string path in RecentHandler.RecentOpenedPaths)
                        if (ImGui.Selectable(path))
                        {
                            ProjectHandler.LoadProject(path);
                            break;
                        }
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (
                ImGui.MenuItem("Exit") /* && Project.Unload() */
            )
                Window.Close();

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Stage"))
        {
            if (ImGui.MenuItem("Save", CurrentScene is not null))
                BackgroundManager.Add(
                    $"Saving stage \"{CurrentScene!.Stage.Name + CurrentScene!.Stage.Scenario}\"...",
                    () => StageHandler.SaveProjectStage(CurrentScene!.Stage)
                );

            if (ImGui.MenuItem("Import from RomFS", ProjectHandler.ProjectLoaded))
                _stageSelectOpened |= true;

            //ImGui.MenuItem("Import through world map selector");

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("Show welcome window"))
                WindowManager.Add(new WelcomeWindowContext());

#if DEBUG
            if (ImGui.MenuItem("Show demo window"))
                _showDemoWindow = true;
#endif

            ImGui.EndMenu();
        }

        #region SceneTabs

        ImGuiTabBarFlags barFlags = ImGuiTabBarFlags.AutoSelectNewTabs;
        int sceneCount = Scenes.Count;

        if (sceneCount > 0 && ImGui.BeginTabBar("sceneTabs", barFlags))
        {
            for (int i = 0; i < sceneCount; i++)
            {
                ImGuiTabItemFlags flags = ImGuiTabItemFlags.NoPushId;

                Scene.Scene scene = Scenes[i];

                if (!scene.Stage.Saved)
                    flags |= ImGuiTabItemFlags.UnsavedDocument;

                bool opened = true;

                ImGui.PushID(scene.Stage.Name + scene.Stage.Scenario);

                if (
                    ImGui.BeginTabItem(scene.Stage.Name + scene.Stage.Scenario, ref opened, flags)
                    && CurrentScene != scene
                )
                    CurrentScene = scene;

                ImGui.EndTabItem();

                ImGui.PopID();

                if (!opened && Scenes.Remove(scene))
                {
                    // TO-DO: Check whether the stage is not saved.

                    i--;
                    sceneCount = Scenes.Count;

                    if (i < 0)
                        CurrentScene = null;
                    else
                        CurrentScene = Scenes[i];
                }
            }

            ImGui.EndTabBar();
        }

        #endregion

        ImGui.EndMainMenuBar();

        return;
    }

    private void RenderStatusBar(float height, Vector2 viewportSize)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
        ImGui.SetNextWindowPos(new(0, viewportSize.Y - height), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new(viewportSize.X, height));

        if (
            !ImGui.Begin(
                "StatusBar",
                ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.NoInputs
            )
        )
            return;

        ImGui.Text(BackgroundManager.StatusMessage);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void RenderEditors(double deltaSeconds)
    {
        StageWindow.Render(this);
        ObjectWindow.Render(this);
        PropertiesWindow.Render(this);
        SceneWindow.Render(this, deltaSeconds);
    }

    private static void RenderNoProjectScreen()
    {
        ImGuiWindowFlags flags =
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoSavedSettings;

        ImGui.SetNextWindowPos(
            ImGui.GetWindowViewport().GetCenter(),
            ImGuiCond.Always,
            new(0.5f, 0.5f)
        );

        if (!ImGui.Begin("##", flags))
            return;

        ImGui.TextDisabled("Please, open a project from the menu or drop a folder here.");

        ImGui.End();
    }

    private void RenderStageSelectPopup()
    {
        if (!RomFSHandler.RomFSAvailable)
        {
            _stageSelectOpened = false;
            return;
        }

        ImGui.OpenPopup("Stage selector");

        Vector2 dimensions = new Vector2(450, 185) + ImGui.GetStyle().ItemSpacing;
        ImGui.SetNextWindowSize(dimensions, ImGuiCond.Always);

        ImGui.SetNextWindowPos(
            ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing,
            new(0.5f, 0.5f)
        );

        if (
            !ImGui.BeginPopupModal(
                "Stage selector",
                ref _stageSelectOpened,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings
            )
        )
            return;

        ImGui.Text("Search:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(450 - ImGui.GetCursorPosX());

        ImGui.InputTextWithHint(
            "",
            "Insert the name of the stage here.",
            ref _stageSearchInput,
            128
        );

        ImGui.SetNextItemWidth(450 - ImGui.GetCursorPosX());

        if (ImGui.BeginListBox(""))
        {
            foreach (var (name, scenario) in RomFSHandler.StageNames)
            {
                if (
                    !string.IsNullOrEmpty(_stageSearchInput)
                    && !name.Contains(
                        _stageSearchInput,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                    continue;

                // Does not show the stage if there is an already opened one with the same name and scenario:
                if (
                    Scenes.Find(
                        scene => scene.Stage.Name == name && scene.Stage.Scenario == scenario
                    )
                    is not null
                )
                    continue;

                if (ImGui.Selectable(name + scenario, false, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    _stageSelectOpened = false;

                    BackgroundManager.Add(
                        $"Importing stage \"{name + scenario}\" from RomFS...",
                        () =>
                        {
                            if (!StageHandler.TryImportStage(name, scenario, out Stage stage))
                            {
                                ImGui.CloseCurrentPopup();
                                ImGui.EndPopup();
                                return;
                            }

                            ProjectHandler.ActiveProject.Stages.Add(stage);
                        }
                    );

                    ImGui.CloseCurrentPopup();
                }
            }
        }

        ImGui.EndPopup();
    }

    // This dialog is shown when there are background tasks being executed.
    private void RenderClosingDialog()
    {
        ImGui.OpenPopup("##ClosingDialog");

        Vector2 dimensions = new Vector2(450, 185) + ImGui.GetStyle().ItemSpacing;
        ImGui.SetNextWindowSize(dimensions, ImGuiCond.Always);

        ImGui.SetNextWindowPos(
            ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing,
            new(0.5f, 0.5f)
        );

        if (
            !ImGui.BeginPopupModal(
                "##ClosingDialog",
                ref _closingDialogOpened,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings
            )
        )
            return;

        ImGui.TextWrapped(
            "Please wait for the following tasks to finish before exiting the program:"
        );

        ImGui.Spacing();

        foreach (var (message, _) in BackgroundManager.GetRemainingTasks())
        {
            if (message is null)
                ImGui.TextDisabled("BackgroundTask");
            else
                ImGui.Text(message);
        }

        if (!BackgroundManager.IsBusy)
            Window.Close();

        ImGui.EndPopup();
    }
}
