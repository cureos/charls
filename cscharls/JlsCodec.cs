// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using static CharLS.util;

namespace CharLS
{
    public class JlsCodec<TStrategy, TSample, TPixel>
        where TStrategy : IStrategy, new() where TSample : struct
    {
        private readonly IStrategy _strategy;

        // codec parameters
        private ITraits<TSample, TPixel> traits;

        private JlsRect _rect;

        private int _width;

        private int T1;

        private int T2;

        private int T3;

        // compression context
        private JlsContext[] _contexts /*[365]*/;

        private CContextRunMode[] _contextRunmode /*[2]*/;

        private int _RUNindex;

        private TPixel[] _previousLine; // previous line ptr

        private TPixel[] _currentLine; // current line ptr

        // quantization lookup table
        private sbyte[] _pquant;

        private IList<sbyte> _rgquant;

        public JlsCodec(ITraits<TSample, TPixel> inTraits, JlsParameters parameters)
        {
            _strategy = new TStrategy { Parameters = parameters };
            traits = inTraits;
            _rect = new JlsRect();
            _width = parameters.width;
            T1 = 0;
            T2 = 0;
            T3 = 0;
            _RUNindex = 0;
            _previousLine = null;
            _currentLine = null;
            _pquant = null;
        }

        private int ApplySign(int i, int sign)
        {
            return (sign ^ i) - sign;
        }


        private int GetPredictedValue(int Ra, int Rb, int Rc)
        {
            // sign trick reduces the number of if statements (branches)
            int sgn = BitWiseSign(Rb - Ra);

            // is Ra between Rc and Rb?
            if ((sgn ^ (Rc - Ra)) < 0)
            {
                return Rb;
            }
            if ((sgn ^ (Rb - Rc)) < 0)
            {
                return Ra;
            }

            // default case, valid if Rc element of [Ra,Rb]
            return Ra + Rb - Rc;
        }

        private int UnMapErrVal(int mappedError)
        {
            int sign = (mappedError << (INT32_BITCOUNT - 1)) >> (INT32_BITCOUNT - 1);
            return sign ^ (mappedError >> 1);
        }

        private int GetMappedErrVal(int Errval)
        {
            int mappedError = (Errval >> (INT32_BITCOUNT - 2)) ^ (2 * Errval);
            return mappedError;
        }

        private int ComputeContextID(int Q1, int Q2, int Q3)
        {
            return (Q1 * 9 + Q2) * 9 + Q3;
        }



        public void SetPresets(JpegLSPresetCodingParameters presets)
        {
            JpegLSPresetCodingParameters presetDefault = ComputeDefault(traits.MAXVAL, traits.NEAR);

            InitParams(
                presets.Threshold1 != 0 ? presets.Threshold1 : presetDefault.Threshold1,
                presets.Threshold2 != 0 ? presets.Threshold2 : presetDefault.Threshold2,
                presets.Threshold3 != 0 ? presets.Threshold3 : presetDefault.Threshold3,
                presets.ResetValue != 0 ? presets.ResetValue : presetDefault.ResetValue);
        }

        private bool IsInterleaved()
        {
            if (Info().interleaveMode == InterleaveMode.None) return false;

            if (Info().components == 1) return false;

            return true;
        }

        private JlsParameters Info()
        {
            return _strategy.Parameters;
        }

        private int QuantizeGratient(int Di)
        {
            Debug.Debug.Assert(QuantizeGratientOrg(Di) == *(_pquant + Di));
            return *(_pquant + Di);
        }

        private void IncrementRunIndex()
        {
            _RUNindex = Math.Min(31, _RUNindex + 1);
        }

        private void DecrementRunIndex()
        {
            _RUNindex = Math.Max(0, _RUNindex - 1);
        }



// Functions to build tables used to decode short golomb codes.

private std::pair<int, int> CreateEncodedValue(int k, int mappedError)
{
    const int highbits = mappedError >> k;
    return std::make_pair(highbits + k + 1, (int(1) << k) | (mappedError & ((int(1) << k) - 1)));
}


private CTable InitTable(int k)
{
    CTable table;
    for (short nerr = 0; ; nerr++)
    {
        // Q is not used when k != 0
        const int merrval = GetMappedErrVal(nerr);
        std::pair<int, int> paircode = CreateEncodedValue(k, merrval);
        if (paircode.first > CTable::cbit)
            break;

        Code code(nerr, static_cast<short>(paircode.first));
        table.AddEntry(static_cast<uint8_t>(paircode.second), code);
    }

    for (short nerr = -1; ; nerr--)
    {
        // Q is not used when k != 0
        const int merrval = GetMappedErrVal(nerr);
        std::pair<int, int> paircode = CreateEncodedValue(k, merrval);
        if (paircode.first > CTable::cbit)
            break;

        Code code = Code(nerr, static_cast<short>(paircode.first));
        table.AddEntry(uint8_t(paircode.second), code);
    }

    return table;
}


// Encoding/decoding of golomb codes

int DecodeValue(int k, int limit, int qbpp)
{
    const int highbits = STRATEGY::ReadHighbits();

    if (highbits >= limit - (qbpp + 1))
        return STRATEGY::ReadValue(qbpp) + 1;

    if (k == 0)
        return highbits;

    return (highbits << k) + STRATEGY::ReadValue(k);
}


template<typename TRAITS, typename STRATEGY>
private void JlsCodec<TRAITS, STRATEGY>::EncodeMappedValue(int k, int mappedError, int limit)
{
    int highbits = mappedError >> k;

    if (highbits < limit - traits.qbpp - 1)
    {
        if (highbits + 1 > 31)
        {
            STRATEGY::AppendToBitStream(0, highbits / 2);
            highbits = highbits - highbits / 2;
        }
        STRATEGY::AppendToBitStream(1, highbits + 1);
        STRATEGY::AppendToBitStream((mappedError & ((1 << k) - 1)), k);
        return;
    }

    if (limit - traits.qbpp > 31)
    {
        STRATEGY::AppendToBitStream(0, 31);
        STRATEGY::AppendToBitStream(1, limit - traits.qbpp - 31);
    }
    else
    {
        STRATEGY::AppendToBitStream(1, limit - traits.qbpp);
    }
    STRATEGY::AppendToBitStream((mappedError - 1) & ((1 << traits.qbpp) - 1), traits.qbpp);
}


// Disable the Microsoft Static Analyzer warning: Potential comparison of a constant with another constant. (false warning, triggered by template construction)
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6326)
#endif

// Sets up a lookup table to "Quantize" sample difference.

template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::InitQuantizationLUT()
{
    // for lossless mode with default parameters, we have precomputed the luts for bitcounts 8, 10, 12 and 16.
    if (traits.NEAR == 0 && traits.MAXVAL == (1 << traits.bpp) - 1)
    {
        const JpegLSPresetCodingParameters presets = ComputeDefault(traits.MAXVAL, traits.NEAR);
        if (presets.Threshold1 == T1 && presets.Threshold2 == T2 && presets.Threshold3 == T3)
        {
            if (traits.bpp == 8)
            {
                _pquant = &rgquant8Ll[rgquant8Ll.size() / 2];
                return;
            }
            if (traits.bpp == 10)
            {
                _pquant = &rgquant10Ll[rgquant10Ll.size() / 2];
                return;
            }
            if (traits.bpp == 12)
            {
                _pquant = &rgquant12Ll[rgquant12Ll.size() / 2];
                return;
            }
            if (traits.bpp == 16)
            {
                _pquant = &rgquant16Ll[rgquant16Ll.size() / 2];
                return;
            }
        }
    }

    const int RANGE = 1 << traits.bpp;

    _rgquant.resize(RANGE * 2);

    _pquant = &_rgquant[RANGE];
    for (int i = -RANGE; i < RANGE; ++i)
    {
        _pquant[i] = QuantizeGratientOrg(i);
    }
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif

template<typename TRAITS, typename STRATEGY>
signed char JlsCodec<TRAITS,STRATEGY>::QuantizeGratientOrg(int Di) const
{
    if (Di <= -T3) return  -4;
    if (Di <= -T2) return  -3;
    if (Di <= -T1) return  -2;
    if (Di < -traits.NEAR)  return  -1;
    if (Di <=  traits.NEAR) return   0;
    if (Di < T1)   return   1;
    if (Di < T2)   return   2;
    if (Di < T3)   return   3;

    return  4;
}


// RI = Run interruption: functions that handle the sample terminating a run.

template<typename TRAITS, typename STRATEGY>
int JlsCodec<TRAITS,STRATEGY>::DecodeRIError(CContextRunMode& ctx)
{
    const int k = ctx.GetGolomb();
    const int EMErrval = DecodeValue(k, traits.LIMIT - J[_RUNindex]-1, traits.qbpp);
    const int Errval = ctx.ComputeErrVal(EMErrval + ctx._nRItype, k);
    ctx.UpdateVariables(Errval, EMErrval);
    return Errval;
}


template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS,STRATEGY>::EncodeRIError(CContextRunMode& ctx, int Errval)
{
    const int k = ctx.GetGolomb();
    const bool map = ctx.ComputeMap(Errval, k);
    const int EMErrval = 2 * std::abs(Errval) - ctx._nRItype - static_cast<int>(map);

    Debug.Assert(Errval == ctx.ComputeErrVal(EMErrval + ctx._nRItype, k));
    EncodeMappedValue(k, EMErrval, traits.LIMIT-J[_RUNindex]-1);
    ctx.UpdateVariables(Errval, EMErrval);
}


template<typename TRAITS, typename STRATEGY>
Triplet<typename TRAITS::SAMPLE> JlsCodec<TRAITS,STRATEGY>::DecodeRIPixel(Triplet<SAMPLE> Ra, Triplet<SAMPLE> Rb)
{
    const int Errval1 = DecodeRIError(_contextRunmode[0]);
    const int Errval2 = DecodeRIError(_contextRunmode[0]);
    const int Errval3 = DecodeRIError(_contextRunmode[0]);

    return Triplet<SAMPLE>(traits.ComputeReconstructedSample(Rb.v1, Errval1 * Sign(Rb.v1  - Ra.v1)),
                           traits.ComputeReconstructedSample(Rb.v2, Errval2 * Sign(Rb.v2  - Ra.v2)),
                           traits.ComputeReconstructedSample(Rb.v3, Errval3 * Sign(Rb.v3  - Ra.v3)));
}


template<typename TRAITS, typename STRATEGY>
Triplet<typename TRAITS::SAMPLE> JlsCodec<TRAITS,STRATEGY>::EncodeRIPixel(Triplet<SAMPLE> x, Triplet<SAMPLE> Ra, Triplet<SAMPLE> Rb)
{
    const int errval1 = traits.ComputeErrVal(Sign(Rb.v1 - Ra.v1) * (x.v1 - Rb.v1));
    EncodeRIError(_contextRunmode[0], errval1);

    const int errval2 = traits.ComputeErrVal(Sign(Rb.v2 - Ra.v2) * (x.v2 - Rb.v2));
    EncodeRIError(_contextRunmode[0], errval2);

    const int errval3 = traits.ComputeErrVal(Sign(Rb.v3 - Ra.v3) * (x.v3 - Rb.v3));
    EncodeRIError(_contextRunmode[0], errval3);

    return Triplet<SAMPLE>(traits.ComputeReconstructedSample(Rb.v1, errval1 * Sign(Rb.v1  - Ra.v1)),
                           traits.ComputeReconstructedSample(Rb.v2, errval2 * Sign(Rb.v2  - Ra.v2)),
                           traits.ComputeReconstructedSample(Rb.v3, errval3 * Sign(Rb.v3  - Ra.v3)));
}


template<typename TRAITS, typename STRATEGY>
typename TRAITS::SAMPLE JlsCodec<TRAITS,STRATEGY>::DecodeRIPixel(int Ra, int Rb)
{
    if (std::abs(Ra - Rb) <= traits.NEAR)
    {
        const int ErrVal = DecodeRIError(_contextRunmode[1]);
        return static_cast<SAMPLE>(traits.ComputeReconstructedSample(Ra, ErrVal));
    }

    const int ErrVal = DecodeRIError(_contextRunmode[0]);
    return static_cast<SAMPLE>(traits.ComputeReconstructedSample(Rb, ErrVal * Sign(Rb - Ra)));
}


template<typename TRAITS, typename STRATEGY>
typename TRAITS::SAMPLE JlsCodec<TRAITS,STRATEGY>::EncodeRIPixel(int x, int Ra, int Rb)
{
    if (std::abs(Ra - Rb) <= traits.NEAR)
    {
        const int ErrVal = traits.ComputeErrVal(x - Ra);
        EncodeRIError(_contextRunmode[1], ErrVal);
        return static_cast<SAMPLE>(traits.ComputeReconstructedSample(Ra, ErrVal));
    }

    const int ErrVal = traits.ComputeErrVal((x - Rb) * Sign(Rb - Ra));
    EncodeRIError(_contextRunmode[0], ErrVal);
    return static_cast<SAMPLE>(traits.ComputeReconstructedSample(Rb, ErrVal * Sign(Rb - Ra)));
}


// RunMode: Functions that handle run-length encoding

template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::EncodeRunPixels(int runLength, bool endOfLine)
{
    while (runLength >= static_cast<int>(1 << J[_RUNindex]))
    {
        STRATEGY::AppendOnesToBitStream(1);
        runLength = runLength - static_cast<int>(1 << J[_RUNindex]);
        IncrementRunIndex();
    }

    if (endOfLine)
    {
        if (runLength != 0)
        {
            STRATEGY::AppendOnesToBitStream(1);
        }
    }
    else
    {
        STRATEGY::AppendToBitStream(runLength, J[_RUNindex] + 1); // leading 0 + actual remaining length
    }
}


template<typename TRAITS, typename STRATEGY>
int JlsCodec<TRAITS, STRATEGY>::DecodeRunPixels(PIXEL Ra, PIXEL* startPos, int cpixelMac)
{
    int index = 0;
    while (STRATEGY::ReadBit())
    {
        const int count = std::min(1 << J[_RUNindex], int(cpixelMac - index));
        index += count;
        Debug.Assert(index <= cpixelMac);

        if (count == (1 << J[_RUNindex]))
        {
            IncrementRunIndex();
        }

        if (index == cpixelMac)
            break;
    }

    if (index != cpixelMac)
    {
        // incomplete run.
        index += (J[_RUNindex] > 0) ? STRATEGY::ReadValue(J[_RUNindex]) : 0;
    }

    if (index > cpixelMac)
        throw charls_error(charls::ApiResult::InvalidCompressedData);

    for (int i = 0; i < index; ++i)
    {
        startPos[i] = Ra;
    }

    return index;
}

template<typename TRAITS, typename STRATEGY>
int JlsCodec<TRAITS, STRATEGY>::DoRunMode(int index, EncoderStrategy*)
{
    const int ctypeRem = _width - index;
    PIXEL* ptypeCurX = _currentLine + index;
    PIXEL* ptypePrevX = _previousLine + index;

    const PIXEL Ra = ptypeCurX[-1];

    int runLength = 0;

    while (traits.IsNear(ptypeCurX[runLength],Ra))
    {
        ptypeCurX[runLength] = Ra;
        runLength++;

        if (runLength == ctypeRem)
            break;
    }

    EncodeRunPixels(runLength, runLength == ctypeRem);

    if (runLength == ctypeRem)
        return runLength;

    ptypeCurX[runLength] = EncodeRIPixel(ptypeCurX[runLength], Ra, ptypePrevX[runLength]);
    DecrementRunIndex();
    return runLength + 1;
}


template<typename TRAITS, typename STRATEGY>
int JlsCodec<TRAITS, STRATEGY>::DoRunMode(int startIndex, DecoderStrategy*)
{
    const PIXEL Ra = _currentLine[startIndex-1];

    const int runLength = DecodeRunPixels(Ra, _currentLine + startIndex, _width - startIndex);
    int endIndex = startIndex + runLength;

    if (endIndex == _width)
        return endIndex - startIndex;

    // run interruption
    const PIXEL Rb = _previousLine[endIndex];
    _currentLine[endIndex] = DecodeRIPixel(Ra, Rb);
    DecrementRunIndex();
    return endIndex - startIndex + 1;
}


// DoLine: Encodes/Decodes a scanline of samples

template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::DoLine(SAMPLE*)
{
    int index = 0;
    int Rb = _previousLine[index-1];
    int Rd = _previousLine[index];

    while (index < _width)
    {
        const int Ra = _currentLine[index -1];
        const int Rc = Rb;
        Rb = Rd;
        Rd = _previousLine[index + 1];

        const int Qs = ComputeContextID(QuantizeGratient(Rd - Rb), QuantizeGratient(Rb - Rc), QuantizeGratient(Rc - Ra));

        if (Qs != 0)
        {
            _currentLine[index] = DoRegular(Qs, _currentLine[index], GetPredictedValue(Ra, Rb, Rc), static_cast<STRATEGY*>(nullptr));
            index++;
        }
        else
        {
            index += DoRunMode(index, static_cast<STRATEGY*>(nullptr));
            Rb = _previousLine[index - 1];
            Rd = _previousLine[index];
        }
    }
}


// DoLine: Encodes/Decodes a scanline of triplets in ILV_SAMPLE mode

template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::DoLine(Triplet<SAMPLE>*)
{
    int index = 0;
    while(index < _width)
    {
        const Triplet<SAMPLE> Ra = _currentLine[index - 1];
        const Triplet<SAMPLE> Rc = _previousLine[index - 1];
        const Triplet<SAMPLE> Rb = _previousLine[index];
        const Triplet<SAMPLE> Rd = _previousLine[index + 1];

        const int Qs1 = ComputeContextID(QuantizeGratient(Rd.v1 - Rb.v1), QuantizeGratient(Rb.v1 - Rc.v1), QuantizeGratient(Rc.v1 - Ra.v1));
        const int Qs2 = ComputeContextID(QuantizeGratient(Rd.v2 - Rb.v2), QuantizeGratient(Rb.v2 - Rc.v2), QuantizeGratient(Rc.v2 - Ra.v2));
        const int Qs3 = ComputeContextID(QuantizeGratient(Rd.v3 - Rb.v3), QuantizeGratient(Rb.v3 - Rc.v3), QuantizeGratient(Rc.v3 - Ra.v3));

        if (Qs1 == 0 && Qs2 == 0 && Qs3 == 0)
        {
            index += DoRunMode(index, static_cast<STRATEGY*>(nullptr));
        }
        else
        {
            Triplet<SAMPLE> Rx;
            Rx.v1 = DoRegular(Qs1, _currentLine[index].v1, GetPredictedValue(Ra.v1, Rb.v1, Rc.v1), static_cast<STRATEGY*>(nullptr));
            Rx.v2 = DoRegular(Qs2, _currentLine[index].v2, GetPredictedValue(Ra.v2, Rb.v2, Rc.v2), static_cast<STRATEGY*>(nullptr));
            Rx.v3 = DoRegular(Qs3, _currentLine[index].v3, GetPredictedValue(Ra.v3, Rb.v3, Rc.v3), static_cast<STRATEGY*>(nullptr));
            _currentLine[index] = Rx;
            index++;
        }
    }
}


// DoScan: Encodes or decodes a scan.
// In ILV_SAMPLE mode, multiple components are handled in DoLine
// In ILV_LINE mode, a call do DoLine is made for every component
// In ILV_NONE mode, DoScan is called for each component

template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::DoScan()
{
    const int pixelstride = _width + 4;
    const int components = Info().interleaveMode == charls::InterleaveMode::Line ? Info().components : 1;

    std::vector<PIXEL> vectmp(2 * components * pixelstride);
    std::vector<int> rgRUNindex(components);

    for (int line = 0; line < Info().height; ++line)
    {
        _previousLine = &vectmp[1];
        _currentLine = &vectmp[1 + components * pixelstride];
        if ((line & 1) == 1)
        {
            std::swap(_previousLine, _currentLine);
        }

        STRATEGY::OnLineBegin(_width, _currentLine, pixelstride);

        for (int component = 0; component < components; ++component)
        {
            _RUNindex = rgRUNindex[component];

            // initialize edge pixels used for prediction
            _previousLine[_width] = _previousLine[_width - 1];
            _currentLine[-1] = _previousLine[0];
            DoLine(static_cast<PIXEL*>(nullptr)); // dummy arg for overload resolution

            rgRUNindex[component] = _RUNindex;
            _previousLine += pixelstride;
            _currentLine += pixelstride;
        }

        if (_rect.Y <= line && line < _rect.Y + _rect.Height)
        {
            STRATEGY::OnLineEnd(_rect.Width, _currentLine + _rect.X - (components * pixelstride), pixelstride);
        }
    }

    STRATEGY::EndScan();
}


// Factory function for ProcessLine objects to copy/transform unencoded pixels to/from our scanline buffers.

template<typename TRAITS, typename STRATEGY>
std::unique_ptr<ProcessLine> JlsCodec<TRAITS, STRATEGY>::CreateProcess(ByteStreamInfo info)
{
    if (!IsInterleaved())
    {
        return info.rawData ?
            std::unique_ptr<ProcessLine>(std::make_unique<PostProcesSingleComponent>(info.rawData, Info(), sizeof(typename TRAITS::PIXEL))) :
            std::unique_ptr<ProcessLine>(std::make_unique<PostProcesSingleStream>(info.rawStream, Info(), sizeof(typename TRAITS::PIXEL)));
    }

    if (Info().colorTransformation == ColorTransformation::None)
        return std::make_unique<ProcessTransformed<TransformNone<typename TRAITS::SAMPLE>>>(info, Info(), TransformNone<SAMPLE>());

    if (Info().bitsPerSample == sizeof(SAMPLE) * 8)
    {
        switch (Info().colorTransformation)
        {
            case ColorTransformation::HP1: return std::make_unique<ProcessTransformed<TransformHp1<SAMPLE>>>(info, Info(), TransformHp1<SAMPLE>());
            case ColorTransformation::HP2: return std::make_unique<ProcessTransformed<TransformHp2<SAMPLE>>>(info, Info(), TransformHp2<SAMPLE>());
            case ColorTransformation::HP3: return std::make_unique<ProcessTransformed<TransformHp3<SAMPLE>>>(info, Info(), TransformHp3<SAMPLE>());
            default:
                std::ostringstream message;
                message << "Color transformation " << Info().colorTransformation << " is not supported.";
                throw charls_error(ApiResult::UnsupportedColorTransform, message.str());
        }
    }

    if (Info().bitsPerSample > 8)
    {
        const int shift = 16 - Info().bitsPerSample;
        switch (Info().colorTransformation)
        {
            case ColorTransformation::HP1: return std::make_unique<ProcessTransformed<TransformShifted<TransformHp1<uint16_t>>>>(info, Info(), TransformShifted<TransformHp1<uint16_t>>(shift));
            case ColorTransformation::HP2: return std::make_unique<ProcessTransformed<TransformShifted<TransformHp2<uint16_t>>>>(info, Info(), TransformShifted<TransformHp2<uint16_t>>(shift));
            case ColorTransformation::HP3: return std::make_unique<ProcessTransformed<TransformShifted<TransformHp3<uint16_t>>>>(info, Info(), TransformShifted<TransformHp3<uint16_t>>(shift));
            default:
                std::ostringstream message;
                message << "Color transformation " << Info().colorTransformation << " is not supported.";
                throw charls_error(ApiResult::UnsupportedColorTransform, message.str());
        }
    }

    throw charls_error(ApiResult::UnsupportedBitDepthForTransform);
}


// Setup codec for encoding and calls DoScan

template<typename TRAITS, typename STRATEGY>
size_t JlsCodec<TRAITS, STRATEGY>::EncodeScan(std::unique_ptr<ProcessLine> processLine, ByteStreamInfo& compressedData)
{
    STRATEGY::_processLine = std::move(processLine);

    STRATEGY::Init(compressedData);
    DoScan();

    return STRATEGY::GetLength();
}

// Setup codec for decoding and calls DoScan


template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::DecodeScan(std::unique_ptr<ProcessLine> processLine, const JlsRect& rect, ByteStreamInfo& compressedData)
{
    STRATEGY::_processLine = std::move(processLine);

    uint8_t* compressedBytes = const_cast<uint8_t*>(static_cast<const uint8_t*>(compressedData.rawData));
    _rect = rect;

    STRATEGY::Init(compressedData);
    DoScan();
    SkipBytes(compressedData, STRATEGY::GetCurBytePos() - compressedBytes);
}


// Initialize the codec data structures. Depends on JPEG-LS parameters like Threshold1-Threshold3.
template<typename TRAITS, typename STRATEGY>
void JlsCodec<TRAITS, STRATEGY>::InitParams(int t1, int t2, int t3, int nReset)
{
    T1 = t1;
    T2 = t2;
    T3 = t3;

    InitQuantizationLUT();

    const int A = std::max(2, (traits.RANGE + 32) / 64);
    for (unsigned int Q = 0; Q < sizeof(_contexts) / sizeof(_contexts[0]); ++Q)
    {
        _contexts[Q] = JlsContext(A);
    }

    _contextRunmode[0] = CContextRunMode(std::max(2, (traits.RANGE + 32) / 64), 0, nReset);
    _contextRunmode[1] = CContextRunMode(std::max(2, (traits.RANGE + 32) / 64), 1, nReset);
    _RUNindex = 0;
}
    }
}
