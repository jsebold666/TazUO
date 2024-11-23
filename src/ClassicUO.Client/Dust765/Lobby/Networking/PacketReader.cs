using System;
using System.IO;
using System.Text;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public class PacketReader
    {
        private static PacketReader m_Instance;

        byte[] _data;

        private int _size;
        private int _index;

        private byte _packetID;
        private string _name;
        private Decoder _dec;

        internal static PacketReader Initialize(ArraySegment<byte> segment, PacketHandler handler)
        {
             if (m_Instance == null)
                m_Instance = new PacketReader();

            m_Instance._data = segment.Array;
            m_Instance._size = segment.Count;
            m_Instance._index = 3;
            m_Instance._packetID = (byte)handler.PacketID;
            m_Instance._name = handler.Name;
            m_Instance._dec = Encoding.UTF8.GetDecoder();

            return m_Instance;
        }

        public string Name { get { return _name; } set { _name = value; } }

        public byte[] Buffer => _data;
        public bool Finished => _index >= _size;
        public int Size => _size;

        public int Seek(int offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _index = offset;
                    break;

                case SeekOrigin.Current:
                    _index += offset;
                    break;

                case SeekOrigin.End:
                    _index = _size - offset;
                    break;
            }

            return _index;
        }

        public bool ReadBoolean() => ( _index + 1 ) > _size ? false : ( _data[_index++] != 0 );

        public byte[] ReadBuffer()
        {
            int length = ReadInt32();
            byte[] data = new byte[length];
            if(data.Length > 0)
            {
                int i = 0;
                do data[i++] = ReadByte();
                while(i < length);
            }
            return data;
        }

        public string ReadString()
        {
            byte[] buff = ReadBuffer();
            char[] chrs = new char[buff.Length];
            int charLen = _dec.GetChars(buff, 0, buff.Length, chrs, 0);
            return new string(chrs, 0, charLen);
        }

        #region unsigned

         public uint ReadUInt32()
        {
            if ((_index + 4) > _size)
                return 0;

            return (uint)((_data[_index++] << 24) | (_data[_index++] << 16) | (_data[_index++] << 8) | _data[_index++]);
        }

        public ushort ReadUInt16()
        {
            if ((_index + 2) > _size)
                return 0;

            return (ushort)((_data[_index++] << 8) | _data[_index++]);
        }

        public byte ReadByte()
        {
            if(( _index + 1 ) > _size)
                return 0;

            return _data[_index++];
        }

        #endregion

        #region signed
         public int ReadInt32()
        {
            if(( _index + 4 ) > _size)
                return 0;

            return (_data[_index++] << 24)
                 | (_data[_index++] << 16)
                 | (_data[_index++] << 8)
                 | _data[_index++];
        }

        public short ReadInt16() => (short)( ( _index + 2 ) > _size ? 0 : ( _data[_index++] << 8 ) | _data[_index++] );
        public sbyte ReadSByte() => (sbyte)( ( _index + 1 ) > _size ? 0 : ( _data[_index++] ) );

        #endregion

        public void Trace()
        {
            Trace(false);
        }

        public void Trace(bool buffer)
        {
            Trace(Console.Out, buffer);
        }

        private void Trace(TextWriter output, bool buffer)
        {
            if (output == null)
                output = StreamWriter.Null;

            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            output.WriteLine("(Tracing) '{0}' ( {1} 0x{1:X} ).. {2} bytes", _name, _packetID, _size);
            if (buffer)
            {
                output.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                FormatBuffer(output, new MemoryStream(_data), _size);
            }
            Console.ForegroundColor = color;
            output.WriteLine();
            output.Flush();
            output.Close();
        }

        private static void FormatBuffer(TextWriter output, Stream input, int length)
        {
            output.WriteLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
            output.WriteLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

            int byteIndex = 0;

            int whole = length >> 4;
            int rem = length & 0xF;

            for (int i = 0; i < whole; ++i, byteIndex += 16)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(16);

                for (int j = 0; j < 16; ++j)
                {
                    int c = input.ReadByte();

                    bytes.Append(c.ToString("X2"));

                    if (j != 7)
                        bytes.Append(' ');
                    else
                        bytes.Append("  ");

                    if (c >= 0x20 && c < 0x7F)
                        chars.Append((char)c);
                    else
                        chars.Append('.');
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }

            if (rem != 0)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(rem);

                for (int j = 0; j < 16; ++j)
                {
                    if (j < rem)
                    {
                        int c = input.ReadByte();

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                            bytes.Append(' ');
                        else
                            bytes.Append("  ");

                        if (c >= 0x20 && c < 0x7F) // 32 - 126
                            chars.Append((char)c);
                        else
                            chars.Append('.');
                    }
                    else
                    {
                        bytes.Append("   ");
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }
        }
    }
}
