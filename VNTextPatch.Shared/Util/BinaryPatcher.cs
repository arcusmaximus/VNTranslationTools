using System;
using System.Collections.Generic;
using System.IO;

namespace VNTextPatch.Shared.Util
{
    internal class BinaryPatcher
    {
        private readonly Stream _inputStream;
        private readonly Stream _outputStream;

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
            _inputStream = inputStream;
            _outputStream = outputStream;

            _reader = new BinaryReader(inputStream);
            _writer = new BinaryWriter(outputStream);

            _addressToOffset = addressToOffset;
            _offsetToAddress = offsetToAddress;
        }

        public int CurrentInputPosition
        {
            get { return (int)_inputStream.Position; }
        }

        public int CurrentOutputPosition
        {
            get { return (int)_outputStream.Position; }
        }

        public void CopyUpTo(int originalOffset)
        {
            if (originalOffset < _inputStream.Position || originalOffset > _inputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            int remainingLength = (int)(originalOffset - _inputStream.Position);
            while (remainingLength > 0)
            {
                int amountRead = _inputStream.Read(_buffer, 0, Math.Min(remainingLength, _buffer.Length));
                _outputStream.Write(_buffer, 0, amountRead);
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
            if (originalLength <= 0 || _inputStream.Position + originalLength > _inputStream.Length)
                throw new ArgumentException(nameof(originalLength));

            if (newDataLength != originalLength)
            {
                _rangeMappings.Add(
                    new RangeMapping(
                        (int)_inputStream.Position,
                        (int)_inputStream.Position + originalLength,
                        (int)_outputStream.Position,
                        (int)_outputStream.Position + newDataLength
                    )
                );
            }

            _inputStream.Seek(originalLength, SeekOrigin.Current);
            _outputStream.Write(newData, newDataOffset, newDataLength);
        }

        public void ReplaceZeroTerminatedSjisString(string newString)
        {
            int originalOffset = (int)_inputStream.Position;
            int originalLength = _reader.SkipZeroTerminatedSjisString();

            int newOffset = (int)_outputStream.Position;
            int newLength = _writer.WriteZeroTerminatedSjisString(newString);

            if (newLength != originalLength)
                _rangeMappings.Add(new RangeMapping(originalOffset, originalOffset + originalLength, newOffset, newOffset + newLength));
        }

        public void ReplaceZeroTerminatedUtf16String(string newString)
        {
            int originalOffset = (int)_inputStream.Position;
            int originalLength = _reader.SkipZeroTerminatedUtf16String();

            int newOffset = (int)_outputStream.Position;
            int newLength = _writer.WriteZeroTerminatedUtf16String(newString);

            if (newLength != originalLength)
                _rangeMappings.Add(new RangeMapping(originalOffset, originalOffset + originalLength, newOffset, newOffset + newLength));
        }

        public int MapOffset(int originalOffset)
        {
            if (originalOffset < 0 || originalOffset > _inputStream.Length)
                throw new ArgumentOutOfRangeException();

            if (originalOffset > _inputStream.Position)
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
            if (newOffset > _outputStream.Length)
                throw new InvalidOperationException();

            return newOffset;
        }

        public void PatchInt32(int originalOffset, int value)
        {
            if (originalOffset < 0 || originalOffset + 4 > _inputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (_inputStream.Position < originalOffset + 4)
                throw new InvalidOperationException();

            _outputStream.Position = MapOffset(originalOffset);
            _writer.Write(value);
            _outputStream.Position = _outputStream.Length;
        }

        public void PatchAddress(int originalOffset)
        {
            if (originalOffset < 0 || originalOffset + 4 > _inputStream.Length)
                throw new ArgumentOutOfRangeException(nameof(originalOffset));

            if (_inputStream.Position < originalOffset + 4)
                throw new InvalidOperationException();

            int inputPos = (int)_inputStream.Position;
            _inputStream.Position = originalOffset;
            int originalAddr = _reader.ReadInt32();
            _inputStream.Position = inputPos;

            int newOffset = MapOffset(originalOffset);
            int newAddr = _offsetToAddress(MapOffset(_addressToOffset(originalAddr)));

            _outputStream.Position = newOffset;
            _writer.Write(newAddr);
            _outputStream.Position = _outputStream.Length;
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
