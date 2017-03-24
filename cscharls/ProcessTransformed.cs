// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CharLS
{
    public class ProcessTransformed<TSample> : IProcessLine<TSample>
        where TSample : struct
    {
        private readonly JlsParameters _params;

        private readonly List<TSample> _templine;

        private readonly List<byte> _buffer;

        private readonly IColorTransform<TSample> _transform;

        private readonly ByteStreamInfo _rawPixels;

        public ProcessTransformed(ByteStreamInfo rawStream, JlsParameters info, IColorTransform<TSample> transform)
        {
            _params = info;
            _templine = new List<TSample>(info.width * info.components);
            _buffer = new List<byte>(info.width * info.components * Marshal.SizeOf(default(TSample)));
            _transform = transform;
            _rawPixels = rawStream;
        }

        // TODO Implement!!!

        public void NewLineDecoded(TSample[] pSrc, int pixelCount, int sourceStride)
        {
            throw new System.NotImplementedException();
        }

        public void NewLineRequested(TSample[] pDest, int pixelCount, int destStride)
        {
            throw new System.NotImplementedException();
        }
    }
}

/*
    void NewLineRequested(void* dest, int pixelCount, int destStride) override
    {
        if (!_rawPixels.rawStream)
        {
            Transform(_rawPixels.rawData, dest, pixelCount, destStride);
            _rawPixels.rawData += _params.stride;
            return;
        }

        Transform(_rawPixels.rawStream, dest, pixelCount, destStride);
    }

    void Transform(std::basic_streambuf<char>* rawStream, void* dest, int pixelCount, int destStride)
    {
        std::streamsize bytesToRead = pixelCount * _params.components * sizeof(SAMPLE);
        while (bytesToRead != 0)
        {
            const auto read = rawStream->sgetn(reinterpret_cast<char*>(_buffer.data()), bytesToRead);
            if (read == 0)
            {
                std::ostringstream message;
                message << "No more bytes available in input buffer, still neededing " << read;
                throw charls_error(charls::ApiResult::UncompressedBufferTooSmall, message.str());
            }

            bytesToRead -= read;
        }
        Transform(_buffer.data(), dest, pixelCount, destStride);
    }

    void Transform(const void* source, void* dest, int pixelCount, int destStride)
    {
        if (_params.outputBgr)
        {
            memcpy(_templine.data(), source, sizeof(Triplet<SAMPLE>) * pixelCount);
            TransformRgbToBgr(_templine.data(), _params.components, pixelCount);
            source = _templine.data();
        }

        if (_params.components == 3)
        {
            if (_params.interleaveMode == charls::InterleaveMode::Sample)
            {
                TransformLine(static_cast<Triplet<SAMPLE>*>(dest), static_cast<const Triplet<SAMPLE>*>(source), pixelCount, _transform);
            }
            else
            {
                TransformTripletToLine(static_cast<const Triplet<SAMPLE>*>(source), pixelCount, static_cast<SAMPLE*>(dest), destStride, _transform);
            }
        }
        else if (_params.components == 4 && _params.interleaveMode == charls::InterleaveMode::Line)
        {
            TransformQuadToLine(static_cast<const Quad<SAMPLE>*>(source), pixelCount, static_cast<SAMPLE*>(dest), destStride, _transform);
        }
    }

    void DecodeTransform(const void* pSrc, void* rawData, int pixelCount, int byteStride)
    {
        if (_params.components == 3)
        {
            if (_params.interleaveMode == charls::InterleaveMode::Sample)
            {
                TransformLine(static_cast<Triplet<SAMPLE>*>(rawData), static_cast<const Triplet<SAMPLE>*>(pSrc), pixelCount, _inverseTransform);
            }
            else
            {
                TransformLineToTriplet(static_cast<const SAMPLE*>(pSrc), byteStride, static_cast<Triplet<SAMPLE>*>(rawData), pixelCount, _inverseTransform);
            }
        }
        else if (_params.components == 4 && _params.interleaveMode == charls::InterleaveMode::Line)
        {
            TransformLineToQuad(static_cast<const SAMPLE*>(pSrc), byteStride, static_cast<Quad<SAMPLE>*>(rawData), pixelCount, _inverseTransform);
        }

        if (_params.outputBgr)
        {
            TransformRgbToBgr(static_cast<SAMPLE*>(rawData), _params.components, pixelCount);
        }
    }

    void NewLineDecoded(const void* pSrc, int pixelCount, int sourceStride) override
    {
        if (_rawPixels.rawStream)
        {
            const std::streamsize bytesToWrite = pixelCount * _params.components * sizeof(SAMPLE);
            DecodeTransform(pSrc, _buffer.data(), pixelCount, sourceStride);

            const auto bytesWritten = _rawPixels.rawStream->sputn(reinterpret_cast<char*>(_buffer.data()), bytesToWrite);
            if (bytesWritten != bytesToWrite)
                throw charls_error(charls::ApiResult::UncompressedBufferTooSmall);
        }
        else
        {
            DecodeTransform(pSrc, _rawPixels.rawData, pixelCount, sourceStride);
            _rawPixels.rawData += _params.stride;
        }
    }

template<typename TRANSFORM, typename SAMPLE>
void TransformLineToQuad(const SAMPLE* ptypeInput, int32_t pixelStrideIn, Quad<SAMPLE>* pbyteBuffer, int32_t pixelStride, TRANSFORM& transform)
{
    const int cpixel = std::min(pixelStride, pixelStrideIn);
    Quad<SAMPLE>* ptypeBuffer = pbyteBuffer;

    for (auto x = 0; x < cpixel; ++x)
    {
        Quad<SAMPLE> pixel(transform(ptypeInput[x], ptypeInput[x + pixelStrideIn], ptypeInput[x + 2*pixelStrideIn]), ptypeInput[x + 3 * pixelStrideIn]);
        ptypeBuffer[x] = pixel;
    }
}


template<typename TRANSFORM, typename SAMPLE>
void TransformQuadToLine(const Quad<SAMPLE>* pbyteInput, int32_t pixelStrideIn, SAMPLE* ptypeBuffer, int32_t pixelStride, TRANSFORM& transform)
{
    const auto cpixel = std::min(pixelStride, pixelStrideIn);
    const Quad<SAMPLE>* ptypeBufferIn = pbyteInput;

    for (auto x = 0; x < cpixel; ++x)
    {
        const Quad<SAMPLE> color = ptypeBufferIn[x];
        Quad<SAMPLE> colorTranformed(transform(color.v1, color.v2, color.v3), color.v4);

        ptypeBuffer[x] = colorTranformed.v1;
        ptypeBuffer[x + pixelStride] = colorTranformed.v2;
        ptypeBuffer[x + 2 * pixelStride] = colorTranformed.v3;
        ptypeBuffer[x + 3 * pixelStride] = colorTranformed.v4;
    }
}


template<typename SAMPLE>
void TransformRgbToBgr(SAMPLE* pDest, int samplesPerPixel, int pixelCount)
{
    for (auto i = 0; i < pixelCount; ++i)
    {
        std::swap(pDest[0], pDest[2]);
        pDest += samplesPerPixel;
    }
}


template<typename TRANSFORM, typename SAMPLE>
void TransformLine(Triplet<SAMPLE>* pDest, const Triplet<SAMPLE>* pSrc, int pixelCount, TRANSFORM& transform)
{
    for (auto i = 0; i < pixelCount; ++i)
    {
        pDest[i] = transform(pSrc[i].v1, pSrc[i].v2, pSrc[i].v3);
    }
}


template<typename TRANSFORM, typename SAMPLE>
void TransformLineToTriplet(const SAMPLE* ptypeInput, int32_t pixelStrideIn, Triplet<SAMPLE>* pbyteBuffer, int32_t pixelStride, TRANSFORM& transform)
{
    const auto cpixel = std::min(pixelStride, pixelStrideIn);
    Triplet<SAMPLE>* ptypeBuffer = pbyteBuffer;

    for (auto x = 0; x < cpixel; ++x)
    {
        ptypeBuffer[x] = transform(ptypeInput[x], ptypeInput[x + pixelStrideIn], ptypeInput[x + 2*pixelStrideIn]);
    }
}


template<typename TRANSFORM, typename SAMPLE>
void TransformTripletToLine(const Triplet<SAMPLE>* pbyteInput, int32_t pixelStrideIn, SAMPLE* ptypeBuffer, int32_t pixelStride, TRANSFORM& transform)
{
    const auto cpixel = std::min(pixelStride, pixelStrideIn);
    const Triplet<SAMPLE>* ptypeBufferIn = pbyteInput;

    for (auto x = 0; x < cpixel; ++x)
    {
        const Triplet<SAMPLE> color = ptypeBufferIn[x];
        Triplet<SAMPLE> colorTranformed = transform(color.v1, color.v2, color.v3);

        ptypeBuffer[x] = colorTranformed.v1;
        ptypeBuffer[x + pixelStride] = colorTranformed.v2;
        ptypeBuffer[x + 2 *pixelStride] = colorTranformed.v3;
    }
}
*/
