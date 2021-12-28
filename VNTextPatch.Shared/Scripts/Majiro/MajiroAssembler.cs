using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Majiro
{
    internal class MajiroAssembler
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;

        public MajiroAssembler()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public void Write(short opcode, params object[] operands)
        {
            _writer.Write(opcode);
            WriteOperands(opcode, operands);
        }

        private void WriteOperands(short opcode, object[] operands)
        {
            string template = MajiroOpcodes.OperandTemplates[opcode];
            for (int i = 0; i < template.Length; i++)
            {
                switch (template[i])
                {
                    case 't':
                        _writer.Write((ushort)operands.Length);
                        foreach (int type in operands)
                        {
                            _writer.Write((byte)type);
                        }
                        break;

                    case 's':
                        string str = (string)operands[i];
                        byte[] bytes = StringUtil.SjisTunnelEncoding.GetBytes(str);

                        _writer.Write((ushort)(bytes.Length + 1));
                        _writer.Write(bytes);
                        _writer.Write((byte)0);
                        break;

                    case 'f':
                        _writer.Write((ushort)(int)operands[i]);
                        break;

                    case 'h':
                        _writer.Write((int)operands[i]);
                        break;

                    case 'o':
                        _writer.Write((short)(int)operands[i]);
                        break;

                    case '0':
                        _writer.Write((int)operands[i]);
                        break;

                    case 'i':
                        _writer.Write((int)operands[i]);
                        break;

                    case 'r':
                        _writer.Write((float)operands[i]);
                        break;

                    case 'a':
                        _writer.Write((ushort)(int)operands[i]);
                        break;

                    case 'j':
                        _writer.Write((int)operands[i]);
                        break;

                    case 'l':
                        _writer.Write((int)operands[i]);
                        break;

                    case 'c':
                        _writer.Write((ushort)operands.Length);
                        foreach (int caseOffset in operands)
                        {
                            _writer.Write(caseOffset);
                        }
                        break;
                }
            }
        }

        public byte[] GetResult()
        {
            return _stream.ToArray();
        }
    }
}
