using System;
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
            foreach ((ILuaNode node, ScriptStringType type) in GetStringNodes())
            {
                string text = node switch
                              {
                                  LuaString str => str.Value,
                                  LuaTable table => SerializeMessage(table),
                                  _ => throw new InvalidDataException()
                              };
                yield return new ScriptString(text, type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            foreach ((ILuaNode node, ScriptStringType type) in GetStringNodes())
            {
                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Too few strings in translation");

                if (stringEnumerator.Current.Type != type)
                    throw new InvalidDataException("Translation type mismatch");

                string text = stringEnumerator.Current.Text;

                switch (node)
                {
                    case LuaString str:
                        str.Value = stringEnumerator.Current.Text;
                        break;

                    case LuaTable table:
                        text = ProportionalWordWrapper.Default.Wrap(text, CommandRegex);
                        DeserializeMessage(text, table);
                        break;
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

        private IEnumerable<(ILuaNode, ScriptStringType)> GetStringNodes()
        {
            LuaNumber version = _rootNodes.OfType<LuaAttribute>()
                                          .FirstOrDefault(a => a.Name == "astver")
                                          ?.Value as LuaNumber;
            return version?.Value switch
                   {
                       null => GetStringNodesV1(),
                       "2.0" => GetStringNodesV2(),
                       _ => throw new NotSupportedException($".ast with version {version} is not supported")
                   };
        }

        private IEnumerable<(ILuaNode, ScriptStringType)> GetStringNodesV1()
        {
            LuaTable texts = _ast["text"] as LuaTable;
            if (texts == null)
                yield break;

            foreach (LuaTable text in texts.OfType<LuaAttribute>()
                                           .Select(a => a.Value)
                                           .OfType<LuaTable>())
            {
                LuaString name = (text["name"] as LuaTable)?["name"] as LuaString;
                if (name != null)
                    yield return (name, ScriptStringType.CharacterName);

                LuaTable select = text["select"] as LuaTable;
                if (select != null)
                {
                    foreach (LuaString choice in select.OfType<LuaString>())
                    {
                        yield return (choice, ScriptStringType.Message);
                    }
                }

                LuaTable message = text.OfType<LuaTable>().FirstOrDefault();
                if (message != null)
                    yield return (message, ScriptStringType.Message);
            }
        }

        private IEnumerable<(ILuaNode, ScriptStringType)> GetStringNodesV2()
        {
            foreach (LuaTable block in _ast.OfType<LuaAttribute>()
                                           .Select(a => a.Value)
                                           .OfType<LuaTable>())
            {
                LuaTable select = (block["select"] as LuaTable)?["ja"] as LuaTable;
                if (select != null)
                {
                    foreach (LuaString choice in select.OfType<LuaString>())
                    {
                        yield return (choice, ScriptStringType.Message);
                    }
                }

                LuaTable text = (block["text"] as LuaTable)?["ja"] as LuaTable;
                if (text != null && text.Count == 1)
                {
                    LuaTable message = text[0] as LuaTable;
                    if (message == null)
                        continue;

                    LuaTable names = message["name"] as LuaTable;
                    if (names != null)
                    {
                        foreach (LuaString name in names.OfType<LuaString>())
                        {
                            yield return (name, ScriptStringType.CharacterName);
                        }
                    }

                    yield return (message, ScriptStringType.Message);
                }
            }
        }

        private static string SerializeMessage(LuaTable message)
        {
            StringBuilder text = new StringBuilder();
            foreach (ILuaNode item in message)
            {
                switch (item)
                {
                    case LuaString str:
                        text.Append(str.Value);
                        break;

                    case LuaTable table:
                        if (table.Count == 1 && table[0] is LuaString cmdName && cmdName.Value == "rt2")
                            text.AppendLine();
                        else
                            SerializeCommand(table, text);

                        break;
                }
            }

            return text.ToString().Trim();
        }

        private static void DeserializeMessage(string text, LuaTable table)
        {
            table.RemoveAll(n => !(n is LuaAttribute));
            foreach ((string segment, Match match) in StringUtil.GetMatchingAndSurroundingTexts(text, new Regex(@"\r\n|\[.+?\]")))
            {
                if (segment != null)
                    table.Add(new LuaString(segment));
                else if (match.Value == "\r\n")
                    table.Add(new LuaTable { new LuaString("rt2") });
                else
                    table.Add(DeserializeCommand(match.Value));
            }
        }

        private static void SerializeCommand(LuaTable table, StringBuilder cmd)
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

        private static LuaTable DeserializeCommand(string cmd)
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
