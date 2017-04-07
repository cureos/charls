// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;

namespace CharLS
{
    public class PostProcesSingleComponent : IProcessLine
    {
        private readonly byte[] _rawData;

        private readonly int _bytesPerPixel;

        private readonly int _bytesPerLine;

        private int _position;

        public PostProcesSingleComponent(byte[] rawData, JlsParameters parameters, int bytesPerPixel)
        {
            _rawData = rawData;
            _bytesPerPixel = bytesPerPixel;
            _bytesPerLine = parameters.stride;
            _position = 0;
        }

        public void NewLineDecoded(byte[] source, int pixelCount, int sourceStride)
        {
            Array.Copy(source, 0, _rawData, _position, pixelCount * _bytesPerPixel);
            _position += _bytesPerLine;
        }

        public void NewLineRequested(byte[] dest, int pixelCount, int destStride)
        {
            Array.Copy(_rawData, _position, dest, 0, pixelCount * _bytesPerPixel);
            _position += _bytesPerLine;
        }
    }
}
