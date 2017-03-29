// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;

namespace CharLS
{
    // 
    // ByteStreamInfo & FromByteArray helper function
    // 
    // ByteStreamInfo describes the stream: either set rawStream to a valid stream, or rawData/count, not both.
    // it's possible to decode to memorystreams, but using rawData will always be faster.
    // 
    // Example use:
    // ByteStreamInfo streamInfo = { fileStream.rdbuf() };
    // or
    // ByteStreamInfo streamInfo = FromByteArray( bytePtr, byteCount);
    // 
    public class ByteStreamInfo
    {
        private readonly bool _isStream;

        private readonly bool _canRead;

        private readonly bool _canWrite;

        private readonly bool _canSeek;

        private readonly byte[] _rawData;

        private int _arrayPosition;

        private readonly int _arrayLength;

        public ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            rawStream = stream;
            _rawData = null;
            _arrayPosition = 0;

            _isStream = true;
            _canRead = stream.CanRead;
            _canWrite = stream.CanWrite;
            _canSeek = stream.CanSeek;
        }

        public ByteStreamInfo(byte[] bytes, int length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            rawStream = null;
            _rawData = bytes;
            _arrayPosition = 0;

            _isStream = false;
            _canRead = true;
            _canWrite = true;
            _canSeek = true;

            _arrayLength = length > 0 ? Math.Min(length, bytes.Length) : bytes.Length;
        }

        public Stream rawStream { get; }

        public ArraySegment<byte> rawData => new ArraySegment<byte>(_rawData, _arrayPosition, Count);

        public int Count
        {
            get
            {
                if (!_canSeek) throw new InvalidOperationException();
                return _isStream ? (int)rawStream.Length : _arrayLength;
            }
        }

        public int Position
        {
            get
            {
                if (!_canSeek) throw new InvalidOperationException();
                return _isStream ? (int)rawStream.Position : _arrayPosition;
            }

            set
            {
                if (!_canSeek) throw new InvalidOperationException();
                if (_isStream)
                {
                    rawStream.Position = value;
                }
                else
                {
                    _arrayPosition = value;
                }
            }
        }

        public static ByteStreamInfo FromByteArray(byte[] bytes, int count)
        {
            return new ByteStreamInfo(bytes, count);
        }

        public void Seek(int offset)
        {
            Position += offset;
        }

        public bool Require(int count)
        {
            if (_isStream) return true;
            return _arrayPosition + count <= _arrayLength;
        }

        public byte ReadByte()
        {
            if (!_canRead) throw new InvalidOperationException();

            if (_isStream)
            {
                var val = rawStream.ReadByte();
                if (val < 0) throw new EndOfStreamException();
                return (byte)val;
            }

            if (_arrayPosition + 1 >= _arrayLength) throw new EndOfStreamException();
            return _rawData[_arrayPosition++];
        }

        public byte[] ReadBytes(int count)
        {
            if (!_canRead) throw new InvalidOperationException();

            if (_isStream)
            {
                var bytes = new byte[count];
                var bytesRead = rawStream.Read(bytes, 0, count);
                if (bytesRead < count) throw new EndOfStreamException();
                return bytes;
            }
            else
            {
                if (_arrayPosition + count >= _arrayLength) throw new EndOfStreamException();
                var bytes = new byte[count];
                Array.Copy(_rawData, _arrayPosition, bytes, 0, count);
                _arrayPosition += count;
                return bytes;
            }
        }

        public void WriteByte(byte value)
        {
            if (!_canWrite) throw new InvalidOperationException();

            if (_isStream)
            {
                rawStream.WriteByte(value);
            }
            else
            {
                if (_arrayPosition + 1 >= _arrayLength) throw new EndOfStreamException();
                _rawData[_arrayPosition++] = value;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            if (!_canWrite) throw new InvalidOperationException();

            if (_isStream)
            {
                rawStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                var count = bytes.Length;
                if (_arrayPosition + count >= _arrayLength) throw new EndOfStreamException();
                Array.Copy(bytes, 0, _rawData, _arrayPosition, count);
                _arrayPosition += count;
            }
        }
    }
}
