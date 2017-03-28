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
            ArraySegment<byte> ptypeInput,
            int pixelStrideIn,
            ArraySegment<byte> ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 4;

            var input = new TSample[SamplesPerPixel * pixelStrideIn];
            ptypeInput.CopyTo(input, 0, SamplesPerPixel * pixelStrideIn);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new Quad<TSample>[pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var pixel =
                    transform.Transform(
                        (int)(object)input[x],
                        (int)(object)input[x + pixelStrideIn],
                        (int)(object)input[x + 2 * pixelStrideIn],
                        (int)(object)input[x + 3 * pixelStrideIn]);

                output[x] = (Quad<TSample>)pixel;
            }

            output.CopyTo(ptypeBuffer, 0, pixelStride);
        }

        private static void TransformQuadToLine(
            ArraySegment<byte> ptypeBufferIn,
            int pixelStrideIn,
            ArraySegment<byte> ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 4;

            var input = new Quad<TSample>[pixelStrideIn];
            ptypeBufferIn.CopyTo(input, 0, pixelStrideIn);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new TSample[SamplesPerPixel * pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var color = input[x];
                var colorTranformed = (Quad<TSample>)transform.Transform(color.v1, color.v2, color.v3, color.v4);

                output[x] = colorTranformed.R;
                output[x + pixelStride] = colorTranformed.G;
                output[x + 2 * pixelStride] = colorTranformed.B;
                output[x + 3 * pixelStride] = colorTranformed.A;
            }

            output.CopyTo(ptypeBuffer, 0, SamplesPerPixel * pixelStride);
        }

        private static void TransformRgbToBgr(ArraySegment<byte> pDest, int samplesPerPixel, int pixelCount)
        {
            var length = samplesPerPixel * pixelCount;
            var dest = new TSample[length];
            pDest.CopyTo(dest, 0, length);

            for (var i = 0; i < length; i += samplesPerPixel)
            {
                var tmp = dest[i];
                dest[i] = dest[i + 2];
                dest[i + 2] = tmp;
            }

            dest.CopyTo(pDest, 0, length);
        }

        private static void TransformLine(
            ArraySegment<byte> pDest,
            ArraySegment<byte> pSrc,
            int pixelCount,
            IColorTransform<TSample> transform)
        {
            var src = new Triplet<TSample>[pixelCount];
            pSrc.CopyTo(src, 0, pixelCount);

            var dest = new Triplet<TSample>[pixelCount];
            for (var i = 0; i < pixelCount; ++i)
            {
                dest[i] = (Triplet<TSample>)transform.Transform(src[i].v1, src[i].v2, src[i].v3);
            }

            dest.CopyTo(pDest, 0, pixelCount);
        }

        private static void TransformLineToTriplet(
            ArraySegment<byte> ptypeInput,
            int pixelStrideIn,
            ArraySegment<byte> ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 3;

            var input = new TSample[SamplesPerPixel * pixelStrideIn];
            ptypeInput.CopyTo(input, 0, SamplesPerPixel * pixelStrideIn);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new Triplet<TSample>[pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var pixel = transform.Transform(
                    (int)(object)input[x],
                    (int)(object)input[x + pixelStrideIn],
                    (int)(object)input[x + 2 * pixelStrideIn]);

                output[x] = (Triplet<TSample>)pixel;
            }

            output.CopyTo(ptypeBuffer, 0, pixelStride);
        }

        private static void TransformTripletToLine(
            /*Triplet<TSample>[]*/ ArraySegment<byte> ptypeBufferIn,
            int pixelStrideIn,
            /*TSample[]*/ ArraySegment<byte> ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 3;

            var input = new Triplet<TSample>[pixelStrideIn];
            ptypeBufferIn.CopyTo(input, 0, pixelStrideIn);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new TSample[SamplesPerPixel * pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var color = input[x];
                var colorTranformed = (Triplet<TSample>)transform.Transform(color.v1, color.v2, color.v3);

                output[x] = colorTranformed.R;
                output[x + pixelStride] = colorTranformed.G;
                output[x + 2 * pixelStride] = colorTranformed.B;
            }

            output.CopyTo(ptypeBuffer, 0, SamplesPerPixel * pixelStride);
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
