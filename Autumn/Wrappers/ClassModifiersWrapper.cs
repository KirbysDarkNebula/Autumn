using System.Numerics;

namespace Autumn.Wrappers;

internal static class ClassModifiersWrapper
{
    public struct ModifierBase
    {
        public ModifierEntry? Default { get; set; }
        public Dictionary<string, ModifierEntry?>? Variants { get; set; }
    }
    public struct ModifierEntry
    {
        public string? ModelReplace { get; set; }
        public Dictionary<string, ExtraModel?>? ExtraModels { get; set; }
        public Vector3? Translation { get; set; }
        public Vector3? Scale { get; set; }
        public Vector3? Rotation { get; set; }
        public List<string>? HiddenMeshes { get; set; }
        public Dictionary<string, byte>? MeshesPriority { get; set; }
        public List<ArgChange>? Args { get; set; }
    }
    public struct ExtraModel
    {
        public Vector3? Translation { get; set; }
        public Vector3? Scale { get; set; }
        public Vector3? Rotation { get; set; }
    }
    public interface ArgChange
    {
        public int ID { get; set; }
    }
    public struct TowerArg : ArgChange
    {
        public int ID { get; set; }
        public string? RepeatModel { get; set; }
        public Vector3 Offset { get; set; }
    }
    public struct MaterialChangeArg : ArgChange
    {
        public int ID { get; set; }
        public int Frame { get; set; } // animation frame to use
    }

    private static SortedDictionary<string, ModifierBase>? s_ModifierEntries = null;

    public static SortedDictionary<string, ModifierBase> ModifierEntries
    {
        get
        {
            if (s_ModifierEntries is null || ReloadEntries)
            {
                s_ModifierEntries = new();
                string path = Path.Join("Resources", "Modifiers");
                // ModifierBase bbb = new() { Variants = new(), TEST = new Vector3(30, 20, 0)};
                // bbb.Variants.Add("CoinCollect4", new());
                // bbb.Variants["CoinCollect4"] = new(){ Translation = new() { X = 0, Y = 100, Z = 0}, HiddenMeshes = ["BlockQuestionEmpty", "BlockQuestionTest"]};
                // YAMLWrapper.Serialize<ModifierBase>(Path.Join(path, "CoinCollect4.yml"), bbb);
                foreach (string entryPath in Directory.EnumerateFiles(path))
                {
                    ModifierBase entry = YAMLWrapper.Deserialize<ModifierBase>(entryPath);
                    s_ModifierEntries[Path.GetFileNameWithoutExtension(entryPath)] = entry;
                }
                ReloadEntries = false;
            }

            return s_ModifierEntries;
        }
    }

    public static bool ReloadEntries = false;
}
