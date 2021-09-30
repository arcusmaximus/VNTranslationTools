using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Yuris
{
    public class YurisCommandList
    {
        private readonly Dictionary<string, byte> _commandIds = new Dictionary<string, byte>();
        private readonly List<Dictionary<string, byte>> _attributeIds = new List<Dictionary<string, byte>>();

        public YurisCommandList(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            int magic = reader.ReadInt32();
            if (magic != 0x4D435359)
                throw new InvalidDataException("Invalid magic in ysc.ybn");

            int version = reader.ReadInt32();
            byte numCommands = checked((byte)reader.ReadInt32());
            reader.ReadInt32();
            for (byte commandId = 0; commandId < numCommands; commandId++)
            {
                ReadCommand(commandId, reader);
            }
        }

        public byte GetCommandId(string name)
        {
            return _commandIds[name];
        }

        public string GetCommandName(byte commandId)
        {
            return _commandIds.FirstOrDefault(p => p.Value == commandId).Key;
        }

        public byte GetAttributeId(string commandName, string attributeName)
        {
            byte commandId = GetCommandId(commandName);
            return _attributeIds[commandId][attributeName];
        }

        private void ReadCommand(byte commandId, BinaryReader reader)
        {
            string commandName = reader.ReadZeroTerminatedSjisString();
            _commandIds.Add(commandName, commandId);

            byte numAttributes = reader.ReadByte();
            Dictionary<string, byte> attributeIds = new Dictionary<string, byte>();
            for (byte attrId = 0; attrId < numAttributes; attrId++)
            {
                string attrName = reader.ReadZeroTerminatedSjisString();
                attributeIds.TryAdd(attrName, attrId);

                reader.Skip(2);
            }
            _attributeIds.Add(attributeIds);
        }
    }
}
