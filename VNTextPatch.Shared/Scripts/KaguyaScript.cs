using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts
{
    public class KaguyaScript : IScript
    {
        private const string Magic = "[SCR-MESSAGE]ver4.0";

        public string Extension => ".dat";

        private byte _encryptionKey;
        private List<string> _choices;
        private List<MessageGroup> _messageGroups;

        public void Load(ScriptLocation location)
        {
            byte[] data = File.ReadAllBytes(location.ToFilePath());
            Read(data);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            foreach (MessageGroup group in _messageGroups)
            {
                foreach (Message message in group.Messages)
                {
                    if (group.Name != null)
                        yield return new ScriptString(group.Name, ScriptStringType.CharacterName);

                    yield return new ScriptString(message.Text, ScriptStringType.Message);
                }
            }

            foreach (string choice in _choices)
            {
                yield return new ScriptString(choice, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            foreach (MessageGroup group in _messageGroups)
            {
                foreach (Message message in group.Messages)
                {
                    if (group.Name != null)
                        group.Name = GetNextString(stringEnumerator, ScriptStringType.CharacterName);

                    message.Text = GetNextString(stringEnumerator, ScriptStringType.Message);
                }
            }

            for (int i = 0; i < _choices.Count; i++)
            {
                _choices[i] = GetNextString(stringEnumerator, ScriptStringType.Message);
            }

            if (stringEnumerator.MoveNext())
                throw new Exception("Too many strings in translation");

            using Stream stream = File.Open(location.ToFilePath(), FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);
            Write(writer);
        }

        private static string GetNextString(IEnumerator<ScriptString> stringEnumerator, ScriptStringType type)
        {
            if (!stringEnumerator.MoveNext())
                throw new Exception("Too few strings in translation");

            if (stringEnumerator.Current.Type != type)
                throw new Exception("Mismatching string type");

            return MonospaceWordWrapper.Default.Wrap(stringEnumerator.Current.Text);
        }

        private void Read(byte[] data)
        {
            if (data.Length < Magic.Length || Encoding.ASCII.GetString(data, 0, Magic.Length) != Magic)
                throw new InvalidDataException();

            _encryptionKey = data[0x13] != 0 ? data[0x14] : (byte)0;

            MemoryStream stream = new MemoryStream(data) { Position = 0x15 };
            BinaryReader reader = new BinaryReader(stream);

            List<string> names = new List<string>();
            int numNames = reader.ReadInt32();
            for (int i = 0; i < numNames; i++)
            {
                names.Add(ReadString(reader, data));
            }

            int numChoices = reader.ReadInt32();
            _choices = new List<string>(numChoices);
            for (int i = 0; i < numChoices; i++)
            {
                _choices.Add(ReadString(reader, data));
            }

            int numMessages = reader.ReadInt32();
            List<Message> messages = new List<Message>(numMessages);
            for (int i = 0; i < numMessages; i++)
            {
                messages.Add(ReadMessage(reader, data));
            }

            int numMessageGroups = reader.ReadInt32();
            _messageGroups = new List<MessageGroup>(numMessageGroups);
            for (int i = 0; i < numMessageGroups; i++)
            {
                _messageGroups.Add(ReadMessageGroup(reader, names, messages));
            }
        }

        private string ReadString(BinaryReader reader, byte[] data)
        {
            int length = reader.ReadInt16();
            for (int i = 0; i < length; i++)
            {
                data[reader.BaseStream.Position + i] ^= _encryptionKey;
            }

            BinaryUtil.ReplaceSjisCodepoint(data, (int)reader.BaseStream.Position, length, 0xF040, 0x8193);
            string result = StringUtil.SjisEncoding.GetString(data, (int)reader.BaseStream.Position, length);
            reader.Skip(length);
            return result;
        }

        private Message ReadMessage(BinaryReader reader, byte[] data)
        {
            int msgLength = reader.ReadInt32();
            for (int i = 0; i < msgLength; i++)
            {
                data[reader.BaseStream.Position + i] ^= _encryptionKey;
            }

            int textLength = reader.ReadInt32();
            BinaryUtil.ReplaceSjisCodepoint(data, (int)reader.BaseStream.Position, textLength, 0xF040, 0x8193);
            string text = StringUtil.SjisEncoding.GetString(data, (int)reader.BaseStream.Position, textLength);
            reader.Skip(textLength);

            Message message = new Message { Text = text };
            int numVoices = reader.ReadByte();
            for (int i = 0; i < numVoices; i++)
            {
                message.Voices.Add(reader.ReadZeroTerminatedUtf16String());
            }

            return message;
        }

        private MessageGroup ReadMessageGroup(BinaryReader reader, List<string> names, List<Message> messages)
        {
            int nameIdx = reader.ReadInt32();
            string name = nameIdx >= 0 ? names[nameIdx] : null;

            MessageGroup group = new MessageGroup { Name = name };

            int numMessages = reader.ReadByte();
            for (int i = 0; i < numMessages; i++)
            {
                int messageIdx = reader.ReadInt32();
                group.Messages.Add(messages[messageIdx]);
            }

            return group;
        }

        private void Write(BinaryWriter writer)
        {
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write((byte)0);
            writer.Write((byte)0);

            Dictionary<string, int> nameIndexes = new Dictionary<string, int>();
            List<int> messageGroupNameIndexes = new List<int>();
            foreach (MessageGroup group in _messageGroups)
            {
                int nameIndex;
                if (group.Name == null)
                {
                    nameIndex = -1;
                }
                else if (!nameIndexes.TryGetValue(group.Name, out nameIndex))
                {
                    nameIndex = nameIndexes.Count;
                    nameIndexes.Add(group.Name, nameIndex);
                }
                messageGroupNameIndexes.Add(nameIndex);
            }

            writer.Write(nameIndexes.Count);
            foreach (string name in nameIndexes.Keys)
            {
                WriteString(writer, name);
            }

            writer.Write(_choices.Count);
            foreach (string choice in _choices)
            {
                WriteString(writer, choice);
            }

            writer.Write(_messageGroups.SelectMany(g => g.Messages).Count());
            foreach (Message message in _messageGroups.SelectMany(g => g.Messages))
            {
                WriteMessage(writer, message);
            }

            writer.Write(_messageGroups.Count);
            int messageIdx = 0;
            for (int i = 0; i < _messageGroups.Count; i++)
            {
                writer.Write(messageGroupNameIndexes[i]);
                writer.Write((byte)_messageGroups[i].Messages.Count);
                for (int j = 0; j < _messageGroups[i].Messages.Count; j++)
                {
                    writer.Write(messageIdx++);
                }
            }
        }

        private void WriteString(BinaryWriter writer, string text)
        {
            byte[] bytes = StringUtil.SjisTunnelEncoding.GetBytes(text);
            BinaryUtil.ReplaceSjisCodepoint(bytes, 0, bytes.Length, 0x8193, 0xF040);

            writer.Write((short)bytes.Length);
            writer.Write(bytes);
        }

        private void WriteMessage(BinaryWriter writer, Message message)
        {
            long msgLengthPos = writer.BaseStream.Position;
            writer.Write(0);

            byte[] textBytes = StringUtil.SjisTunnelEncoding.GetBytes(message.Text);
            BinaryUtil.ReplaceSjisCodepoint(textBytes, 0, textBytes.Length, 0x8193, 0xF040);
            writer.Write(textBytes.Length);
            writer.Write(textBytes);

            writer.Write((byte)message.Voices.Count);
            foreach (string voice in message.Voices)
            {
                writer.WriteZeroTerminatedUtf16String(voice);
            }

            int msgLength = (int)(writer.BaseStream.Position - (msgLengthPos + 4));
            writer.BaseStream.Position = msgLengthPos;
            writer.Write(msgLength);
            writer.BaseStream.Position = writer.BaseStream.Length;
        }

        private class MessageGroup
        {
            public string Name
            {
                get;
                set;
            }

            public List<Message> Messages
            {
                get;
            } = new List<Message>();
        }

        private class Message
        {
            public string Text
            {
                get;
                set;
            }

            public List<string> Voices
            {
                get;
            } = new List<string>();
        }
    }
}
