using System.IO;

namespace VNTextPatch.Shared.Scripts.Yuris
{
    public struct YurisAttribute
    {
        public void Read(BinaryReader reader, int valuesOffset)
        {
            DescriptorOffset = (int)reader.BaseStream.Position;
            Id = reader.ReadInt16();
            Type = (YurisAttributeType)reader.ReadInt16();
            ValueLength = reader.ReadInt32();
            ValueOffset = valuesOffset + reader.ReadInt32();
        }

        public int DescriptorOffset;

        public short Id;
        public YurisAttributeType Type;
        public int ValueLength;
        public int ValueOffset;
    }

    public enum YurisAttributeType : short
    {
        Raw = 0,
        Long = 1,
        Double = 2,
        Expression = 3
    }
}
