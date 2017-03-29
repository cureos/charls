﻿// Copyright (c) 2017 cscharls contributors.
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

        private int _lastComponentIndex;

        private IList<IJpegSegment> _segments;

        public JpegStreamWriter()
        {
            _data = null;
            _lastComponentIndex = 0;
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
            _lastComponentIndex += 1;
            int componentCount = parameters.interleaveMode == InterleaveMode.None ? 1 : parameters.components;
            AddSegment(
                JpegMarkerSegment.CreateStartOfScanSegment(
                    _lastComponentIndex,
                    componentCount,
                    parameters.allowedLossyError,
                    parameters.interleaveMode));

            AddSegment(new JpegImageDataSegment(info, parameters, componentCount));
        }

        public void AddColorTransform(ColorTransformation transformation)
        {
            AddSegment(JpegMarkerSegment.CreateColorTransformSegment(transformation));
        }

        public int GetBytesWritten()
        {
            return _data.Position;
        }

        public int GetLength()
        {
            return _data.Count - _data.Position;
        }

        public int Write(ByteStreamInfo info)
        {
            _data = info;

            WriteMarker(JpegMarkerCode.StartOfImage);

            for (var i = 0; i < _segments.Count; ++i)
            {
                _segments[i].Serialize(this);
            }

            WriteMarker(JpegMarkerCode.EndOfImage);

            return _data.Position;
        }

        internal int GetPos()
        {
            return _data.Position;
        }

        internal ByteStreamInfo OutputStream()
        {
            return _data;
        }

        internal void WriteByte(byte val)
        {
            if (!_data.Require(1)) throw new charls_error(ApiResult.CompressedBufferTooSmall);
            _data.WriteByte(val);
        }

        internal void WriteBytes(byte[] bytes)
        {
            if (!_data.Require(bytes.Length)) throw new charls_error(ApiResult.CompressedBufferTooSmall);
            _data.WriteBytes(bytes);
        }

        internal void WriteWord(ushort value)
        {
            if (!_data.Require(2)) throw new charls_error(ApiResult.CompressedBufferTooSmall);
            _data.WriteByte((byte)(value / 0x100));
            _data.WriteByte((byte)(value % 0x100));
        }

        internal void WriteMarker(JpegMarkerCode marker)
        {
            WriteByte(0xFF);
            WriteByte((byte)marker);
        }

        internal void Seek(int byteCount)
        {
            _data.Seek(byteCount);
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
