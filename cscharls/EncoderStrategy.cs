// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;
using System.IO;

using static CharLS.util;

namespace CharLS
{
    public class EncoderStrategy<TSample, TPixel> : JlsCodec<TSample, TPixel>, IEncoderStrategy where TSample : struct
    {
        private DecoderStrategy<TSample, TPixel> _qdecoder;

        private IProcessLine _processLine;

        private uint _bitBuffer;

        private int _freeBitCount;

        private int _compressedLength;

        // encoding
        private int _position;

        private bool _isFFWritten;

        private int _bytesWritten;

        private byte[] _buffer;

        Stream _compressedStream;

        public EncoderStrategy(ITraits<TSample, TPixel> inTraits, JlsParameters parameters)
            : base(inTraits, parameters)
        {
            _bitBuffer = 0;
            _freeBitCount = sizeof(uint) * 8;
            _compressedLength = 0;
            _position = 0;
            _isFFWritten = false;
            _bytesWritten = 0;
            _buffer = null;
            _compressedStream = null;
        }

        // Setup codec for encoding and calls DoScan
        public int EncodeScan(IProcessLine processLine, ByteStreamInfo compressedData)
        {
            _processLine = processLine;

            Init(compressedData);
            DoScan();

            return GetLength();
        }

        protected override void OnLineBegin(int cpixel, byte[] ptypeBuffer, int pixelStride)
        {
            _processLine.NewLineRequested(ptypeBuffer, 0, pixelStride, cpixel);
        }

        protected override void OnLineEnd(int cpixel, byte[] ptypeBuffer, int pixelStride)
        {
        }

        protected override void Init(ByteStreamInfo compressedStream)
        {
            _freeBitCount = sizeof(uint) * 8;
            _bitBuffer = 0;

            if (compressedStream.rawStream)
            {
                _compressedStream = compressedStream.rawStream;
                _buffer.resize(4000);
                _position = _buffer.data();
                _compressedLength = _buffer.size();
            }
            else
            {
                _position = compressedStream.rawData;
                _compressedLength = compressedStream.count;
            }
        }

        protected override void EndScan()
        {
            Flush();

            // if a 0xff was written, Flush() will force one unset bit anyway
            if (_isFFWritten) AppendToBitStream(0, (_freeBitCount - 1) % 8);
            else AppendToBitStream(0, _freeBitCount % 8);

            Flush();
            Debug.Assert(_freeBitCount == 0x20);

            if (_compressedStream != null)
            {
                OverFlow();
            }
        }

        protected override TSample DoRegular(int Qs, int x, int pred)
        {
            int sign = BitWiseSign(Qs);
            JlsContext ctx = _contexts[ApplySign(Qs, sign)];
            int k = ctx.GetGolomb();
            int Px = traits.CorrectPrediction(pred + ApplySign(ctx.C, sign));
            int ErrVal = traits.ComputeErrVal(ApplySign(x - Px, sign));

            EncodeMappedValue(k, GetMappedErrVal(ctx.GetErrorCorrection(k | traits.NEAR) ^ ErrVal), traits.LIMIT);
            ctx.UpdateVariables(ErrVal, traits.NEAR, traits.RESET);
            Debug.Assert(traits.IsNear(traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign)), x));
            return traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign));
        }

        protected override int DoRunMode(int index)
        {
            const int ctypeRem = _width - index;
            TPixel* ptypeCurX = _currentLine + index;
            TPixel* ptypePrevX = _previousLine + index;

            const TPixel Ra = ptypeCurX[-1];

            int runLength = 0;

            while (traits.IsNear(ptypeCurX[runLength], Ra))
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

        // Encoding of golomb codes
        private void EncodeMappedValue(int k, int mappedError, int limit)
        {
            int highbits = mappedError >> k;

            if (highbits < limit - traits.qbpp - 1)
            {
                if (highbits + 1 > 31)
                {
                    AppendToBitStream(0, highbits / 2);
                    highbits = highbits - highbits / 2;
                }
                AppendToBitStream(1, highbits + 1);
                AppendToBitStream((mappedError & ((1 << k) - 1)), k);
                return;
            }

            if (limit - traits.qbpp > 31)
            {
                AppendToBitStream(0, 31);
                AppendToBitStream(1, limit - traits.qbpp - 31);
            }
            else
            {
                AppendToBitStream(1, limit - traits.qbpp);
            }
            AppendToBitStream((mappedError - 1) & ((1 << traits.qbpp) - 1), traits.qbpp);
        }

        private void AppendToBitStream(int bits, int bitCount)
        {
            Debug.Assert(bitCount < 32 && bitCount >= 0);
            Debug.Assert(
                _qdecoder == null || bitCount == 0 && bits == 0 || _qdecoder.ReadLongValue(bitCount) == bits);
#if !DEBUG
        var mask = (1u << (bitCount)) - 1;
        Debug.Assert((bits | mask) == mask); // Not used bits must be set to zero.
#endif

            _freeBitCount -= bitCount;
            if (_freeBitCount >= 0)
            {
                _bitBuffer |= (uint)bits << _freeBitCount;
            }
            else
            {
                // Add as much bits in the remaining space as possible and flush.
                _bitBuffer |= (uint)bits >> -_freeBitCount;
                Flush();

                // A second flush may be required if extra marker detect bits were needed and not all bits could be written.
                if (_freeBitCount < 0)
                {
                    _bitBuffer |= (uint)bits >> -_freeBitCount;
                    Flush();
                }

                Debug.Assert(_freeBitCount >= 0);
                _bitBuffer |= (uint)bits << _freeBitCount;
            }
        }

        private void OverFlow()
        {
            if (_compressedStream == null) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            int bytesCount = _position - _buffer.data();
            int bytesWritten =
                static_cast<std::size_t>(
                    _compressedStream->sputn(reinterpret_cast<char*>(_buffer.data()), _position - _buffer.data()));

            if (bytesWritten != bytesCount) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            _position = _buffer.data();
            _compressedLength = _buffer.size();
        }

        private void Flush()
        {
            if (_compressedLength < 4)
            {
                OverFlow();
            }

            for (int i = 0; i < 4; ++i)
            {
                if (_freeBitCount >= 32) break;

                if (_isFFWritten)
                {
                    // JPEG-LS requirement (T.87, A.1) to detect markers: after a xFF value a single 0 bit needs to be inserted.
                    *_position = static_cast<uint8_t>(_bitBuffer >> 25);
                    _bitBuffer = _bitBuffer << 7;
                    _freeBitCount += 7;
                }
                else
                {
                    *_position = static_cast<uint8_t>(_bitBuffer >> 24);
                    _bitBuffer = _bitBuffer << 8;
                    _freeBitCount += 8;
                }

                _isFFWritten = *_position == 0xFF;
                _position++;
                _compressedLength--;
                _bytesWritten++;
            }
        }

        private int GetLength()
        {
            return _bytesWritten - (_freeBitCount - 32) / 8;
        }

        private void AppendOnesToBitStream(int length)
        {
            AppendToBitStream((1 << length) - 1, length);
        }

        // RI = Run interruption: functions that handle the sample terminating a run.

        private void EncodeRIError(CContextRunMode ctx, int Errval)
        {
            int k = ctx.GetGolomb();
            bool map = ctx.ComputeMap(Errval, k);
            int EMErrval = 2 * Math.Abs(Errval) - ctx._nRItype - (map ? 1 : 0);

            Debug.Assert(Errval == ctx.ComputeErrVal(EMErrval + ctx._nRItype, k));
            EncodeMappedValue(k, EMErrval, traits.LIMIT - J[_RUNindex] - 1);
            ctx.UpdateVariables(Errval, EMErrval);
        }


        private ITriplet<TSample> EncodeRIPixel(Triplet<TSample> x, Triplet<TSample> Ra, Triplet<TSample> Rb)
        {
            int errval1 = traits.ComputeErrVal(Sign(Rb.v1 - Ra.v1) * (x.v1 - Rb.v1));
            EncodeRIError(_contextRunmode[0], errval1);

            int errval2 = traits.ComputeErrVal(Sign(Rb.v2 - Ra.v2) * (x.v2 - Rb.v2));
            EncodeRIError(_contextRunmode[0], errval2);

            int errval3 = traits.ComputeErrVal(Sign(Rb.v3 - Ra.v3) * (x.v3 - Rb.v3));
            EncodeRIError(_contextRunmode[0], errval3);

            return new Triplet<TSample>(traits.ComputeReconstructedSample(Rb.v1, errval1 * Sign(Rb.v1 - Ra.v1)),
                                   traits.ComputeReconstructedSample(Rb.v2, errval2 * Sign(Rb.v2 - Ra.v2)),
                                   traits.ComputeReconstructedSample(Rb.v3, errval3 * Sign(Rb.v3 - Ra.v3)));
        }

        private TSample EncodeRIPixel(int x, int Ra, int Rb)
        {
            if (Math.Abs(Ra - Rb) <= traits.NEAR)
            {
                int ErrVal = traits.ComputeErrVal(x - Ra);
                EncodeRIError(_contextRunmode[1], ErrVal);
                return traits.ComputeReconstructedSample(Ra, ErrVal);
            }
            else
            {
                int ErrVal = traits.ComputeErrVal((x - Rb) * Sign(Rb - Ra));
                EncodeRIError(_contextRunmode[0], ErrVal);
                return traits.ComputeReconstructedSample(Rb, ErrVal * Sign(Rb - Ra));
            }
        }

        // RunMode: Functions that handle run-length encoding

        private void EncodeRunPixels(int runLength, bool endOfLine)
        {
            while (runLength >= 1 << J[_RUNindex])
            {
                AppendOnesToBitStream(1);
                runLength = runLength - 1 << J[_RUNindex];
                IncrementRunIndex();
            }

            if (endOfLine)
            {
                if (runLength != 0)
                {
                    AppendOnesToBitStream(1);
                }
            }
            else
            {
                AppendToBitStream(runLength, J[_RUNindex] + 1); // leading 0 + actual remaining length
            }
        }
    }
}
