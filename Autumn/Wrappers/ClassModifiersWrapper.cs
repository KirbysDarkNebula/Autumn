using System.Numerics;
using Autumn.Enums;

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
        public string? ModelReplace { get; set; } // Implemented
        public Dictionary<string, ExtraModel?>? ExtraModels { get; set; } // Implemented
        public Vector3? Translation { get; set; } // Implemented
        public Vector3? Scale { get; set; } // Implemented
        public Vector3? Rotation { get; set; } // Implemented
        public List<string>? HiddenMeshes { get; set; } // Implemented
        public Dictionary<string, byte>? MeshesPriority { get; set; }
        public Dictionary<string, BaseArgChange>? Args { get; set; } // ArgID, Arg
    }
    public struct ExtraModel
    {
        public Vector3? Translation { get; set; }
        public Vector3? Scale { get; set; }
        public Vector3? Rotation { get; set; }
    }
    /// <summary>
    /// Temporary replacement for args
    /// </summary>
    [Obsolete]
    public struct BaseArgChange : ArgChange
    {
        public ArgType ArgType { get; set; }

        // Tower
        public string? RepeatModel { get; set; } // If no RepeatModel -> use basemodel
        public bool? CountTop { get; set; }
        public Vector3? Offset { get; set; }

        //Animation
        public string? AnimName { get; set; } // animation to use
        public H3DAnimation? AnimType { get; set; } // type of the animation
    }

    public interface ArgChange
    {
        public ArgType ArgType { get; set; }
    }
    public struct TowerArg : ArgChange
    {
        public ArgType ArgType { get; set; }
        public string? RepeatModel { get; set; } // If no RepeatModel -> use basemodel
        public bool StartAtZero { get; set; } // whether the top model disappears at 0 or not
        public Vector3 Offset { get; set; }
    }
    /// <summary>
    /// Plays the specific frame of an animation in the model file 
    /// Can be used for material and skeleton animations
    /// Uses value of the Arg as the frame
    /// </summary>
    public struct AnimChangeArg : ArgChange
    {
        public ArgType ArgType { get; set; }
        public string AnimName { get; set; } // animation to use
        public H3DAnimation AnimType { get; set; } // type of the animation
        // public int Frame { get; set; } // animation frame to use
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
                // ModifierBase bbb = new() { Variants = new() };
                // bbb.Variants.Add("CoinCollect4", new());
                // bbb.Variants["CoinCollect4"] = new(){ Translation = new() { X = 0, Y = 100, Z = 0}, HiddenMeshes = ["BlockQuestionEmpty", "BlockQuestionTest"], 
                // Args = [ new AnimChangeArg() { AnimName = "Color", AnimType = H3DAnimation.MaterialAnim, Frame = 0, ID = 0 } ]   };
            
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

    public static ModifierEntry? GetEntry(string actorName, string className)
    {
        if (s_ModifierEntries is null || !s_ModifierEntries.ContainsKey(className))
        {
            return null;
        }

        if (s_ModifierEntries[className].Variants != null && s_ModifierEntries[className].Variants!.ContainsKey(actorName))
            return s_ModifierEntries[className].Variants![actorName];
        else if (s_ModifierEntries[className].Default != null)
            return s_ModifierEntries[className].Default;
        else
            return null;
    }

}
