// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Diagnostics;
using System.IO;

namespace CharLS
{
    public class EncoderStrategy<TSample> : IStrategy<TSample> where TSample : struct
    {
        internal DecoderStrategy<TSample> _qdecoder;

        internal JlsParameters _params;

        internal IProcessLine _processLine;

        private uint _bitBuffer;

        private int _freeBitCount;

        private int _compressedLength;

        // encoding
        private int _position;

        private bool _isFFWritten;

        private int _bytesWritten;

        private byte[] _buffer;

        Stream _compressedStream;

        public EncoderStrategy()
        {
            _params = null;
            _bitBuffer = 0;
            _freeBitCount = sizeof(uint) * 8;
            _compressedLength = 0;
            _position = 0;
            _isFFWritten = false;
            _bytesWritten = 0;
            _buffer = null;
            _compressedStream = null;
        }

        public JlsParameters Parameters { get; set; }

        public void OnLineBegin(int cpixel, byte[] ptypeBuffer, int pixelStride)
        {
            _processLine.NewLineRequested(ptypeBuffer, 0, pixelStride, cpixel);
        }

        public void OnLineEnd(int cpixel, byte[] ptypeBuffer, int pixelStride)
        {
        }

        public void Init(ByteStreamInfo compressedStream)
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

        public void EndScan()
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

        public TSample DoRegular(int Qs, int x, int pred)
        {
            const int sign = BitWiseSign(Qs);
            JlsContext & ctx = _contexts[ApplySign(Qs, sign)];
            const int k = ctx.GetGolomb();
            const int Px = traits.CorrectPrediction(pred + ApplySign(ctx.C, sign));
            const int ErrVal = traits.ComputeErrVal(ApplySign(x - Px, sign));

            EncodeMappedValue(k, GetMappedErrVal(ctx.GetErrorCorrection(k | traits.NEAR) ^ ErrVal), traits.LIMIT);
            ctx.UpdateVariables(ErrVal, traits.NEAR, traits.RESET);
            Debug.Assert(traits.IsNear(traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign)), x));
            return static_cast<SAMPLE>(traits.ComputeReconstructedSample(Px, ApplySign(ErrVal, sign)));
        }

        internal void AppendToBitStream(int bits, int bitCount)
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

        internal void OverFlow()
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

        internal void Flush()
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

        internal int GetLength()
        {
            return _bytesWritten - (_freeBitCount - 32) / 8;
        }

        internal void AppendOnesToBitStream(int length)
        {
            AppendToBitStream((1 << length) - 1, length);
        }
    }
}
