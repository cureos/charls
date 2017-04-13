// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Diagnostics;

using static CharLS.util;

namespace CharLS
{
    public interface IDecoderStrategy : ICodecStrategy
    {
        void DecodeScan(IProcessLine processLine, JlsRect rect, ByteStreamInfo compressedData);
    }

    public class DecoderStrategy<TSample, TPixel> : JlsCodec<TSample, TPixel>, IDecoderStrategy where TSample : struct
    {
        private const int bufferbits = sizeof(ulong) * 8;

        private IProcessLine _processLine;

        private byte[] _buffer;

        private ByteStreamInfo _byteStream;

        // decoding
        private ulong _readCache;

        private int _validBits;

        private int _position;

        private int _nextFFPosition;

        private int _endPosition;

        public DecoderStrategy(ITraits<TSample, TPixel> inTraits, JlsParameters parameters)
            : base(inTraits, parameters)
        {
            _processLine = null;
            _buffer = null;
            _byteStream = null;
            _readCache = 0;
            _validBits = 0;
            _position = 0;
            _nextFFPosition = 0;
            _endPosition = 0;
        }

        // Setup codec for decoding and calls DoScan
        public virtual void DecodeScan(IProcessLine processLine, JlsRect rect, ByteStreamInfo compressedData)
        {
            _processLine = processLine;

            _rect = rect;

            Init(compressedData);
            DoScan();
            compressedData.Skip(GetCurBytePos());
        }

        protected override void OnLineBegin(int cpixel, Subarray<TPixel> ptypeBuffer, int pixelStride)
        {
        }


        protected override void OnLineEnd(int pixelCount, Subarray<TPixel> ptypeBuffer, int pixelStride)
        {
            _processLine.NewLineDecoded(ptypeBuffer.ToBytes(), pixelCount, pixelStride);
        }

        protected override void Init(ByteStreamInfo compressedStream)
        {
            _validBits = 0;
            _readCache = 0;

            if (compressedStream.IsBuffered)
            {
                _byteStream = null;
                _buffer = compressedStream.Buffer;
                _position = compressedStream.Position;
                _endPosition = _buffer.Length;

            }
            else
            {
                _buffer = new byte[40000];
                _byteStream = compressedStream;
                _position = compressedStream.Position;
                _endPosition = _position;
                AddBytesFromStream();
            }

            _nextFFPosition = FindNextFF();
            MakeValid();
        }

        protected override void EndScan()
        {
            if (_buffer[_position] != 0xFF)
            {
                ReadBit();

                if (_buffer[_position] != 0xFF) throw new charls_error(ApiResult.TooMuchCompressedData);
            }

            if (_readCache != 0) throw new charls_error(ApiResult.TooMuchCompressedData);
        }

        protected override TSample DoRegular(int Qs, int x, int pred)
        {
            int sign = BitWiseSign(Qs);
            JlsContext ctx = _contexts[ApplySign(Qs, sign)];
            int k = ctx.GetGolomb();
            int Px = _traits.CorrectPrediction(pred + ApplySign(ctx.C, sign));

            int ErrVal;
            Code code = decodingTables[k].Get(PeekByte());
            if (code != null && code.GetLength() != 0)
            {
                Skip(code.GetLength());
                ErrVal = code.GetValue();
                Debug.Assert(Math.Abs(ErrVal) < 65535);
            }
            else
            {
                ErrVal = UnMapErrVal(DecodeValue(k, _traits.LIMIT, _traits.qbpp));
                if (Math.Abs(ErrVal) > 65535)
                    throw new charls_error(ApiResult.InvalidCompressedData);
            }
            if (k == 0)
            {
                ErrVal = ErrVal ^ ctx.GetErrorCorrection(_traits.NEAR);
            }
            ctx.UpdateVariables(ErrVal, _traits.NEAR, _traits.RESET);
            ErrVal = ApplySign(ErrVal, sign);
            return _traits.ComputeReconstructedSample(Px, ErrVal);
        }

        protected override int DoRunMode(int startIndex)
        {
            TPixel Ra = _currentLine[startIndex - 1];

            int runLength = DecodeRunPixels(Ra, _currentLine.Copy(startIndex), _width - startIndex);
            int endIndex = startIndex + runLength;

            if (endIndex == _width)
                return endIndex - startIndex;

            // run interruption
            TPixel Rb = _previousLine[endIndex];
            _currentLine[endIndex] = DecodeRIPixel(Ra, Rb);
            DecrementRunIndex();
            return endIndex - startIndex + 1;
        }

        internal int ReadLongValue(int length)
        {
            if (length <= 24) return ReadValue(length);

            return (ReadValue(length - 24) << 24) + ReadValue(24);
        }

        private void AddBytesFromStream()
        {
            if (_byteStream == null /* OR eof */) return;

            int count = _endPosition - _position;

            if (count > 64) return;

            Array.Copy(_buffer, _position, _buffer, 0, count);
            int offset = -_position;

            _position += offset;
            _endPosition += offset;
            _nextFFPosition += offset;

            int readbytes = _byteStream.Read(_buffer, _endPosition, _buffer.Length - count);
            _endPosition += readbytes;
        }

        private void Skip(int length)
        {
            _validBits -= length;
            _readCache = _readCache << length;
        }

        private bool OptimizedRead()
        {
            // Easy & fast: if there is no 0xFF byte in sight, we can read without bitstuffing
            if (_position < _nextFFPosition - (sizeof(ulong) - 1))
            {
                _readCache |= FromBigEndian.Read(sizeof(ulong), new Subarray<byte>(_buffer, _position, sizeof(ulong))) >> _validBits;
                int bytesToRead = (bufferbits - _validBits) >> 3;
                _position += bytesToRead;
                _validBits += bytesToRead * 8;
                Debug.Assert(_validBits >= bufferbits - 8);
                return true;
            }
            return false;
        }

        private void MakeValid()
        {
            Debug.Assert(_validBits <= bufferbits - 8);

            if (OptimizedRead()) return;

            AddBytesFromStream();

            do
            {
                if (_position >= _endPosition)
                {
                    if (_validBits <= 0) throw new charls_error(ApiResult.InvalidCompressedData);

                    return;
                }

                ulong valnew = _buffer[_position];

                if (valnew == 0xFF)
                {
                    // JPEG bitstream rule: no FF may be followed by 0x80 or higher
                    if (_position == _endPosition - 1 || (_buffer[_position + 1] & 0x80) != 0)
                    {
                        if (_validBits <= 0) throw new charls_error(ApiResult.InvalidCompressedData);
                        return;
                    }
                }

                _readCache |= valnew << (bufferbits - 8 - _validBits);
                _position += 1;
                _validBits += 8;

                if (valnew == 0xFF)
                {
                    _validBits--;
                }
            }
            while (_validBits < bufferbits - 8);

            _nextFFPosition = FindNextFF();
        }

        private int FindNextFF()
        {
            var positionNextFF = _position;

            while (positionNextFF < _endPosition)
            {
                if (_buffer[positionNextFF] == 0xFF) break;

                positionNextFF++;
            }

            return positionNextFF;
        }

        private int GetCurBytePos()
        {
            int validBits = _validBits;
            int compressedBytes = _position - 1;

            for (;;)
            {
                int cbitLast = _buffer[compressedBytes] == 0xFF ? 7 : 8;

                if (validBits < cbitLast) return compressedBytes + 1;

                validBits -= cbitLast;
                compressedBytes--;
            }
        }

        private int ReadValue(int length)
        {
            if (_validBits < length)
            {
                MakeValid();
                if (_validBits < length) throw new charls_error(ApiResult.InvalidCompressedData);
            }

            Debug.Assert(length != 0 && length <= _validBits);
            Debug.Assert(length < 32);
            var result = (int)(_readCache >> (bufferbits - length));
            Skip(length);
            return result;
        }

        private ulong PeekByte()
        {
            if (_validBits < 8)
            {
                MakeValid();
            }

            return _readCache >> (bufferbits - 8);
        }

        private bool ReadBit()
        {
            if (_validBits <= 0)
            {
                MakeValid();
            }

            bool bSet = (_readCache & ((ulong)1 << (bufferbits - 1))) != 0;
            Skip(1);
            return bSet;
        }

        private int Peek0Bits()
        {
            if (_validBits < 16)
            {
                MakeValid();
            }
            var valTest = _readCache;

            for (int count = 0; count < 16; count++)
            {
                if ((valTest & ((ulong)1 << (bufferbits - 1))) != 0) return count;

                valTest <<= 1;
            }
            return -1;
        }

        private int ReadHighbits()
        {
            int count = Peek0Bits();
            if (count >= 0)
            {
                Skip(count + 1);
                return count;
            }
            Skip(15);

            for (int highbits = 15;; highbits++)
            {
                if (ReadBit()) return highbits;
            }
        }

        // Encoding/decoding of golomb codes

        private int DecodeValue(int k, int limit, int qbpp)
        {
            int highbits = ReadHighbits();

            if (highbits >= limit - (qbpp + 1))
                return ReadValue(qbpp) + 1;

            if (k == 0)
                return highbits;

            return (highbits << k) + ReadValue(k);
        }

        // RI = Run interruption: functions that handle the sample terminating a run.

        private int DecodeRIError(CContextRunMode ctx)
        {
            int k = ctx.GetGolomb();
            int EMErrval = DecodeValue(k, _traits.LIMIT - J[_RUNindex] - 1, _traits.qbpp);
            int Errval = ctx.ComputeErrVal(EMErrval + ctx._nRItype, k);
            ctx.UpdateVariables(Errval, EMErrval);
            return Errval;
        }

        private TPixel DecodeRIPixel(TPixel Ra, TPixel Rb)
        {
            return
                (TPixel)
                (_pixelIsTriplet
                     ? DecodeRIPixel((ITriplet<TSample>)Ra, (ITriplet<TSample>)Rb)
                     : (object)DecodeRIPixel(Convert.ToInt32(Ra), Convert.ToInt32(Rb)));
        }

        private ITriplet<TSample> DecodeRIPixel(ITriplet<TSample> Ra, ITriplet<TSample> Rb)
        {
            int Errval1 = DecodeRIError(_contextRunmode[0]);
            int Errval2 = DecodeRIError(_contextRunmode[0]);
            int Errval3 = DecodeRIError(_contextRunmode[0]);

            return new Triplet<TSample>(_traits.ComputeReconstructedSample(Rb.v1, Errval1 * Sign(Rb.v1 - Ra.v1)),
                                   _traits.ComputeReconstructedSample(Rb.v2, Errval2 * Sign(Rb.v2 - Ra.v2)),
                                   _traits.ComputeReconstructedSample(Rb.v3, Errval3 * Sign(Rb.v3 - Ra.v3)));
        }


        private TSample DecodeRIPixel(int Ra, int Rb)
        {
            if (Math.Abs(Ra - Rb) <= _traits.NEAR)
            {
                int ErrVal = DecodeRIError(_contextRunmode[1]);
                return _traits.ComputeReconstructedSample(Ra, ErrVal);
            }
            else
            {
                int ErrVal = DecodeRIError(_contextRunmode[0]);
                return _traits.ComputeReconstructedSample(Rb, ErrVal * Sign(Rb - Ra));
            }
        }

        // RunMode: Functions that handle run-length encoding

        private int DecodeRunPixels(TPixel Ra, Subarray<TPixel> pixels, int cpixelMac)
        {
            int index = 0;
            while (ReadBit())
            {
                int count = Math.Min(1 << J[_RUNindex], cpixelMac - index);
                index += count;
                Debug.Assert(index <= cpixelMac);

                if (count == 1 << J[_RUNindex])
                {
                    IncrementRunIndex();
                }

                if (index == cpixelMac)
                    break;
            }

            if (index != cpixelMac)
            {
                // incomplete run.
                index += J[_RUNindex] > 0 ? ReadValue(J[_RUNindex]) : 0;
            }

            if (index > cpixelMac)
                throw new charls_error(ApiResult.InvalidCompressedData);

            for (int i = 0; i < index; ++i)
            {
                pixels[i] = Ra;
            }

            return index;
        }

        private static int UnMapErrVal(int mappedError)
        {
            int sign = (mappedError << (INT32_BITCOUNT - 1)) >> (INT32_BITCOUNT - 1);
            return sign ^ (mappedError >> 1);
        }
    }
}
