using System.Numerics;
using Autumn.Enums;
using SharpYaml.Serialization;

namespace Autumn.Wrappers;

internal static class ClassModifiersWrapper
{
    public struct ModifierBase
    {
        public ModifierEntry? Default { get; set; }
        public Dictionary<string, ModifierEntry>? Variants { get; set; }
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
        public Dictionary<string, ArgChange>? Args { get; set; } // ArgID, Arg
        [YamlIgnore]
        public Dictionary<string, BaseArg>? ArgsRem { get; set; } // ArgID, Arg
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
    public struct BaseArgChange
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

    public class ArgChange
    {
        public ArgType ArgType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
    public interface BaseArg
    {

    }
    public class SimpleArg : BaseArg
    {
        public byte BasicTestValue = 0; 
    }
    public class TowerArg : BaseArg
    {
        public string? RepeatModel { get; set; } // If no RepeatModel -> use basemodel
        public bool? CountTop { get; set; } // whether the top model disappears at 0 or not
        public Vector3 Offset { get; set; }
        public TowerArg(Dictionary<string, object> props)
        {
            object? v;
            if (props.TryGetValue("RepeatModel", out v))
                RepeatModel = (string)v;
            if (props.TryGetValue("CountTop", out v))
                CountTop = (bool)v;
            if (props.TryGetValue("Offset", out v))
                Offset = FromDict((Dictionary<object,object>)v);
        }
    }
    /// <summary>
    /// Plays the specific frame of an animation in the model file 
    /// Can be used for material and skeleton animations
    /// Uses value of the Arg as the frame
    /// </summary>
    public class AnimChangeArg : BaseArg
    {
        public string AnimName { get; set; } // animation to use
        public H3DAnimation AnimType { get; set; } // type of the animation
        // public int Frame { get; set; } // animation frame to use
        public AnimChangeArg(Dictionary<string, object> props)
        {
            object? v;
            if (props.TryGetValue("AnimName", out v))
                AnimName = (string)v;
            if (props.TryGetValue("AnimType", out v))
                AnimType = Enum.Parse<H3DAnimation>((string)v);
        }
    }

    /// <summary>
    /// Converts the X Y Z dictionary to a Vector3, assumes the dictionary is formatted properly
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static Vector3 FromDict(Dictionary<object, object> o) => new Vector3(Convert.ToSingle(o["X"]), Convert.ToSingle(o["Y"]), Convert.ToSingle(o["Z"])); 

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
                    if (entry.Default != null && entry.Default.Value.Args != null)
                    {
                        var vl = entry.Default.Value;
                        vl.ArgsRem = new();
                        entry.Default = vl;
                        foreach (string k in entry.Default.Value.Args.Keys)
                        {
                            BaseArg arg;
                            switch (entry.Default.Value.Args[k].ArgType)
                            {
                                case ArgType.Tower:
                                    arg = new TowerArg(entry.Default.Value.Args[k].Properties);
                                break;
                                case ArgType.AnimChange:
                                    arg = new AnimChangeArg(entry.Default.Value.Args[k].Properties);
                                break;
                                default:
                                    arg = new SimpleArg();
                                break;
                            }
                            entry.Default.Value.ArgsRem.Add(k, arg);
                        }
                    }
                    if (entry.Variants != null)
                    foreach (string ent in entry.Variants.Keys)
                    {           
                        if (entry.Variants[ent]!.Args == null) continue;
                        var vl = entry.Variants[ent]!;
                        vl.ArgsRem = new();
                        entry.Variants[ent] = vl;
                        foreach (string k in entry.Variants[ent]!.Args!.Keys)
                        {
                            BaseArg arg;
                            switch (entry.Variants[ent]!.Args![k].ArgType)
                            {
                                case ArgType.Tower:
                                    arg = new TowerArg(entry.Variants[ent]!.Args![k].Properties);
                                break;
                                case ArgType.AnimChange:
                                    arg = new AnimChangeArg(entry.Variants[ent]!.Args![k].Properties);
                                break;
                                default:
                                    arg = new SimpleArg();
                                break;
                            }
                            entry.Variants[ent]!.ArgsRem!.Add(k, arg);
                        }
                    }
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
