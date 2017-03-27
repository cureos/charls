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
    //     ByteStreamInfo streamInfo = { fileStream.rdbuf() };
    // or
    //     ByteStreamInfo streamInfo = FromByteArray( bytePtr, byteCount);
    //
    public class ByteStreamInfo
    {
        private byte[] _rawData;

        private long _position;

        public ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            rawStream = stream;
            _rawData = null;
            _position = 0;
            count = stream.CanSeek ? stream.Length : -1;
        }

        public ByteStreamInfo(byte[] bytes, long length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            rawStream = null;
            _rawData = bytes;
            _position = 0;
            count = length > 0 ? length : bytes.Length;
        }

        public Stream rawStream { get; }

        public ArraySegment<byte> rawData => new ArraySegment<byte>(_rawData, (int)_position, (int)count);

        public long count { get; private set; }

        public static ByteStreamInfo FromByteArray(byte[] bytes, long count)
        {
            return new ByteStreamInfo(bytes, count);
        }

        public static void SkipBytes(ByteStreamInfo streamInfo, long count)
        {
            streamInfo.Seek(count);
        }

        public void Seek(long skip)
        {
            if (_rawData == null) return;

            _position += skip;
            count -= skip;
        }
    }
}
