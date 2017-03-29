// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Diagnostics;
using System.IO;

namespace CharLS
{
    public class DecoderStrategy<TSample> : IStrategy<TSample> where TSample : struct
    {
        private const int bufferbits = sizeof(int) * 8;

        internal readonly JlsParameters _params;

        internal IProcessLine _processLine;

        private byte[] _buffer;

        private Stream _byteStream;

        // decoding
        private int _readCache;

        private int _validBits;

        private int _position;

        private int _nextFFPosition;

        private int _endPosition;

        public DecoderStrategy()
        {
            _params = null;
            _processLine = null;
            _buffer = null;
            _byteStream = null;
            _readCache = 0;
            _validBits = 0;
            _position = 0;
            _nextFFPosition = 0;
            _endPosition = 0;
        }

        public JlsParameters Parameters { get; set; }

        public void Init(ByteStreamInfo compressedStream)
        {
            _validBits = 0;
            _readCache = 0;

            if (compressedStream.rawStream)
            {
                _buffer = new byte[40000];
                _position = 0;
                _endPosition = _position;
                _byteStream = compressedStream.rawStream;
                AddBytesFromStream();
            }
            else
            {
                _byteStream = nullptr;
                _position = compressedStream.rawData;
                _endPosition = _position + compressedStream.count;
            }

            _nextFFPosition = FindNextFF();
            MakeValid();
        }

        public void AddBytesFromStream()
        {
            if (_byteStream == null /* OR eof */) return;

            int count = _endPosition - _position;

            if (count > 64) return;

            for (int i = 0; i < count; ++i)
            {
                _buffer[i] = _position[i];
            }
            int offset = _buffer.data() - _position;

            _position += offset;
            _endPosition += offset;
            _nextFFPosition += offset;

            int readbytes = _byteStream->sgetn(reinterpret_cast<char*>(_endPosition), _buffer.size() - count);
            _endPosition += readbytes;
        }

        public void Skip(int length)
        {
            _validBits -= length;
            _readCache = _readCache << length;
        }


        public void OnLineBegin(int cpixel, byte[] ptypeBuffer, int pixelStride)
        {
        }


        public void OnLineEnd(int pixelCount, byte[] ptypeBuffer, int pixelStride)
        {
            _processLine.NewLineDecoded(ptypeBuffer, 0, pixelStride, pixelCount);
        }

        public void EndScan()
        {
            if ((*_position) != 0xFF)
            {
                ReadBit();

                if ((*_position) != 0xFF) throw new charls_error(ApiResult.TooMuchCompressedData);
            }

            if (_readCache != 0) throw new charls_error(ApiResult.TooMuchCompressedData);
        }

        public TSample DoRegular(int Qs, int x, int pred)
        {
            const int sign = BitWiseSign(Qs);
            JlsContext & ctx = _contexts[ApplySign(Qs, sign)];
            const int k = ctx.GetGolomb();
            const int Px = traits.CorrectPrediction(pred + ApplySign(ctx.C, sign));

            int ErrVal;
            const Code&code = decodingTables[k].Get(STRATEGY::PeekByte());
            if (code.GetLength() != 0)
            {
                Skip(code.GetLength());
                ErrVal = code.GetValue();
                Debug.Assert(std::abs(ErrVal) < 65535);
            }
            else
            {
                ErrVal = UnMapErrVal(DecodeValue(k, traits.LIMIT, traits.qbpp));
                if (std::abs(ErrVal) > 65535)
                    throw charls_error(charls::ApiResult::InvalidCompressedData);
            }
            if (k == 0)
            {
                ErrVal = ErrVal ^ ctx.GetErrorCorrection(traits.NEAR);
            }
            ctx.UpdateVariables(ErrVal, traits.NEAR, traits.RESET);
            ErrVal = ApplySign(ErrVal, sign);
            return traits.ComputeReconstructedSample(Px, ErrVal);
        }

        public bool OptimizedRead()
        {
            // Easy & fast: if there is no 0xFF byte in sight, we can read without bitstuffing
            if (_position < _nextFFPosition - (sizeof(int) - 1))
            {
                _readCache |= FromBigEndian.Read(sizeof(int), _position) >> _validBits;
                int bytesToRead = (bufferbits - _validBits) >> 3;
                _position += bytesToRead;
                _validBits += bytesToRead * 8;
                Debug.Assert(_validBits >= bufferbits - 8);
                return true;
            }
            return false;
        }


        public void MakeValid()
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

                int valnew = _position[0];

                if (valnew == 0xFF)
                {
                    // JPEG bitstream rule: no FF may be followed by 0x80 or higher
                    if (_position == _endPosition - 1 || (_position[1] & 0x80) != 0)
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

        public int FindNextFF()
        {
            var positionNextFF = _position;

            while (positionNextFF < _endPosition)
            {
                if (*positionNextFF == 0xFF) break;

                positionNextFF++;
            }

            return positionNextFF;
        }

        public int GetCurBytePos()
        {
            int validBits = _validBits;
            uint8_t* compressedBytes = _position;

            for (;;)
            {
                const int cbitLast = compressedBytes[-1] == 0xFF ? 7 : 8;

                if (validBits < cbitLast) return compressedBytes;

                validBits -= cbitLast;
                compressedBytes--;
            }
        }

        public int ReadValue(int length)
        {
            if (_validBits < length)
            {
                MakeValid();
                if (_validBits < length) throw new charls_error(ApiResult.InvalidCompressedData);
            }

            Debug.Assert(length != 0 && length <= _validBits);
            Debug.Assert(length < 32);
            int result = _readCache >> (bufferbits - length);
            Skip(length);
            return result;
        }

        public int PeekByte()
        {
            if (_validBits < 8)
            {
                MakeValid();
            }

            return _readCache >> (bufferbits - 8);
        }

        public bool ReadBit()
        {
            if (_validBits <= 0)
            {
                MakeValid();
            }

            bool bSet = (_readCache & (1 << (bufferbits - 1))) != 0;
            Skip(1);
            return bSet;
        }

        public int Peek0Bits()
        {
            if (_validBits < 16)
            {
                MakeValid();
            }
            int valTest = _readCache;

            for (int count = 0; count < 16; count++)
            {
                if ((valTest & (1 << (bufferbits - 1))) != 0) return count;

                valTest <<= 1;
            }
            return -1;
        }

        public int ReadHighbits()
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

        public int ReadLongValue(int length)
        {
            if (length <= 24) return ReadValue(length);

            return (ReadValue(length - 24) << 24) + ReadValue(24);
        }
    }
}
