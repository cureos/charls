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
        private byte[] _rawData;

        private int _position;

        public ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            rawStream = stream;
            _rawData = null;
            _position = 0;
            count = stream.CanSeek ? (int)stream.Length : -1;
        }

        public ByteStreamInfo(byte[] bytes, int length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            rawStream = null;
            _rawData = bytes;
            _position = 0;
            count = length > 0 ? length : bytes.Length;
        }

        public Stream rawStream { get; }

        public ArraySegment<byte> rawData => new ArraySegment<byte>(_rawData, _position, count);

        public int count { get; private set; }

        public static ByteStreamInfo FromByteArray(byte[] bytes, int count)
        {
            return new ByteStreamInfo(bytes, count);
        }

        public void Seek(int skip)
        {
            if (_rawData == null) return;

            _position += skip;
            count -= skip;
        }
    }
}
