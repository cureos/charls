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

            IsBuffered = false;
            _canRead = stream.CanRead;
            _canWrite = stream.CanWrite;
            _canSeek = stream.CanSeek;
        }

        internal ByteStreamInfo(byte[] bytes, int length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            _rawStream = null;
            _rawData = bytes;
            _arrayPosition = 0;

            IsBuffered = true;
            _canRead = true;
            _canWrite = true;
            _canSeek = true;

            _arrayLength = length > 0 ? Math.Min(length, bytes.Length) : bytes.Length;
        }

        public bool IsBuffered { get; }

        public int Position
        {
            get
            {
                if (!_canSeek) throw new InvalidOperationException();
                return IsBuffered ? _arrayPosition : (int)_rawStream.Position;
            }

            set
            {
                if (!_canSeek) throw new InvalidOperationException();
                if (IsBuffered)
                {
                    _arrayPosition = value;
                }
                else
                {
                    _rawStream.Position = value;
                }
            }
        }

        public byte[] Buffer
        {
            get
            {
                if (!IsBuffered) throw new InvalidOperationException();

                return _rawData;
            }
        }

        public void Skip(int count)
        {
            Position += count;
        }

        public bool Require(bool read, int count)
        {
            if (IsBuffered) return _arrayPosition + count <= _arrayLength;
            return read ? _canRead : _canWrite;
        }

        public byte ReadByte()
        {
            if (!_canRead) throw new InvalidOperationException();

            if (IsBuffered)
            {
                if (_arrayPosition >= _arrayLength) throw new EndOfStreamException();
                return _rawData[_arrayPosition++];
            }

            var val = _rawStream.ReadByte();
            if (val < 0) throw new EndOfStreamException();
            return (byte)val;
        }

        public int Read(byte[] bytes, int offset, int count)
        {
            if (!_canRead) throw new InvalidOperationException();
            if (offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Array too small for specified offset and count");
            }

            if (IsBuffered)
            {
                count = Math.Min(count, _arrayLength - _arrayPosition);
                if (count > 0)
                {
                    Array.Copy(_rawData, _arrayPosition, bytes, offset, count);
                    _arrayPosition += count;
                }

                return count;
            }

            return _rawStream.Read(bytes, offset, count);
        }

        public void WriteByte(byte value)
        {
            if (!_canWrite) throw new InvalidOperationException();

            if (IsBuffered)
            {
                if (_arrayPosition >= _arrayLength) throw new EndOfStreamException();
                _rawData[_arrayPosition++] = value;
            }
            else
            {
                _rawStream.WriteByte(value);
            }
        }

        public ulong Write(byte[] bytes, int offset = 0, int count = -1)
        {
            if (!_canWrite) throw new InvalidOperationException();
            if (count < 0) count = bytes.Length;

            if (IsBuffered)
            {
                count = Math.Min(count, _arrayLength - _arrayPosition);
                Array.Copy(bytes, offset, _rawData, _arrayPosition, count);
                _arrayPosition += count;
            }
            else
            {
                _rawStream.Write(bytes, offset, count);
            }

            return (ulong)count;
        }
    }
}
