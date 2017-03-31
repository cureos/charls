// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.IO;

namespace CharLS
{
    public class PostProcesSingle : IProcessLine
    {
        private readonly ByteStreamInfo _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        private int _position;

        public PostProcesSingle(ByteStreamInfo rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
            _position = 0;
        }

        public void NewLineDecoded(byte[] source, int sourceStride, int pixelCount)
        {
            _rawData.Write(source, 0, pixelCount * _bytesPerPixel);
        }

        public void NewLineRequested(byte[] dest, int destStride, int pixelCount)
        {
            var bytesToRead = pixelCount * _bytesPerPixel;
            var bytesRead = _rawData.Read(dest, 0, pixelCount * _bytesPerPixel);
            if (bytesRead < bytesToRead) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            // TODO Are the remaining operations stream specific?
            if (_bytesPerPixel == 2)
            {
                ByteSwap(dest, 0, bytesToRead);
            }

            if (_bytesPerLine > bytesToRead)
            {
                _rawData.Skip(_bytesPerLine - bytesToRead);
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
