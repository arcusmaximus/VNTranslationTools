using System;
using System.IO;
using System.Text;

namespace VNTextPatch.Shared.Scripts.Mware
{
    internal static class SquirrelObject
    {
        public static object Read(BinaryReader reader, Encoding encoding)
        {
            ObjectType type = (ObjectType)reader.ReadInt32();
            switch (type)
            {
                case ObjectType.Null:
                    return null;

                case ObjectType.Integer:
                    return reader.ReadInt32();

                case ObjectType.Float:
                    return reader.ReadSingle();

                case ObjectType.String:
                    int length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(length);
                    return encoding.GetString(data);

                default:
                    throw new InvalidDataException();
            }
        }

        public static void Write(BinaryWriter writer, object value, Encoding encoding)
        {
            switch (value)
            {
                case null:
                    writer.Write((int)ObjectType.Null);
                    break;

                case int intValue:
                    writer.Write((int)ObjectType.Integer);
                    writer.Write(intValue);
                    break;

                case float floatValue:
                    writer.Write((int)ObjectType.Float);
                    writer.Write(floatValue);
                    break;

                case string stringValue:
                    byte[] bytes = encoding.GetBytes(stringValue);
                    writer.Write((int)ObjectType.String);
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                    break;

                default:
                    throw new ArgumentException();
            }
        }

        private enum ObjectType
        {
            Null = 0x01000001,
            Integer = 0x05000002,
            Float = 0x05000004,
            String = 0x08000010
        }
    }
}
