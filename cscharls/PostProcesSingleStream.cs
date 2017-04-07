// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

namespace CharLS
{
    public class PostProcesSingleStream : IProcessLine
    {
        private readonly ByteStreamInfo _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        public PostProcesSingleStream(ByteStreamInfo rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
        }

        public void NewLineDecoded(byte[] source, int pixelCount, int sourceStride)
        {
            var bytesToWrite = (ulong)(pixelCount * _bytesPerPixel);
            var bytesWritten = _rawData.Write(source, 0, pixelCount * _bytesPerPixel);
            if (bytesWritten != bytesToWrite)
                throw new charls_error(ApiResult.UncompressedBufferTooSmall);
        }

        public void NewLineRequested(byte[] dest, int pixelCount, int destStride)
        {
            var bytesToRead = pixelCount * _bytesPerPixel;
            var bytesRead = _rawData.Read(dest, 0, bytesToRead);
            if (bytesRead < bytesToRead) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            if (_bytesPerPixel == 2)
            {
                ByteSwap(dest, bytesToRead);
            }

            if (_bytesPerLine - bytesToRead > 0)
            {
                _rawData.Skip(_bytesPerLine - bytesToRead);
            }
        }

        private static void ByteSwap(byte[] data, int count)
        {
            if ((count & 1) != 0)
            {
                var message = $"An odd number of bytes ({count}) cannot be swapped.";
                throw new charls_error(ApiResult.InvalidJlsParameters, message);
            }

            for (var i = 0; i < count; i += 2)
            {
                var tmp = data[i];
                data[i] = data[i + 1];
                data[i + 1] = tmp;
            }
        }
    }
}
