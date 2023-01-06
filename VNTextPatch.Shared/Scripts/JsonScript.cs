using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace VNTextPatch.Shared.Scripts
{
    public class JsonScript : IScript
    {
        public string Extension => ".json";

        private Entry[] _entries;

        public void Load(ScriptLocation location)
        {
            using StreamReader reader = new StreamReader(location.ToFilePath());
            JsonSerializer serializer = new JsonSerializer();
            _entries = serializer.Deserialize<Entry[]>(new JsonTextReader(reader));
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (Entry entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    yield return new ScriptString(entry.Name, ScriptStringType.CharacterName);
                }
                else if (entry.Names != null)
                {
                    foreach (string name in entry.Names)
                    {
                        yield return new ScriptString(name, ScriptStringType.CharacterName);
                    }
                }

                yield return new ScriptString(entry.Message, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            List<Entry> entries = new List<Entry>();
            Entry pendingEntry = null;
            foreach (ScriptString str in strings)
            {
                if (str.Type == ScriptStringType.CharacterName)
                {
                    if (pendingEntry == null)
                    {
                        pendingEntry = new Entry { Name = str.Text };
                    }
                    else
                    {
                        if (pendingEntry.Names == null)
                        {
                            pendingEntry.Names = new List<string> { pendingEntry.Name };
                            pendingEntry.Name = null;
                        }
                        pendingEntry.Names.Add(str.Text);
                    }
                }
                else
                {
                    if (pendingEntry != null)
                    {
                        pendingEntry.Message = str.Text;
                        entries.Add(pendingEntry);
                        pendingEntry = null;
                    }
                    else
                    {
                        entries.Add(new Entry { Message = str.Text });
                    }
                }
            }

            using Stream stream = File.Open(location.ToFilePath(), FileMode.Create);
            using StreamWriter writer = new StreamWriter(stream);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(new JsonTextWriter(writer) { Formatting = Formatting.Indented }, entries);
        }

        private class Entry
        {
            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name
            {
                get;
                set;
            }

            [JsonProperty("names", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Names
            {
                get;
                set;
            }

            [JsonProperty("message")]
            public string Message
            {
                get;
                set;
            }
        }
    }
}
