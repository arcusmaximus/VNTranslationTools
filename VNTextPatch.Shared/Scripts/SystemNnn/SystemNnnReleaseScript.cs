using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.SystemNnn
{
    internal class SystemNnnReleaseScript : SystemNnnBaseScript, IScript
    {
        public string Extension => ".spt";

        private byte[] _data;
        private int _messageTableIdx;
        private int _messageCount;
        private int _stringTableIdx;
        private int _stringCount;

        public void Load(ScriptLocation location)
        {
            _data = File.ReadAllBytes(location.ToFilePath());
            BinaryUtil.Xor(_data, 0, _data.Length, 0xFF);

            SptItem header = GetSptItems().First();
            if (header.Identify != SptIdentify.Data ||
                header.Code != SptCode.DataHeader)
            {
                throw new InvalidDataException("Invalid .spt header");
            }

            _messageTableIdx = ReadSptInt((int)SptHeaderField.MessageTable);
            _messageCount = ReadSptInt((int)SptHeaderField.MessageCount);

            _stringTableIdx = ReadSptInt((int)SptHeaderField.StringTable);
            _stringCount = ReadSptInt((int)SptHeaderField.StringCount);
        }

        public IEnumerable<ScriptString> GetStrings()
        {
            MemoryStream stream = new MemoryStream(_data);
            BinaryReader reader = new BinaryReader(stream);

            IEnumerable<ScriptString> printStrings = GetSptItems().Where(IsPrintItem)
                                                                  .SelectMany(i => GetPrintStrings(i, reader));
            
            IEnumerable<ScriptString> selectStrings = GetSptItems().Where(IsSelectItem)
                                                                   .SelectMany(i => GetSelectStrings(i, reader));

            foreach (ScriptString str in printStrings.Concat(selectStrings))
            {
                yield return str;
            }
        }

        private IEnumerable<ScriptString> GetPrintStrings(SptItem item, BinaryReader reader)
        {
            reader.BaseStream.Position = GetPrintItemMessageOffset(item);
            string text = reader.ReadZeroTerminatedSjisString();
            return ParseFileText(text);
        }

        private IEnumerable<ScriptString> GetSelectStrings(SptItem item, BinaryReader reader)
        {
            foreach (int stringOffset in GetSelectItemStringOffsets(item))
            {
                reader.BaseStream.Position = stringOffset;
                string text = reader.ReadZeroTerminatedSjisString();
                yield return new ScriptString(text, ScriptStringType.Message);
            }
        }

        public void WritePatched(IEnumerable<ScriptString> strings, ScriptLocation location)
        {
            MemoryStream inputStream = new MemoryStream(_data);
            MemoryStream outputStream = new MemoryStream();
            BinaryPatcher patcher = new BinaryPatcher(inputStream, outputStream, a => a * 4, o => o / 4);

            using IEnumerator<ScriptString> stringEnumerator = strings.GetEnumerator();

            PatchMessages(stringEnumerator, patcher);
            PatchStrings(stringEnumerator, patcher);

            patcher.CopyUpTo(_data.Length);
            PatchAddresses(patcher);

            outputStream.TryGetBuffer(out ArraySegment<byte> outSegment);
            BinaryUtil.Xor(outSegment.Array, outSegment.Offset, outSegment.Count, 0xFF);
            using Stream outputFileStream = File.Open(location.ToFilePath(), FileMode.Create, FileAccess.Write);
            outputFileStream.Write(outSegment.Array, outSegment.Offset, outSegment.Count);
        }

        private void PatchMessages(IEnumerator<ScriptString> stringEnumerator, BinaryPatcher patcher)
        {
            foreach (SptItem item in GetSptItems().Where(IsPrintItem))
            {
                int origMessageOffset = GetPrintItemMessageOffset(item);
                int origMessageLength = GetPaddedTextLength(origMessageOffset);

                NnnMessageType type = item.Code switch
                                      {
                                          SptCode.SystemCommandPrint => NnnMessageType.Print,
                                          SptCode.SystemCommandLPrint => NnnMessageType.LPrint,
                                          SptCode.SystemCommandAppend => NnnMessageType.Append
                                      };

                byte[] textBytes = FormatFileText(stringEnumerator, type);

                patcher.CopyUpTo(origMessageOffset);
                patcher.ReplaceBytes(origMessageLength, textBytes);
            }
        }

        private void PatchStrings(IEnumerator<ScriptString> stringEnumerator, BinaryPatcher patcher)
        {
            foreach (int origStringOffset in GetSptItems().Where(IsSelectItem)
                                                          .SelectMany(GetSelectItemStringOffsets))
            {
                int origStringLength = GetPaddedTextLength(origStringOffset);

                byte[] textBytes = FormatFileText(stringEnumerator, NnnMessageType.Print);

                patcher.CopyUpTo(origStringOffset);
                patcher.ReplaceBytes(origStringLength, textBytes);
            }
        }

        private void PatchAddresses(BinaryPatcher patcher)
        {
            patcher.PatchAddress(4 * (int)SptHeaderField.MessageTable);
            patcher.PatchAddress(4 * (int)SptHeaderField.StringTable);

            SptItem header = GetSptItems().First();
            if (header.Length > (int)SptHeaderField.SubcallTable)
            {
                patcher.PatchAddress(4 * (int)SptHeaderField.SubcallTable);
                patcher.PatchAddress(4 * (int)SptHeaderField.SelectTable);
                patcher.PatchAddress(4 * (int)SptHeaderField.CommandCallTable);
                patcher.PatchAddress(4 * (int)SptHeaderField.ScriptCallTable);
            }

            for (int i = 0; i < _messageCount; i++)
            {
                patcher.PatchAddress(4 * (_messageTableIdx + i));
            }

            for (int i = 0; i < _stringCount; i++)
            {
                patcher.PatchAddress(4 * (_stringTableIdx + i));
            }
        }

        private int ReadSptInt(int index)
        {
            return BitConverter.ToInt32(_data, 4 * index);
        }

        private IEnumerable<SptItem> GetSptItems()
        {
            int index = 0;
            while (index < _data.Length / 4)
            {
                SptItem sptItem = new SptItem
                                  {
                                      Index = index,
                                      Length = ReadSptInt(index),
                                      Identify = (SptIdentify)ReadSptInt(index + 1)
                                  };
                if (sptItem.Identify == SptIdentify.SystemCommand ||
                    sptItem.Identify == SptIdentify.Data)
                {
                    sptItem.Code = (SptCode)ReadSptInt(index + 2);
                    if (sptItem.Code == SptCode.DataTable)
                        sptItem.TableType = (SptTableType)ReadSptInt(index + 3);
                }

                yield return sptItem;
                index += sptItem.Length;
            }
        }

        private static bool IsPrintItem(SptItem item)
        {
            if (item.Identify != SptIdentify.SystemCommand)
                return false;

            return item.Code == SptCode.SystemCommandPrint ||
                   item.Code == SptCode.SystemCommandLPrint ||
                   item.Code == SptCode.SystemCommandAppend;
        }

        private int GetPrintItemMessageOffset(SptItem item)
        {
            int messageId = ReadSptInt(item.Index + 3);
            return 4 * ReadSptInt(_messageTableIdx + messageId);
        }

        private static bool IsSelectItem(SptItem item)
        {
            return item.Identify == SptIdentify.SystemCommand &&
                   item.Code == SptCode.SystemCommandSelect;
        }

        private IEnumerable<int> GetSelectItemStringOffsets(SptItem item)
        {
            int numChoices = ReadSptInt(item.Index + 3);
            if (item.Length != numChoices + 5)
                numChoices += ReadSptInt(item.Index + item.Length - 1);

            for (int i = 0; i < numChoices; i++)
            {
                int stringId = ReadSptInt(item.Index + 4 + i);
                yield return 4 * ReadSptInt(_stringTableIdx + stringId);
            }
        }

        protected override byte[] FormatFileText(IEnumerator<ScriptString> stringEnumerator, NnnMessageType type)
        {
            byte[] textBytes = base.FormatFileText(stringEnumerator, type);
            byte[] paddedTextBytes = new byte[(textBytes.Length + 1 + 3) & ~3];
            Array.Copy(textBytes, paddedTextBytes, textBytes.Length);
            return paddedTextBytes;
        }

        private int GetPaddedTextLength(int offset)
        {
            int length = 0;
            while (_data[offset + length] != 0)
            {
                length++;
            }

            return (length + 1 + 3) & ~3;
        }

        private enum SptHeaderField
        {
            MessageCount = 4,
            MessageTable = 5,
            StringCount = 6,
            StringTable = 7,
            SubcallTable = 20,
            SelectTable = 21,
            CommandCallTable = 22,
            ScriptCallTable = 23
        }

        private struct SptItem
        {
            public int Index;
            public int Offset => Index * 4;
            public int Length;
            public SptIdentify Identify;
            public SptCode Code;
            public SptTableType TableType;

            public override string ToString()
            {
                string str = Identify.ToString();
                if (Identify == SptIdentify.SystemCommand ||
                    Identify == SptIdentify.Data)
                {
                    str += "." + Code;
                    if (Code == SptCode.DataTable)
                        str += "." + TableType;
                }
                return str;
            }
        }

        private enum SptIdentify
        {
            Data = 0x66660001,
            Control = 0x66660002,
            Command = 0x66660003,
            Function = 0x66660004,
            Calcu = 0x66660005,
            SystemCommand = 0x66660006,
            SystemFunction = 0x066660007
        }

        private enum SptCode
        {
            SystemCommandPrint = 0x22220001,
            SystemCommandLPrint = 0x22220002,
            SystemCommandAppend = 0x22220003,
            SystemCommandDraw = 0x22220004,
            SystemCommandOverlap = 0x22220005,
            SystemCommandSelect = 0x22220006,
            DataHeader = 0x55550001,
            DataTable = 0x55550002,
            DataLabel = 0x55550003,
            DataFilmLabel = 0x55550004
        }

        private enum SptTableType
        {
            String = 0x44440001,
            Message = 0x44440002,
            Label = 0x44440003,
            Int32 = 0x44440004,
            Sub = 0x44440005,
            Select = 0x44440006,
            Command = 0x44440007,
            Script = 0x44440008
        }
    }
}
