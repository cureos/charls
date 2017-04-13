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
    // ByteStreamInfo streamInfo = new ByteStreamInfo(fileStream);
    // or
    // ByteStreamInfo streamInfo = ByteStreamInfo.FromByteArray( bytePtr, byteCount);
    // 
    public class ByteStreamInfo
    {
        private readonly bool _canRead;

        private readonly bool _canWrite;

        private readonly bool _canSeek;

        private readonly Stream _rawStream;

        private readonly byte[] _rawData;

        private int _arrayPosition;

        private readonly int _arrayLength;

        internal ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _rawStream = stream;
            _rawData = null;
            _arrayPosition = 0;

            IsStream = true;
            _canRead = stream.CanRead;
            _canWrite = stream.CanWrite;
            _canSeek = stream.CanSeek;

            Count = 0;
        }

        internal ByteStreamInfo(byte[] bytes, int length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            _rawStream = null;
            _rawData = bytes;
            _arrayPosition = 0;

            IsStream = false;
            _canRead = true;
            _canWrite = true;
            _canSeek = true;

            _arrayLength = length > 0 ? Math.Min(length, bytes.Length) : bytes.Length;
            Count = (ulong)_arrayLength;
        }

        public bool IsStream { get; }

        public int Position
        {
            get
            {
                if (!_canSeek) throw new InvalidOperationException();
                return IsStream ? (int)_rawStream.Position : _arrayPosition;
            }

            set
            {
                if (!_canSeek) throw new InvalidOperationException();
                if (IsStream)
                {
                    _rawStream.Position = value;
                }
                else
                {
                    _arrayPosition = value;
                }
            }
        }

        public Stream Stream => _rawStream;

        public byte[] Data => _rawData;

        public ulong Count { get; set; }

        public void Skip(int count)
        {
            Position += count;
        }

        public bool Require(bool read, int count)
        {
            if (IsStream) return read ? _canRead : _canWrite;
            return _arrayPosition + count <= _arrayLength;
            }

        public byte ReadByte()
        {
            if (!_canRead) throw new InvalidOperationException();

            if (IsStream)
            {
                var val = _rawStream.ReadByte();
                if (val < 0) throw new EndOfStreamException();
                return (byte) val;
            }

            if (_arrayPosition >= _arrayLength) throw new EndOfStreamException();
            return _rawData[_arrayPosition++];
        }

        public int Read(byte[] bytes, int offset, int count)
        {
            if (!_canRead) throw new InvalidOperationException();
            if (offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Array too small for specified offset and count");
            }

            if (IsStream)
            {
                return _rawStream.Read(bytes, offset, count);
            }

            count = Math.Min(count, _arrayLength - _arrayPosition);
            if (count > 0)
            {
                Array.Copy(_rawData, _arrayPosition, bytes, offset, count);
                _arrayPosition += count;
            }

            return count;
        }

        public void WriteByte(byte value)
        {
            if (!_canWrite) throw new InvalidOperationException();

            if (IsStream)
            {
                _rawStream.WriteByte(value);
            }
            else
            {
                if (_arrayPosition >= _arrayLength) throw new EndOfStreamException();
                _rawData[_arrayPosition++] = value;
            }
        }

        public ulong Write(byte[] bytes, int offset = 0, int count = -1)
        {
            if (!_canWrite) throw new InvalidOperationException();
            if (count < 0) count = bytes.Length;

            if (IsStream)
            {
                _rawStream.Write(bytes, offset, count);
            }
            else
            {
                count = Math.Min(count, _arrayLength - _arrayPosition);
                Array.Copy(bytes, offset, _rawData, _arrayPosition, count);
                _arrayPosition += count;
            }

            return (ulong)count;
        }
    }
}
