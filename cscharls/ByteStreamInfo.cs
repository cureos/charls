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
        private readonly bool _isStream;

        private readonly bool _canRead;

        private readonly bool _canWrite;

        private readonly bool _canSeek;

        private readonly Stream _rawStream;

        private readonly byte[] _rawData;

        private int _arrayPosition;

        private readonly int _arrayLength;

        public ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _rawStream = stream;
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

            _rawStream = null;
            _rawData = bytes;
            _arrayPosition = 0;

            _isStream = false;
            _canRead = true;
            _canWrite = true;
            _canSeek = true;

            _arrayLength = length > 0 ? Math.Min(length, bytes.Length) : bytes.Length;
        }

        public int Position
        {
            get
            {
                if (!_canSeek) throw new InvalidOperationException();
                return _isStream ? (int)_rawStream.Position : _arrayPosition;
            }

            set
            {
                if (!_canSeek) throw new InvalidOperationException();
                if (_isStream)
                {
                    _rawStream.Position = value;
                }
                else
                {
                    _arrayPosition = value;
                }
            }
        }

        public void Seek(int offset)
        {
            Position += offset;
        }

        public bool Require(bool read, int count)
        {
            var isModeSupported = read ? _canSeek && _canRead : _canWrite;
            if (_isStream) return isModeSupported;
            return isModeSupported && _arrayPosition + count <= _arrayLength;
        }

        public byte ReadByte()
        {
            if (!_canRead) throw new InvalidOperationException();

            if (_isStream)
            {
                var val = _rawStream.ReadByte();
                if (val < 0) throw new EndOfStreamException();
                return (byte)val;
            }

            if (_arrayPosition + 1 >= _arrayLength) throw new EndOfStreamException();
            return _rawData[_arrayPosition++];
        }

        public int Read(byte[] bytes, int offset, int count)
        {
            if (!_canRead) throw new InvalidOperationException();
            if (offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), "Array too small for specified offset and count");
            }

            if (_isStream)
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

            if (_isStream)
            {
                _rawStream.WriteByte(value);
            }
            else
            {
                if (_arrayPosition + 1 >= _arrayLength) throw new EndOfStreamException();
                _rawData[_arrayPosition++] = value;
            }
        }

        public void WriteBytes(byte[] bytes, int count = -1)
        {
            if (!_canWrite) throw new InvalidOperationException();
            if (count < 0) count = bytes.Length;

            if (_isStream)
            {
                _rawStream.Write(bytes, 0, count);
            }
            else
            {
                if (_arrayPosition + count >= _arrayLength) throw new EndOfStreamException();
                Array.Copy(bytes, 0, _rawData, _arrayPosition, count);
                _arrayPosition += count;
            }
        }
    }
}
