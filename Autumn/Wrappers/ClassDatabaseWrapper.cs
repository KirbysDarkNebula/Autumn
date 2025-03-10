namespace Autumn.Wrappers;

internal static class ClassDatabaseWrapper
{
    public struct DatabaseEntry
    {
        public Dictionary<string, Arg> Args { get; set; }
        public string ClassName { get; set; }
        public string ClassNameFull { get; set; }
        public string Description { get; set; }
        public string DescriptionAdditional { get; set; }
        public string? Name { get; set; }
        public bool RailRequired { get; set; }
        public string? ArchiveName { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, Switch?> Switches { get; set; }
    }

    public struct Arg
    {
        public object Default { get; set; }
        public string? Type { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public bool Required { get; set; }
        public Dictionary<int, string>? Values { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
    }

    public struct Switch
    {
        public string Description { get; set; }
        public string Type { get; set; }
    }

    private static SortedDictionary<string, DatabaseEntry>? s_DatabaseEntries = null;

    public static SortedDictionary<string, DatabaseEntry> DatabaseEntries
    {
        get
        {
            if (s_DatabaseEntries is null || ReloadEntries)
            {
                s_DatabaseEntries = new();
                string path = Path.Join(Path.Join("Resources", "RedPepper-ClassDataBase"), "Data");

                foreach (string entryPath in Directory.EnumerateFiles(path))
                {
                    DatabaseEntry entry = YAMLWrapper.Deserialize<DatabaseEntry>(entryPath);

                    if(entry.ClassName is not null)
                        s_DatabaseEntries[entry.ClassName] = entry;
                }
                ReloadEntries = false;
            }

            return s_DatabaseEntries;
        }
    }

    public static bool ReloadEntries = false;
}
