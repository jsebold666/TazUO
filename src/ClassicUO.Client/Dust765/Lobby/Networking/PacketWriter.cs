using System;
using System.IO;
using System.Text;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public class PacketWriter : IDisposable
    {
        private static byte[] _Bytes = new byte[4];

        int _capacity;
        MemoryStream _stream;

        public PacketWriter()
            : this(32)
        {
        }

        internal PacketWriter(int capacity = 32)
        {
            _stream = new MemoryStream(_capacity = capacity);
        }

        protected Stream BaseStream => _stream;
        public long Count => _stream.Length;

        public byte[] Compile()
        {
            _stream.Flush();
            return _stream.ToArray();
        }

        public void Flush()
        {
            _stream.Flush();
        }

        protected void Flush(int count)
        {
            Flush(0, count);
        }

        protected void Flush(int offset, int count)
        {
            Write(_Bytes, offset, count);
        }

        public void Write(byte[] buffer)
        {
            lock (buffer)
            {
                Write((int)buffer.Length);
                _stream.Write(buffer, 0, buffer.Length);
            }
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public void Write(byte value)
        {
            _stream.WriteByte(value);
        }

        public void Write(short value)
        {
            lock (_Bytes)
            {
                _Bytes[0] = (byte)(value >> 8);
                _Bytes[1] = (byte)(value >> 0);
                Flush(0, 2);
            }
        }

        public void Write(int value)
        {
            lock (_Bytes)
            {
                _Bytes[0] = (byte)(value >> 24);
                _Bytes[1] = (byte)(value >> 16);
                _Bytes[2] = (byte)(value >> 08);
                _Bytes[3] = (byte)(value >> 00);
                Flush(0, 4);
            }
        }

        public void Write(long value)
        {
            lock (_Bytes)
            {
                _Bytes[0] = (byte)(value >> 56);
                _Bytes[1] = (byte)(value >> 48);
                _Bytes[2] = (byte)(value >> 40);
                _Bytes[4] = (byte)(value >> 32);
                Flush(0, 4);
                _Bytes[0] = (byte)(value >> 24);
                _Bytes[1] = (byte)(value >> 16);
                _Bytes[2] = (byte)(value >> 08);
                _Bytes[3] = (byte)(value >> 00);
                Flush(0, 4);
            }
        }

        public void Write(sbyte value) => Write((byte)value);
        public void Write(ushort value) => Write((short)value);
        public void Write(uint value) => Write((int)value);
        public void Write(ulong value) => Write((long)value);
        public void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public void Write(string text)
        {
            if(text == null || text.Length == 0)
                return;

            Write(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Fills the stream from the current position up to (capacity) with 0x00's
        /// </summary>
        public void Fill() => Fill((int)( _capacity - _stream.Length ));

        /// <summary>
        /// Writes a number of 0x00 byte values to the underlying stream.
        /// </summary>
        public void Fill(int count)
        {
            if (_stream.Position != _stream.Length)
            {
                Write(new byte[count], 0, count);
            }
            else
            {
                _stream.SetLength((_stream.Length + count));
                _stream.Position = _stream.Length;
            }
        }

        /// <summary>
        /// Offsets the current position from an origin.
        /// </summary>
        public long Seek(long offset, SeekOrigin loc) => _stream.Seek(offset, loc);

        #region IDisposable Members

        public void Dispose()
        {
            _capacity = 0;
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion IDisposable Members
    }
}
