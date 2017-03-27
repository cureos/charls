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
        private long _position;

        public ByteStreamInfo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            rawStream = stream;
            rawData = null;
            count = stream.CanSeek ? stream.Length : -1;
        }

        public ByteStreamInfo(byte[] bytes, long length = -1)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            rawStream = null;
            rawData = bytes;
            count = length > 0 ?  length : bytes.Length;
        }

        public Stream rawStream { get; }

        public byte[] rawData { get; }

        public long count { get; private set; }

        public static ByteStreamInfo FromByteArray(byte[] bytes, long count)
        {
            return new ByteStreamInfo(bytes, count);
        }

        public static void SkipBytes(ByteStreamInfo streamInfo, long count)
        {
            if (streamInfo.rawData == null) return;

            streamInfo._position += count;
            streamInfo.count -= count;
        }
    }
}
