using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    public class ArtemisAstScript : IScript
    {
        private static readonly Regex CommandRegex = new Regex(@"\[(?<cmd>/?\w+)(?:\s+(?<attrname>\w+)\s*=\s*(?<attrvalue>""(?:\\.|[^""])*""|[-+\.\d]+))*\]");

        private readonly List<ILuaNode> _rootNodes = new List<ILuaNode>();
        private LuaTable _ast;

        public string Extension => ".ast";

        public void Load(ScriptLocation location)
        {
            string doc = File.ReadAllText(location.ToFilePath());

            _rootNodes.Clear();
            int pos = 0;
            while (true)
            {
                ILuaNode value = LuaParser.Read(doc, ref pos);
                if (value == null)
                    break;

                _rootNodes.Add(value);
            }

            _ast = _rootNodes.OfType<LuaAttribute>()
                             .FirstOrDefault(n => n.Name == "ast")
                             ?.Value as LuaTable;
            if (_ast == null)
                throw new InvalidDataException("No \"ast\" attribute found in file");
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (LuaTable line in GetLineTables())
            {
                StringBuilder text = new StringBuilder();
                foreach (ILuaNode item in line)
                {
                    switch (item)
                    {
                        case LuaAttribute attr:
                            if (attr.Name == "name" && attr.Value is LuaTable names)
                            {
                                foreach (LuaString name in names.OfType<LuaString>())
                                {
                                    yield return new ScriptString(name.Value, ScriptStringType.CharacterName);
                                }
                            }
                            else
                            {
                                throw new InvalidDataException($"Encountered unknown attribute \"{attr.Name}\" in message");
                            }
                            break;

                        case LuaString str:
                            text.Append(str.Value);
                            break;

                        case LuaTable table:
                            if (table.Count == 1 && table[0] is LuaString cmdName && cmdName.Value == "rt2")
                                text.AppendLine();
                            else
                                TableToCommand(table, text);

                            break;

                        default:
                            throw new InvalidDataException("Message contains invalid item types");
                    }
                }
                yield return new ScriptString(text.ToString().Trim(), ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            foreach (LuaTable line in GetLineTables())
            {
                line.Clear();
                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Not enough strings in translation");

                LuaTable names = null;
                while (stringEnumerator.Current.Type == ScriptStringType.CharacterName)
                {
                    names ??= new LuaTable();
                    names.Add(new LuaString(stringEnumerator.Current.Text));
                    if (!stringEnumerator.MoveNext())
                        throw new InvalidDataException("Not enough strings in translation");
                }
                if (names != null)
                    line.Add(new LuaAttribute("name", names));

                string text = ProportionalWordWrapper.Default.Wrap(stringEnumerator.Current.Text, CommandRegex);
                foreach ((string segment, Match match) in StringUtil.GetMatchingAndSurroundingTexts(text, new Regex(@"\r\n|\[.+?\]")))
                {
                    if (segment != null)
                        line.Add(new LuaString(segment));
                    else if (match.Value == "\r\n")
                        line.Add(new LuaTable { new LuaString("rt2") });
                    else
                        line.Add(CommandToTable(match.Value));
                }
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many strings in translation");

            using Stream stream = File.Create(location.ToFilePath());
            using StreamWriter writer = new StreamWriter(stream);
            foreach (ILuaNode node in _rootNodes)
            {
                writer.WriteLine(node);
            }
        }

        private IEnumerable<LuaTable> GetLineTables()
        {
            foreach (LuaTable block in _ast.OfType<LuaAttribute>()
                                           .Select(a => a.Value)
                                           .OfType<LuaTable>())
            {
                LuaTable text = block["text"] as LuaTable;
                if (text == null)
                    continue;

                LuaTable ja = text["ja"] as LuaTable;
                if (ja == null || ja.Count != 1)
                    continue;

                LuaTable line = ja[0] as LuaTable;
                if (line == null)
                    continue;

                yield return line;
            }
        }

        private static void TableToCommand(LuaTable table, StringBuilder cmd)
        {
            cmd.Append("[");

            if (table.Count == 0 || !(table[0] is LuaString cmdName))
                throw new InvalidDataException("Command table must start with a string");

            cmd.Append(cmdName.Value);

            for (int i = 1; i < table.Count; i++)
            {
                cmd.Append(" ");
                if (!(table[i] is LuaAttribute attr))
                    throw new InvalidDataException("Command table must only contain a name and attributes");

                cmd.Append($"{attr.Name}={attr.Value}");
            }

            cmd.Append("]");
        }

        private static LuaTable CommandToTable(string cmd)
        {
            Match match = CommandRegex.Match(cmd);
            if (!match.Success)
                throw new InvalidDataException($"Failed to parse command {cmd}");

            LuaTable result = new LuaTable();
            result.Add(new LuaString(match.Groups["cmd"].Value));
            for (int i = 0; i < match.Groups["attrname"].Captures.Count; i++)
            {
                string name = match.Groups["attrname"].Captures[i].Value;
                string value = match.Groups["attrvalue"].Captures[i].Value;
                ILuaNode valueNode;
                if (value.StartsWith("\""))
                    valueNode = new LuaString(StringUtil.UnquoteC(value));
                else
                    valueNode = new LuaNumber(value);

                result.Add(new LuaAttribute(name, valueNode));
            }
            return result;
        }
    }
}
