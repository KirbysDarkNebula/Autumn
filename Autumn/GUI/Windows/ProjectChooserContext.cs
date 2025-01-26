using Autumn.Context;
using ImGuiNET;

namespace Autumn.GUI.Windows;

internal class ProjectChooserContext : FileChooserWindowContext
{
    private const ImGuiTableFlags _fileChooseFlags =
        ImGuiTableFlags.ScrollY
        | ImGuiTableFlags.BordersOuterH
        | ImGuiTableFlags.Reorderable
        | ImGuiTableFlags.NoSavedSettings;

    private const ImGuiSelectableFlags _fileSelectableFlags =
        ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

    public ProjectChooserContext(ContextHandler contextHandler, WindowManager windowManager)
        : base(contextHandler, windowManager) { }

    protected override void RenderFileChoosePanel()
    {
        if (!ImGui.BeginTable("FileChoose", 2, _fileChooseFlags))
            return;

        float tableWidth = ImGui.CalcItemWidth();

        ImGui.TableSetupScrollFreeze(0, 1); // Makes top row always visible.
        ImGui.TableSetupColumn(" Name");
        ImGui.TableSetupColumn(" Modified Date");
        ImGui.TableHeadersRow();

        foreach (FileSystemInfo info in DirectoryEntries)
        {
            if (info is not DirectoryInfo dir)
                continue;

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);

            ImGui.SetNextItemWidth(tableWidth);

            if (ImGui.Selectable(info.Name, false, _fileSelectableFlags))
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    ChangeDirectory(dir.FullName);
                    break;
                }
            }

            ImGui.TableNextColumn();

            ImGui.Text(dir.LastWriteTime.ToString());
            ImGui.TableNextColumn();
        }

        ImGui.EndTable();
    }
}
