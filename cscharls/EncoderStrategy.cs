// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using static CharLS.util;

namespace CharLS
{
    public interface IEncoderStrategy : ICodecStrategy
    {
        int EncodeScan(IProcessLine processLine, ByteStreamInfo compressedData);
    }

    public sealed class EncoderStrategy<TSample, TPixel> : JlsCodec<TSample, TPixel>, IEncoderStrategy where TSample : struct
    {
        private readonly int _sizeOfPixel = Marshal.SizeOf(default(TPixel));

        private DecoderStrategy<TSample, TPixel> _qdecoder; // TODO Skip?

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

        protected override void OnLineBegin(int cpixel, Subarray<TPixel> ptypeBuffer, int pixelStride)
        {
            var bytes = new byte[cpixel * _sizeOfPixel];
            _processLine.NewLineRequested(bytes, pixelStride, cpixel);
            ptypeBuffer.FromBytes(bytes);
        }

        protected override void OnLineEnd(int cpixel, Subarray<TPixel> ptypeBuffer, int pixelStride)
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
            int Px = _traits.CorrectPrediction(pred + ApplySign(ctx.C, sign));
            int ErrVal = _traits.ComputeErrVal(ApplySign(x - Px, sign));

            EncodeMappedValue(k, GetMappedErrVal(ctx.GetErrorCorrection(k | _traits.NEAR) ^ ErrVal), _traits.LIMIT);
            ctx.UpdateVariables(ErrVal, _traits.NEAR, _traits.RESET);
            Debug.Assert(_traits.IsNear((int)(object)_traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign)), x));
            return _traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign));
        }

        protected override int DoRunMode(int index)
        {
            int ctypeRem = _width - index;

            TPixel Ra = _currentLine[index - 1];

            int runLength = 0;

            while (_traits.IsNear(_currentLine[index + runLength], Ra))
            {
                _currentLine[index + runLength] = Ra;
                runLength++;

                if (runLength == ctypeRem)
                    break;
            }

            EncodeRunPixels(runLength, runLength == ctypeRem);

            if (runLength == ctypeRem)
                return runLength;

            _currentLine[index + runLength] = EncodeRIPixel(_currentLine[index + runLength], Ra, _previousLine[index + runLength]);
            DecrementRunIndex();
            return runLength + 1;
        }

        // Encoding of golomb codes
        private void EncodeMappedValue(int k, int mappedError, int limit)
        {
            int highbits = mappedError >> k;

            if (highbits < limit - _traits.qbpp - 1)
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

            if (limit - _traits.qbpp > 31)
            {
                AppendToBitStream(0, 31);
                AppendToBitStream(1, limit - _traits.qbpp - 31);
            }
            else
            {
                AppendToBitStream(1, limit - _traits.qbpp);
            }
            AppendToBitStream((mappedError - 1) & ((1 << _traits.qbpp) - 1), _traits.qbpp);
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
            EncodeMappedValue(k, EMErrval, _traits.LIMIT - J[_RUNindex] - 1);
            ctx.UpdateVariables(Errval, EMErrval);
        }

        private TPixel EncodeRIPixel(TPixel x, TPixel Ra, TPixel Rb)
        {
            return
                (TPixel)
                (_isPixelTriplet
                     ? EncodeRIPixel((ITriplet<TSample>)x, (ITriplet<TSample>)Ra, (ITriplet<TSample>)Rb)
                     : (object)EncodeRIPixel((int)(object)x, (int)(object)Ra, (int)(object)Rb));
        }

        private ITriplet<TSample> EncodeRIPixel(ITriplet<TSample> x, ITriplet<TSample> Ra, ITriplet<TSample> Rb)
        {
            int errval1 = _traits.ComputeErrVal(Sign(Rb.v1 - Ra.v1) * (x.v1 - Rb.v1));
            EncodeRIError(_contextRunmode[0], errval1);

            int errval2 = _traits.ComputeErrVal(Sign(Rb.v2 - Ra.v2) * (x.v2 - Rb.v2));
            EncodeRIError(_contextRunmode[0], errval2);

            int errval3 = _traits.ComputeErrVal(Sign(Rb.v3 - Ra.v3) * (x.v3 - Rb.v3));
            EncodeRIError(_contextRunmode[0], errval3);

            return new Triplet<TSample>(_traits.ComputeReconstructedSample(Rb.v1, errval1 * Sign(Rb.v1 - Ra.v1)),
                                   _traits.ComputeReconstructedSample(Rb.v2, errval2 * Sign(Rb.v2 - Ra.v2)),
                                   _traits.ComputeReconstructedSample(Rb.v3, errval3 * Sign(Rb.v3 - Ra.v3)));
        }

        private TSample EncodeRIPixel(int x, int Ra, int Rb)
        {
            if (Math.Abs(Ra - Rb) <= _traits.NEAR)
            {
                int ErrVal = _traits.ComputeErrVal(x - Ra);
                EncodeRIError(_contextRunmode[1], ErrVal);
                return _traits.ComputeReconstructedSample(Ra, ErrVal);
            }
            else
            {
                int ErrVal = _traits.ComputeErrVal((x - Rb) * Sign(Rb - Ra));
                EncodeRIError(_contextRunmode[0], ErrVal);
                return _traits.ComputeReconstructedSample(Rb, ErrVal * Sign(Rb - Ra));
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
