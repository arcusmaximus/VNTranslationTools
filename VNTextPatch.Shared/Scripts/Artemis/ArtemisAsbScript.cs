using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Artemis
{
    public class ArtemisAsbScript : IScript
    {
        private List<object> _items;
        private List<TextReference> _textRefs;

        public string Extension => ".asb";

        public void Load(ScriptLocation location)
        {
            byte[] data = File.ReadAllBytes(location.ToFilePath());
            _items = ReadFile(new MemoryStream(data));
            _textRefs = GetTextReferences(_items);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (TextReference textRef in _textRefs)
            {
                string text = textRef switch
                              {
                                  PrintCommandRange printRange => PrintCommandsToText(printRange),
                                  AttributeReference attrRef => attrRef.Command.Attributes[attrRef.AttributeName]
                              };
                yield return new ScriptString(text, textRef.Type);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            PatchItems(strings);

            using Stream stream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            WriteFile(stream);
        }

        private void PatchItems(IEnumerable<ScriptString> strings)
        {
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            int itemOffset = 0;
            foreach (TextReference textRef in _textRefs)
            {
                if (!stringEnumerator.MoveNext())
                    throw new InvalidDataException("Too few lines in translation");

                if (stringEnumerator.Current.Type != textRef.Type)
                    throw new InvalidDataException("String type mismatch between game file and translation");

                string text = ProportionalWordWrapper.Default.Wrap(stringEnumerator.Current.Text);

                switch (textRef)
                {
                    case PrintCommandRange printRange:
                        List<Command> newCommands = TextToPrintCommands(text);
                        _items.RemoveRange(printRange.Index + itemOffset, printRange.Count);
                        _items.InsertRange(printRange.Index + itemOffset, newCommands);
                        itemOffset += newCommands.Count - printRange.Count;
                        break;

                    case AttributeReference attrRef:
                        attrRef.Command.Attributes[attrRef.AttributeName] = text;
                        break;
                }
            }

            if (stringEnumerator.MoveNext())
                throw new InvalidDataException("Too many lines in translation");
        }

        private static List<TextReference> GetTextReferences(List<object> items)
        {
            List<TextReference> refs = new List<TextReference>();

            int index;
            int printStartIdx = -1;
            for (index = 0; index < items.Count; index++)
            {
                object item = items[index];
                if (!(item is Command command))
                {
                    EndPrintRange();
                    continue;
                }

                if (command.Name == "print" || command.Name == "ruby" || command.Name == "/ruby")
                {
                    if (printStartIdx < 0)
                        printStartIdx = index;
                }
                else
                {
                    EndPrintRange();
                    refs.AddRange(GetTextReferences(command));
                }
            }

            EndPrintRange();
            return refs;

            void EndPrintRange()
            {
                if (printStartIdx < 0)
                    return;

                refs.Add(new PrintCommandRange(printStartIdx, index - printStartIdx));
                printStartIdx = -1;
            }
        }

        private static IEnumerable<AttributeReference> GetTextReferences(Command command)
        {
            switch (command.Name)
            {
                case "name":
                    yield return new AttributeReference(command, "0", ScriptStringType.CharacterName);
                    break;

                case "sel_text":
                    yield return new AttributeReference(command, "text", ScriptStringType.Message);
                    break;

                case "RegisterTextToHistory":
                    yield return new AttributeReference(command, "1", ScriptStringType.Message);
                    break;
            }
        }

        private string PrintCommandsToText(PrintCommandRange range)
        {
            StringBuilder text = new StringBuilder();

            int index = range.Index;
            int endIndex = range.Index + range.Count;
            while (index < endIndex)
            {
                Command command = (Command)_items[index++];
                switch (command.Name)
                {
                    case "print":
                    {
                        text.Append(command.Attributes["data"]);
                        break;
                    }

                    case "ruby":
                    {
                        if (index >= endIndex)
                            throw new InvalidDataException();

                        Command printCommand = (Command)_items[index++];
                        if (printCommand.Name != "print")
                            throw new InvalidDataException();

                        if (index >= endIndex)
                            throw new InvalidDataException();

                        Command endRubyCommand = (Command)_items[index++];
                        if (endRubyCommand.Name != "/ruby")
                            throw new InvalidDataException();

                        string baseText = printCommand.Attributes["data"];
                        string rubyText = command.Attributes["text"];
                        text.Append($"[{baseText}/{rubyText}]");
                        break;
                    }

                    default:
                        throw new InvalidDataException();
                }
            }

            return text.ToString();
        }

        private static List<Command> TextToPrintCommands(string text)
        {
            List<Command> commands = new List<Command>();

            int printStartIdx = 0;
            foreach (Match match in Regex.Matches(text, @"\[([^\[\]/]+)/([^\[\]]+)\]"))
            {
                if (printStartIdx < match.Index)
                    commands.Add(new Command("print") { { "data", text.Substring(printStartIdx, match.Index - printStartIdx) } });

                commands.Add(new Command("ruby") { { "text", match.Groups[2].Value } });
                commands.Add(new Command("print") { { "data", match.Groups[1].Value } });
                commands.Add(new Command("/ruby"));

                printStartIdx = match.Index + match.Length;
            }

            if (printStartIdx < text.Length)
                commands.Add(new Command("print") { { "data", text.Substring(printStartIdx) } });

            return commands;
        }

        private static List<object> ReadFile(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            if (stream.Length < 9 ||
                reader.ReadByte() != 'A' ||
                reader.ReadByte() != 'S' ||
                reader.ReadByte() != 'B' ||
                reader.ReadByte() != 0 ||
                reader.ReadByte() != 0)
            {
                throw new InvalidDataException("Invalid ASB header");
            }

            int numItems = reader.ReadInt32();
            List<object> items = new List<object>();
            for (int i = 0; i < numItems; i++)
            {
                items.Add(ReadItem(reader));
            }

            return items;
        }

        private static object ReadItem(BinaryReader reader)
        {
            int type = reader.ReadInt32();
            switch (type)
            {
                case 0:
                    string name = ReadString(reader);
                    int lineNumber = reader.ReadInt32();
                    Command command = new Command(name);

                    int numAttrs = reader.ReadInt32();
                    for (int i = 0; i < numAttrs; i++)
                    {
                        string attrName = ReadString(reader);
                        string attrValue = ReadString(reader);
                        command.Attributes[attrName] = attrValue;
                    }

                    return command;

                case 1:
                    string label = ReadString(reader);
                    return new Label(label);

                default:
                    throw new InvalidDataException();
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            long nameStartPos = reader.BaseStream.Position;
            string text = reader.ReadZeroTerminatedUtf8String();
            if (reader.BaseStream.Position - nameStartPos != length + 1)
                throw new InvalidDataException();

            return text;
        }

        private void WriteFile(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)'A');
            writer.Write((byte)'S');
            writer.Write((byte)'B');
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(_items.Count);
            foreach (object item in _items)
            {
                WriteItem(writer, item);
            }
        }

        private static void WriteItem(BinaryWriter writer, object item)
        {
            switch (item)
            {
                case Command command:
                    writer.Write(0);
                    WriteString(writer, command.Name);
                    writer.Write(0);
                    writer.Write(command.Attributes.Count);
                    foreach ((string attrName, string attrValue) in command.Attributes)
                    {
                        WriteString(writer, attrName);
                        WriteString(writer, attrValue);
                    }
                    break;

                case Label label:
                    writer.Write(1);
                    WriteString(writer, label.Name);
                    break;
            }
        }

        private static void WriteString(BinaryWriter writer, string text)
        {
            Stream stream = writer.BaseStream;

            int lengthPos = (int)stream.Position;
            writer.Write(0);

            int textPos = (int)stream.Position;
            writer.WriteZeroTerminatedUtf8String(text);
            int textLength = (int)stream.Position - 1 - textPos;

            stream.Position = lengthPos;
            writer.Write(textLength);

            stream.Position = stream.Length;
        }

        private class Label
        {
            public Label(string name)
            {
                Name = name;
            }

            public string Name
            {
                get;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private class Command : IEnumerable
        {
            public Command(string name)
            {
                Name = name;
                Attributes = new Dictionary<string, string>();
            }

            public string Name
            {
                get;
            }

            public Dictionary<string, string> Attributes
            {
                get;
            }

            public void Add(string attrName, string attrValue)
            {
                Attributes.Add(attrName, attrValue);
            }

            public override string ToString()
            {
                return Name + "(" + string.Join(", ", Attributes.Select(a => $"{a.Key}={a.Value}")) + ")";
            }

            public IEnumerator GetEnumerator() => Attributes.GetEnumerator();
        }

        private abstract class TextReference
        {
            public abstract ScriptStringType Type
            {
                get;
            }
        }

        private class PrintCommandRange : TextReference
        {
            public PrintCommandRange(int index, int count)
            {
                Index = index;
                Count = count;
            }

            public int Index
            {
                get;
            }

            public int Count
            {
                get;
            }

            public override ScriptStringType Type => ScriptStringType.Message;
        }

        private class AttributeReference : TextReference
        {
            public AttributeReference(Command command, string attrName, ScriptStringType type)
            {
                Command = command;
                AttributeName = attrName;
                Type = type;
            }

            public Command Command
            {
                get;
            }

            public string AttributeName
            {
                get;
            }

            public override ScriptStringType Type
            {
                get;
            }
        }
    }
}
