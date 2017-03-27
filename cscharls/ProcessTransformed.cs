// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CharLS
{
    public class ProcessTransformed<TSample> : IProcessLine
        where TSample : struct
    {
        private readonly int _sizeofSample = Marshal.SizeOf(default(TSample));

        private readonly JlsParameters _params;

        private readonly ArraySegment<byte> _buffer;

        private readonly IColorTransform<TSample> _transform;

        private readonly IColorTransform<TSample> _inverseTransform;

        private readonly ByteStreamInfo _rawPixels;

        public ProcessTransformed(ByteStreamInfo rawStream, JlsParameters info, IColorTransform<TSample> transform)
        {
            _params = info;
            _buffer = new ArraySegment<byte>(new byte[info.width * info.components * _sizeofSample]);
            _transform = transform;
            _inverseTransform = transform.Inverse;
            _rawPixels = rawStream;
        }

        public void NewLineDecoded(ArraySegment<byte> pSrc, int pixelCount, int sourceStride)
        {
            if (_rawPixels.rawStream != null)
            {
                var bytesToWrite = pixelCount * _params.components * _sizeofSample;
                if (bytesToWrite > _buffer.Array.Length)
                    throw new charls_error(ApiResult.UncompressedBufferTooSmall);

                DecodeTransform(pSrc, _buffer, pixelCount, sourceStride);
                _rawPixels.rawStream.Write(_buffer.Array, 0, bytesToWrite);
            }
            else
            {
                DecodeTransform(pSrc, _rawPixels.rawData, pixelCount, sourceStride);
                _rawPixels.Seek(_params.stride);
            }
        }

        public void NewLineRequested(ArraySegment<byte> pDest, int pixelCount, int destStride)
        {
            if (_rawPixels.rawStream == null)
            {
                Transform(_rawPixels.rawData, pDest, pixelCount, destStride);
                _rawPixels.Seek(_params.stride);
                return;
            }

            Transform(_rawPixels.rawStream, pDest, pixelCount, destStride);
        }

        private static void TransformLineToQuad(
            TSample[] ptypeInput,
            int pixelStrideIn,
            Quad<TSample>[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            var cpixel = Math.Min(pixelStride, pixelStrideIn);

            for (var x = 0; x < cpixel; ++x)
            {
                var pixel =
                    new Quad<TSample>(
                        transform.Transform(
                            (int)(object)ptypeInput[x],
                            (int)(object)ptypeInput[x + pixelStrideIn],
                            (int)(object)ptypeInput[x + 2 * pixelStrideIn]),
                        (int)(object)ptypeInput[x + 3 * pixelStrideIn]);

                ptypeBuffer[x] = pixel;
            }
        }

        private static void TransformQuadToLine(
            Quad<TSample>[] ptypeBufferIn,
            int pixelStrideIn,
            TSample[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            var cpixel = Math.Min(pixelStride, pixelStrideIn);

            for (var x = 0; x < cpixel; ++x)
            {
                var color = ptypeBufferIn[x];
                var colorTranformed = (Quad<TSample>)transform.Transform(color.v1, color.v2, color.v3, color.v4);

                ptypeBuffer[x] = colorTranformed.R;
                ptypeBuffer[x + pixelStride] = colorTranformed.G;
                ptypeBuffer[x + 2 * pixelStride] = colorTranformed.B;
                ptypeBuffer[x + 3 * pixelStride] = colorTranformed.A;
            }
        }

        private static void TransformRgbToBgr(TSample[] pDest, int samplesPerPixel, int pixelCount)
        {
            for (var i = 0; i < samplesPerPixel * pixelCount; i += samplesPerPixel)
            {
                var tmp = pDest[i];
                pDest[i] = pDest[i + 2];
                pDest[i + 2] = tmp;
            }
        }

        private static void TransformLine(
            ITriplet<TSample>[] pDest,
            Triplet<TSample>[] pSrc,
            int pixelCount,
            IColorTransform<TSample> transform)
        {
            for (var i = 0; i < pixelCount; ++i)
            {
                pDest[i] = transform.Transform(pSrc[i].v1, pSrc[i].v2, pSrc[i].v3);
            }
        }

        private static void TransformLineToTriplet(
            TSample[] ptypeInput,
            int pixelStrideIn,
            ITriplet<TSample>[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            var cpixel = Math.Min(pixelStride, pixelStrideIn);

            for (var x = 0; x < cpixel; ++x)
            {
                ptypeBuffer[x] = transform.Transform(
                    (int)(object)ptypeInput[x],
                    (int)(object)ptypeInput[x + pixelStrideIn],
                    (int)(object)ptypeInput[x + 2 * pixelStrideIn]);
            }
        }

        private static void TransformTripletToLine(
            Triplet<TSample>[] ptypeBufferIn,
            int pixelStrideIn,
            TSample[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            var cpixel = Math.Min(pixelStride, pixelStrideIn);

            for (var x = 0; x < cpixel; ++x)
            {
                var color = ptypeBufferIn[x];
                var colorTranformed = transform.Transform(color.v1, color.v2, color.v3);

                ptypeBuffer[x] = colorTranformed.R;
                ptypeBuffer[x + pixelStride] = colorTranformed.G;
                ptypeBuffer[x + 2 * pixelStride] = colorTranformed.B;
            }
        }

        private void Transform(Stream rawStream, ArraySegment<byte> dest, int pixelCount, int destStride)
        {
            var bytesToRead = pixelCount * _params.components * _sizeofSample;
            var read = rawStream.Read(_buffer.Array, 0, bytesToRead);
            if (read < bytesToRead)
            {
                var message = $"No more bytes available in input buffer, still needing {bytesToRead - read}";
                throw new charls_error(ApiResult.UncompressedBufferTooSmall, message);
            }

            Transform(_buffer, dest, pixelCount, destStride);
        }

        private void Transform(ArraySegment<byte> source, ArraySegment<byte> dest, int pixelCount, int destStride)
        {
            if (_params.outputBgr)
            {
                TransformRgbToBgr(source, _params.components, pixelCount);
            }

            if (_params.components == 3)
            {
                if (_params.interleaveMode == InterleaveMode.Sample)
                {
                    TransformLine(dest, source, pixelCount, _transform);
                }
                else
                {
                    TransformTripletToLine(source, pixelCount, dest, destStride, _transform);
                }
            }
            else if (_params.components == 4 && _params.interleaveMode == InterleaveMode.Line)
            {
                TransformQuadToLine(source, pixelCount, dest, destStride, _transform);
            }
        }

        private void DecodeTransform(ArraySegment<byte> pSrc, ArraySegment<byte> rawData, int pixelCount, int byteStride)
        {
            if (_params.components == 3)
            {
                if (_params.interleaveMode == InterleaveMode.Sample)
                {
                    TransformLine(rawData, pSrc, pixelCount, _inverseTransform);
                }
                else
                {
                    TransformLineToTriplet(pSrc, byteStride, rawData, pixelCount, _inverseTransform);
                }
            }
            else if (_params.components == 4 && _params.interleaveMode == InterleaveMode.Line)
            {
                TransformLineToQuad(pSrc, byteStride, rawData, pixelCount, _inverseTransform);
            }

            if (_params.outputBgr)
            {
                TransformRgbToBgr(rawData, _params.components, pixelCount);
            }
        }
    }
}
