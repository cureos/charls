// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;

namespace CharLS
{
    public class PostProcesSingleStream : IProcessLine
    {
        private readonly Stream _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        private readonly bool _canDecode;

        private readonly bool _canEncode;

        public PostProcesSingleStream(Stream rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _canDecode = rawData.CanWrite;
            _canEncode = rawData.CanSeek && rawData.CanRead;
            if (!_canDecode && !_canEncode)
            {
                throw new ArgumentException("Raw data stream does not support reading nor writing", nameof(rawData));
            }

            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
        }

        public void NewLineDecoded(byte[] source, int sourceOffset, int sourceStride, int pixelCount)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support writing");
            _rawData.Write(source, sourceOffset, pixelCount * _bytesPerPixel);
        }

        public void NewLineRequested(byte[] dest, int destOffset, int destStride, int pixelCount)
        {
            if (!_canDecode) throw new InvalidOperationException("Raw data stream does not support reading");

            var bytesToRead = pixelCount * _bytesPerPixel;
            var bytesRead = _rawData.Read(dest, destOffset, bytesToRead);
            if (bytesRead < bytesToRead) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            if (_bytesPerPixel == 2)
            {
                ByteSwap(dest, destOffset, bytesToRead);
            }

            if (_bytesPerLine > bytesToRead)
            {
                _rawData.Seek(_bytesPerLine - bytesToRead, SeekOrigin.Current);
            }
        }

        private static void ByteSwap(byte[] data, int offset, int count)
        {
            if ((count & 1) != 0)
            {
                var message = $"An odd number of bytes ({count}) cannot be swapped.";
                throw new charls_error(ApiResult.InvalidJlsParameters, message);
            }

            for (var i = offset; i < offset + count; i += 2)
            {
                var tmp = data[i];
                data[i] = data[i + 1];
                data[i + 1] = tmp;
            }
        }
    }
}
