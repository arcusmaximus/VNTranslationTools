using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class TextScript : IScript
    {
        private string _filePath;

        public string Extension => ".txt";

        public void Load(ScriptLocation location)
        {
            _filePath = location.ToFilePath();
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            using StreamReader reader = new StreamReader(_filePath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Match match = Regex.Match(line, @"^(<(?<name>.+?)>)?(?<text>.+)$");
                if (!match.Success)
                    continue;

                if (match.Groups["name"].Success)
                {
                    IEnumerable<string> names = SplitNames(match.Groups["name"].Value);
                    foreach (string name in names)
                    {
                        yield return new ScriptString(name, ScriptStringType.CharacterName);
                    }
                }

                string text = StringUtil.UnescapeC(match.Groups["text"].Value);
                yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using StreamWriter writer = new StreamWriter(location.ToFilePath());
            List<string> pendingNames = new List<string>();
            foreach (ScriptString str in strings)
            {
                if (str.Type == ScriptStringType.CharacterName)
                {
                    pendingNames.Add(str.Text);
                }
                else
                {
                    if (pendingNames.Count > 0)
                    {
                        writer.Write($"<{JoinNames(pendingNames)}>");
                        pendingNames.Clear();
                    }

                    writer.WriteLine(StringUtil.EscapeC(str.Text));
                }
            }
        }

        private static string JoinNames(IEnumerable<string> names)
        {
            return string.Join("/", names.Select(EscapeName));
        }

        private static string EscapeName(string name)
        {
            return name.Replace(@"\", @"\\").Replace("/", "\\/");
        }

        private static IEnumerable<string> SplitNames(string names)
        {
            int pos = 0;
            StringBuilder currentName = new StringBuilder();
            while (pos < names.Length)
            {
                switch (names[pos])
                {
                    case '\\':
                        if (pos < names.Length - 1)
                        {
                            currentName.Append(names[pos + 1]);
                            pos += 2;
                        }
                        else
                        {
                            pos++;
                        }
                        break;

                    case '/':
                        yield return currentName.ToString();
                        currentName.Clear();
                        pos++;
                        break;

                    default:
                        currentName.Append(names[pos++]);
                        break;
                }
            }
            yield return currentName.ToString();
        }
    }
}
