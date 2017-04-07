// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Runtime.InteropServices;

namespace CharLS
{
    public class ProcessTransformed<TSample> : IProcessLine
        where TSample : struct
    {
        private readonly int _sizeofSample = Marshal.SizeOf(default(TSample));

        private readonly JlsParameters _params;

        private readonly byte[] _buffer;

        private readonly IColorTransform<TSample> _transform;

        private readonly IColorTransform<TSample> _inverseTransform;

        private readonly ByteStreamInfo _rawPixels;

        public ProcessTransformed(ByteStreamInfo rawStream, JlsParameters info, IColorTransform<TSample> transform)
        {
            _params = info;
            _buffer = new byte[info.width * info.components * _sizeofSample];
            _transform = transform;
            _inverseTransform = transform.Inverse;
            _rawPixels = rawStream;
        }

        public void NewLineDecoded(byte[] source, int pixelCount, int sourceStride)
        {
            var bytesToWrite = pixelCount * _params.components * _sizeofSample;
            if (bytesToWrite > _buffer.Length) throw new charls_error(ApiResult.UncompressedBufferTooSmall);

            DecodeTransform(source, 0, _buffer, pixelCount, sourceStride);
            _rawPixels.Write(_buffer, 0, bytesToWrite);
        }

        public void NewLineRequested(byte[] dest, int pixelCount, int destStride)
        {
            var bytesToRead = pixelCount * _params.components * _sizeofSample;
            if (!_rawPixels.Require(true, bytesToRead))
            {
                var message = $"Not enough bytes available in input buffer, need to read {bytesToRead}";
                throw new charls_error(ApiResult.UncompressedBufferTooSmall, message);
            }

            var bytesRead = _rawPixels.Read(_buffer, 0, bytesToRead);
            Transform(_buffer, dest, 0, destStride, pixelCount);
            if (bytesRead < _params.stride) _rawPixels.Skip(_params.stride - bytesRead);
        }

        private static void TransformLineToQuad(
            byte[] ptypeInput,
            int inputOffset,
            int pixelStrideIn,
            byte[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 4;

            var input = new TSample[SamplesPerPixel * pixelStrideIn];
            ptypeInput.CopyTo(input, inputOffset, 0, ptypeInput.Length);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new Quad<TSample>[pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var pixel = transform.Transform(
                    Convert.ToInt32(input[x]),
                    Convert.ToInt32(input[x + pixelStrideIn]),
                    Convert.ToInt32(input[x + 2 * pixelStrideIn]),
                    Convert.ToInt32(input[x + 3 * pixelStrideIn]));

                output[x] = (Quad<TSample>)pixel;
            }

            output.CopyTo(ptypeBuffer, 0, pixelStride, 0);
        }

        private static void TransformQuadToLine(
            byte[] ptypeBufferIn,
            int pixelStrideIn,
            byte[] ptypeBuffer,
            int bufferOffset,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 4;

            var input = new Quad<TSample>[pixelStrideIn];
            ptypeBufferIn.CopyTo(input, 0, 0, pixelStrideIn);

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

            output.CopyTo(ptypeBuffer, 0, SamplesPerPixel * pixelStride, bufferOffset);
        }

        private static void TransformRgbToBgr(byte[] pDest, int samplesPerPixel, int pixelCount)
        {
            var length = samplesPerPixel * pixelCount;
            var dest = new TSample[length];
            pDest.CopyTo(dest, 0, 0, length);

            for (var i = 0; i < length; i += samplesPerPixel)
            {
                var tmp = dest[i];
                dest[i] = dest[i + 2];
                dest[i + 2] = tmp;
            }

            dest.CopyTo(pDest, 0, length, 0);
        }

        private static void TransformLine(
            byte[] pDest,
            int destOffset,
            byte[] pSrc,
            int srcOffset,
            int pixelCount,
            IColorTransform<TSample> transform)
        {
            var src = new Triplet<TSample>[pixelCount];
            pSrc.CopyTo(src, srcOffset, 0, pixelCount);

            var dest = new Triplet<TSample>[pixelCount];
            for (var i = 0; i < pixelCount; ++i)
            {
                dest[i] = (Triplet<TSample>)transform.Transform(src[i].v1, src[i].v2, src[i].v3);
            }

            dest.CopyTo(pDest, 0, pixelCount, destOffset);
        }

        private static void TransformLineToTriplet(
            byte[] ptypeInput,
            int inputOffset,
            int pixelStrideIn,
            byte[] ptypeBuffer,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 3;

            var input = new TSample[SamplesPerPixel * pixelStrideIn];
            ptypeInput.CopyTo(input, inputOffset, 0, ptypeInput.Length);

            var cpixel = Math.Min(pixelStride, pixelStrideIn);
            var output = new Triplet<TSample>[pixelStride];

            for (var x = 0; x < cpixel; ++x)
            {
                var pixel = transform.Transform(
                    Convert.ToInt32(input[x]),
                    Convert.ToInt32(input[x + pixelStrideIn]),
                    Convert.ToInt32(input[x + 2 * pixelStrideIn]));

                output[x] = (Triplet<TSample>)pixel;
            }

            output.CopyTo(ptypeBuffer, 0, pixelStride, 0);
        }

        private static void TransformTripletToLine(
            byte[] ptypeBufferIn,
            int pixelStrideIn,
            byte[] ptypeBuffer,
            int bufferOffset,
            int pixelStride,
            IColorTransform<TSample> transform)
        {
            const int SamplesPerPixel = 3;

            var input = new Triplet<TSample>[pixelStrideIn];
            ptypeBufferIn.CopyTo(input, 0, 0, pixelStrideIn);

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

            output.CopyTo(ptypeBuffer, 0, SamplesPerPixel * pixelStride, bufferOffset);
        }

        private void Transform(byte[] source, byte[] dest, int destOffset, int destStride, int pixelCount)
        {
            if (_params.outputBgr)
            {
                TransformRgbToBgr(source, _params.components, pixelCount);
            }

            if (_params.components == 3)
            {
                if (_params.interleaveMode == InterleaveMode.Sample)
                {
                    TransformLine(dest, destOffset, source, 0, pixelCount, _transform);
                }
                else
                {
                    TransformTripletToLine(source, pixelCount, dest, destOffset, destStride, _transform);
                }
            }
            else if (_params.components == 4 && _params.interleaveMode == InterleaveMode.Line)
            {
                TransformQuadToLine(source, pixelCount, dest, destOffset, destStride, _transform);
            }
        }

        private void DecodeTransform(byte[] source, int sourceOffset, byte[] rawData, int pixelCount, int byteStride)
        {
            if (_params.components == 3)
            {
                if (_params.interleaveMode == InterleaveMode.Sample)
                {
                    TransformLine(rawData, 0, source, sourceOffset, pixelCount, _inverseTransform);
                }
                else
                {
                    TransformLineToTriplet(source, sourceOffset, byteStride, rawData, pixelCount, _inverseTransform);
                }
            }
            else if (_params.components == 4 && _params.interleaveMode == InterleaveMode.Line)
            {
                TransformLineToQuad(source, sourceOffset, byteStride, rawData, pixelCount, _inverseTransform);
            }

            if (_params.outputBgr)
            {
                TransformRgbToBgr(rawData, _params.components, pixelCount);
            }
        }
    }
}
