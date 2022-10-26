using System;
using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Util
{
    internal class BinaryPatcher
    {
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        private readonly byte[] _buffer = new byte[1024];
        private readonly List<RangeMapping> _rangeMappings = new List<RangeMapping>();

        private readonly Func<int, int> _addressToOffset;
        private readonly Func<int, int> _offsetToAddress;

        public BinaryPatcher(Stream inputStream, Stream outputStream)
            : this(inputStream, outputStream, addr => addr, offset => offset)
        {
        }

        public BinaryPatcher(Stream inputStream, Stream outputStream, Func<int, int> addressToOffset, Func<int, int> offsetToAddress)
        {
            InputStream = inputStream;
            OutputStream = outputStream;

            _reader = new BinaryReader(inputStream);
            _writer = new BinaryWriter(outputStream);

            _addressToOffset = addressToOffset;
            _offsetToAddress = offsetToAddress;
        }

        public Stream InputStream
        {
            get;
        }

        public Stream OutputStream
        {
            get;
        }

        public void CopyUpTo(int originalOffset)
        {
            if (originalOffset < InputStream.Position || originalOffset > InputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            int remainingLength = (int)(originalOffset - InputStream.Position);
            while (remainingLength > 0)
            {
                int amountRead = InputStream.Read(_buffer, 0, Math.Min(remainingLength, _buffer.Length));
                OutputStream.Write(_buffer, 0, amountRead);
                remainingLength -= amountRead;
            }
        }

        public void ReplaceBytes(int originalLength, byte[] newData)
        {
            ReplaceBytes(originalLength, newData, 0, newData.Length);
        }

        public void ReplaceBytes(int originalLength, ArraySegment<byte> newData)
        {
            ReplaceBytes(originalLength, newData.Array, newData.Offset, newData.Count);
        }

        public void ReplaceBytes(int originalLength, byte[] newData, int newDataOffset, int newDataLength)
        {
            ReplaceBytes(originalLength, _ => OutputStream.Write(newData, newDataOffset, newDataLength));
        }

        public void ReplaceBytes(int originalLength, Action<BinaryWriter> writeNewData)
        {
            if (originalLength <= 0 || InputStream.Position + originalLength > InputStream.Length)
                throw new ArgumentException(nameof(originalLength));

            int newDataOffset = (int)OutputStream.Position;
            writeNewData(_writer);
            int newDataLength = (int)OutputStream.Position - newDataOffset;

            if (newDataLength != originalLength)
            {
                _rangeMappings.Add(
                    new RangeMapping(
                        (int)InputStream.Position,
                        (int)InputStream.Position + originalLength,
                        newDataOffset,
                        newDataOffset + newDataLength
                    )
                );
            }

            InputStream.Seek(originalLength, SeekOrigin.Current);
        }

        public void ReplaceZeroTerminatedSjisString(string newString)
        {
            int originalOffset = (int)InputStream.Position;
            int originalLength = _reader.SkipZeroTerminatedSjisString();

            int newOffset = (int)OutputStream.Position;
            int newLength = _writer.WriteZeroTerminatedSjisString(newString);

            if (newLength != originalLength)
                _rangeMappings.Add(new RangeMapping(originalOffset, originalOffset + originalLength, newOffset, newOffset + newLength));
        }

        public void ReplaceZeroTerminatedUtf8String(string newString)
        {
            int originalOffset = (int)InputStream.Position;
            int originalLength = _reader.SkipZeroTerminatedUtf8String();

            int newOffset = (int)OutputStream.Position;
            int newLength = _writer.WriteZeroTerminatedUtf8String(newString);

            if (newLength != originalLength)
                _rangeMappings.Add(new RangeMapping(originalOffset, originalOffset + originalLength, newOffset, newOffset + newLength));
        }

        public void ReplaceZeroTerminatedUtf16String(string newString)
        {
            int originalOffset = (int)InputStream.Position;
            int originalLength = _reader.SkipZeroTerminatedUtf16String();

            int newOffset = (int)OutputStream.Position;
            int newLength = _writer.WriteZeroTerminatedUtf16String(newString);

            if (newLength != originalLength)
                _rangeMappings.Add(new RangeMapping(originalOffset, originalOffset + originalLength, newOffset, newOffset + newLength));
        }

        public int MapOffset(int originalOffset)
        {
            if (originalOffset < 0 || originalOffset > InputStream.Length)
                throw new ArgumentOutOfRangeException();

            if (originalOffset > InputStream.Position)
                throw new InvalidOperationException();

            int start = 0;
            int end = _rangeMappings.Count;
            while (start < end)
            {
                int pivot = (start + end) / 2;
                RangeMapping mapping = _rangeMappings[pivot];
                if (originalOffset < mapping.Original.StartOffset)
                    end = pivot;
                else if (originalOffset == mapping.Original.StartOffset)
                    return mapping.New.StartOffset;
                else if (originalOffset >= mapping.Original.EndOffset)
                    start = pivot + 1;
                else
                    throw new ArgumentException("Can't map an offset inside a changed section");
            }
            int index = start - 1;
            if (index < 0)
                return originalOffset;

            RangeMapping precedingSection = _rangeMappings[index];
            int newOffset = originalOffset - precedingSection.Original.EndOffset + precedingSection.New.EndOffset;
            if (newOffset > OutputStream.Length)
                throw new InvalidOperationException();

            return newOffset;
        }

        public void PatchByte(int originalOffset, byte value)
        {
            if (originalOffset < 0 || originalOffset + 1 > InputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (InputStream.Position < originalOffset + 1)
                throw new InvalidOperationException();

            OutputStream.Position = MapOffset(originalOffset);
            _writer.Write(value);
            OutputStream.Position = OutputStream.Length;
        }

        public void PatchInt16(int originalOffset, short value)
        {
            if (originalOffset < 0 || originalOffset + 2 > InputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (InputStream.Position < originalOffset + 2)
                throw new InvalidOperationException();

            OutputStream.Position = MapOffset(originalOffset);
            _writer.Write(value);
            OutputStream.Position = OutputStream.Length;
        }

        public void PatchInt32(int originalOffset, int value)
        {
            if (originalOffset < 0 || originalOffset + 4 > InputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (InputStream.Position < originalOffset + 4)
                throw new InvalidOperationException();

            OutputStream.Position = MapOffset(originalOffset);
            _writer.Write(value);
            OutputStream.Position = OutputStream.Length;
        }

        public void PatchAddress(int originalOffset)
        {
            if (originalOffset < 0 || originalOffset + 4 > InputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (InputStream.Position < originalOffset + 4)
                throw new InvalidOperationException();

            int inputPos = (int)InputStream.Position;
            InputStream.Position = originalOffset;
            int originalAddr = _reader.ReadInt32();
            InputStream.Position = inputPos;

            int newOffset = MapOffset(originalOffset);
            int newAddr = _offsetToAddress(MapOffset(_addressToOffset(originalAddr)));

            OutputStream.Position = newOffset;
            _writer.Write(newAddr);
            OutputStream.Position = OutputStream.Length;
        }

        private struct RangeMapping
        {
            public RangeMapping(int originalStartOffset, int originalEndOffset, int newStartOffset, int newEndOffset)
            {
                Original = new Range(originalStartOffset, originalEndOffset);
                New = new Range(newStartOffset, newEndOffset);
            }

            public Range Original { get; }
            public Range New { get; }
        }

        private struct Range
        {
            public Range(int startOffset, int endOffset)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
            }

            public int StartOffset { get; }
            public int EndOffset { get; }
        }
    }
}
