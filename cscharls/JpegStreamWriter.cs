// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;

using static CharLS.util;

namespace CharLS
{
    //
    // Purpose: 'Writer'class that can generate JPEG-LS file streams.
    //
    public class JpegStreamWriter
    {
        private ByteStreamInfo _data;

        private long _byteOffset;

        private int _lastCompenentIndex;

        private IList<IJpegSegment> _segments;

        public JpegStreamWriter()
        {
            _data = null;
            _byteOffset = 0;
            _lastCompenentIndex = 0;
            _segments = new List<IJpegSegment>();
        }

        public void AddSegment(IJpegSegment segment)
        {
            _segments.Add(segment);
        }

        public void AddScan(ByteStreamInfo info, JlsParameters parameters)
        {
            if (!IsDefault(parameters.custom))
            {
                AddSegment(JpegMarkerSegment.CreateJpegLSPresetParametersSegment(parameters.custom));
            }
            else if (parameters.bitsPerSample > 12)
            {
                JpegLSPresetCodingParameters preset = ComputeDefault(
                    (1 << parameters.bitsPerSample) - 1,
                    parameters.allowedLossyError);
                AddSegment(JpegMarkerSegment.CreateJpegLSPresetParametersSegment(preset));
            }

            // Note: it is a common practice to start to count components by index 1.
            _lastCompenentIndex += 1;
            int componentCount = parameters.interleaveMode == InterleaveMode.None ? 1 : parameters.components;
            AddSegment(
                JpegMarkerSegment.CreateStartOfScanSegment(
                    _lastCompenentIndex,
                    componentCount,
                    parameters.allowedLossyError,
                    parameters.interleaveMode));

            AddSegment(new JpegImageDataSegment(info, parameters, componentCount));
        }

        public void AddColorTransform(ColorTransformation transformation)
        {
            AddSegment(JpegMarkerSegment.CreateColorTransformSegment(transformation));
        }

        public long GetBytesWritten()
        {
            return _byteOffset;
        }

        public long GetLength()
        {
            return _data.count - _byteOffset;
        }

        public long Write(ByteStreamInfo info)
        {
            _data = info;

            WriteMarker(JpegMarkerCode.StartOfImage);

            for (var i = 0; i < _segments.Count; ++i)
            {
                _segments[i].Serialize(this);
            }

            WriteMarker(JpegMarkerCode.EndOfImage);

            return _byteOffset;
        }

        internal byte GetPos()
        {
            return _data.rawData.Array[_byteOffset];
        }

        internal ByteStreamInfo OutputStream()
        {
            ByteStreamInfo data = _data;
            data.Seek(_byteOffset);
            return data;
        }

        internal void WriteByte(byte val)
        {
            if (_data.rawStream != null)
            {
                _data.rawStream.WriteByte(val);
            }
            else
            {
                if (_byteOffset >= _data.count) throw new charls_error(ApiResult.CompressedBufferTooSmall);

                _data.rawData.Array[_byteOffset++] = val;
            }
        }

        internal void WriteBytes(byte[] bytes)
        {
            if (_data.rawStream != null)
            {
                _data.rawStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                var length = bytes.Length;
                if (length > _data.count) throw new charls_error(ApiResult.CompressedBufferTooSmall);

                Array.Copy(bytes, 0, _data.rawData.Array, (int)_byteOffset, length);
                _byteOffset += length;
            }
        }

        internal void WriteWord(ushort value)
        {
            WriteByte((byte)(value / 0x100));
            WriteByte((byte)(value % 0x100));
        }

        internal void WriteMarker(JpegMarkerCode marker)
        {
            WriteByte(0xFF);
            WriteByte((byte)marker);
        }

        internal void Seek(long byteCount)
        {
            if (_data.rawStream != null) return;

            _byteOffset += byteCount;
        }

        private static bool IsDefault(JpegLSPresetCodingParameters custom)
        {
            if (custom.MaximumSampleValue != 0) return false;

            if (custom.Threshold1 != 0) return false;

            if (custom.Threshold2 != 0) return false;

            if (custom.Threshold3 != 0) return false;

            if (custom.ResetValue != 0) return false;

            return true;
        }
    }
}
