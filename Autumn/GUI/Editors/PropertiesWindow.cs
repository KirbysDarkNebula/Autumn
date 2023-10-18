using System.Numerics;
using Autumn.GUI;
using Autumn.Scene;
using Autumn.Storage;
using ImGuiNET;

namespace Autumn;

internal static class PropertiesWindow
{
    public static void Render(MainWindowContext context)
    {
        if (!ImGui.Begin("Properties"))
            return;

        if (context.CurrentScene is null)
        {
            ImGui.TextDisabled("Please open a stage.");
            ImGui.End();
            return;
        }

        List<SceneObj> selectedObjects = context.CurrentScene.SelectedObjects;
        int selectedCount = selectedObjects.Count;

        if (selectedCount <= 0)
        {
            ImGui.TextDisabled("No object is selected.");
            ImGui.End();
            return;
        }

        if (selectedCount == 1)
        {
            // Only one object selected:
            SceneObj sceneObj = selectedObjects[0];
            StageObj stageObj = sceneObj.StageObj;

            ImGui.InputText("Name", ref stageObj.Name, 128);
            ImGui.InputText("Layer", ref stageObj.Layer, 30);

            if (stageObj.ID != -1)
                ImGui.InputInt("ID", ref stageObj.ID);

            if (ImGui.InputFloat3("Translation", ref stageObj.Translation))
                sceneObj.UpdateTransform();

            if (ImGui.InputFloat3("Rotation", ref stageObj.Rotation))
                sceneObj.UpdateTransform();

            if (ImGui.InputFloat3("Scale", ref stageObj.Scale))
                sceneObj.UpdateTransform();

            foreach (var (name, property) in stageObj.Properties)
            {
                if (property.Value is null)
                {
                    ImGui.TextDisabled(name);
                    return;
                }

                switch (property.Value)
                {
                    case object p when p is int:
                        int intBuf = (int)(p ?? -1);
                        if (ImGui.InputInt(name, ref intBuf))
                            property.Value = intBuf;

                        break;

                    case object p when p is string:
                        string strBuf = (string)(p ?? string.Empty);
                        if (ImGui.InputText(name, ref strBuf, 128))
                            property.Value = strBuf;

                        break;

                    default:
                        throw new NotImplementedException(
                            "The property type " + property.Value?.GetType().FullName
                                ?? "null" + " is not supported."
                        );
                }
            }
        }
        else
        {
            // Multiple objects selected:
        }

        ImGui.End();
    }
}
