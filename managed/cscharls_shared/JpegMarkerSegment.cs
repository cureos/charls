// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CharLS
{
    public class JpegMarkerSegment : IJpegSegment
    {
        private readonly JpegMarkerCode _markerCode;

        private readonly List<byte> _content;

        private JpegMarkerSegment(JpegMarkerCode markerCode, List<byte> content)
        {
            _markerCode = markerCode;
            _content = content;
        }

        public void Serialize(JpegStreamWriter streamWriter)
        {
            streamWriter.WriteByte(0xFF);
            streamWriter.WriteByte((byte)_markerCode);
            streamWriter.WriteWord((ushort)(_content.Count + 2));
            streamWriter.WriteBytes(_content.ToArray());
        }

        /// <summary>
        /// Creates a JPEG-LS Start Of Frame (SOF-55) segment.
        /// </summary>
        /// <param name="width">The width of the frame.</param>
        /// <param name="height">The height of the frame.</param>
        /// <param name="bitsPerSample">The bits per sample.</param>
        /// <param name="componentCount">The component count.</param>
        public static JpegMarkerSegment CreateStartOfFrameSegment(
            int width,
            int height,
            int bitsPerSample,
            int componentCount)
        {
            Debug.Assert(width >= 0 && width <= ushort.MaxValue);
            Debug.Assert(height >= 0 && height <= ushort.MaxValue);
            Debug.Assert(bitsPerSample > 0 && bitsPerSample <= byte.MaxValue);
            Debug.Assert(componentCount > 0 && componentCount <= byte.MaxValue - 1);

            // Create a Frame Header as defined in T.87, C.2.2 and T.81, B.2.2
            var content = new List<byte>();
            content.Add((byte)bitsPerSample); // P = Sample precision
            Add(content, (ushort)height); // Y = Number of lines
            Add(content, (ushort)width); // X = Number of samples per line

            // Components
            content.Add((byte)componentCount); // Nf = Number of image components in frame
            for (var component = 0; component < componentCount; ++component)
            {
                // Component Specification parameters
                content.Add((byte)(component + 1)); // Ci = Component identifier
                content.Add(0x11); // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
                content.Add(0);
                // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
            }

            return new JpegMarkerSegment(JpegMarkerCode.StartOfFrameJpegLS, content);
        }


        /// <summary>
        /// Creates a JPEG File Interchange (APP1 + jfif) segment.
        /// </summary>
        /// <param name="jfif">Parameters to write into the JFIF segment.</param>
        public static JpegMarkerSegment CreateJpegFileInterchangeFormatSegment(JfifParameters jfif)
        {
            Debug.Assert(jfif.units == 0 || jfif.units == 1 || jfif.units == 2);
            Debug.Assert(jfif.Xdensity > 0);
            Debug.Assert(jfif.Ydensity > 0);
            Debug.Assert(jfif.Xthumbnail >= 0 && jfif.Xthumbnail < 256);
            Debug.Assert(jfif.Ythumbnail >= 0 && jfif.Ythumbnail < 256);

            // Create a JPEG APP0 segment in the JPEG File Interchange Format (JFIF), v1.02
            var content = new List<byte> { (byte)'J', (byte)'F', (byte)'I', (byte)'F', (byte)'\0' };
            Add(content, (ushort)jfif.version);
            content.Add((byte)jfif.units);
            Add(content, (ushort)jfif.Xdensity);
            Add(content, (ushort)jfif.Ydensity);

            // thumbnail
            content.Add((byte)jfif.Xthumbnail);
            content.Add((byte)jfif.Ythumbnail);
            if (jfif.Xthumbnail > 0)
            {
                if (jfif.thumbnail == null)
                    throw new charls_error(
                        ApiResult.InvalidJlsParameters,
                        "jfif.Xthumbnail is > 0 but jfif.thumbnail == null_ptr");
#if NET20
                var thumbnail = new byte[3 * jfif.Xthumbnail * jfif.Ythumbnail];
                Array.Copy(jfif.thumbnail, 0, thumbnail, 0, 3 * jfif.Xthumbnail * jfif.Ythumbnail);
                content.AddRange(jfif.thumbnail);
#else
                content.AddRange(new ArraySegment<byte>(jfif.thumbnail, 0, 3 * jfif.Xthumbnail * jfif.Ythumbnail));
#endif
            }

            return new JpegMarkerSegment(JpegMarkerCode.ApplicationData0, content);
        }


        /// <summary>
        /// Creates a JPEG-LS preset parameters (LSE) segment.
        /// </summary>
        /// <param name="presets">Parameters to write into the JPEG-LS preset segment.</param>
        public static JpegMarkerSegment CreateJpegLSPresetParametersSegment(JpegLSPresetCodingParameters presets)
        {
            var content = new List<byte>();

            // Parameter ID. 0x01 = JPEG-LS preset coding parameters.
            content.Add(1);

            Add(content, (ushort)presets.MaximumSampleValue);
            Add(content, (ushort)presets.Threshold1);
            Add(content, (ushort)presets.Threshold2);
            Add(content, (ushort)presets.Threshold3);
            Add(content, (ushort)presets.ResetValue);

            return new JpegMarkerSegment(JpegMarkerCode.JpegLSPresetParameters, content);
        }


        /// <summary>
        /// Creates a color transformation (APP8) segment.
        /// </summary>
        /// <param name="transformation">Parameters to write into the JFIF segment.</param>
        public static JpegMarkerSegment CreateColorTransformSegment(ColorTransformation transformation)
        {
            return new JpegMarkerSegment(
                JpegMarkerCode.ApplicationData8,
                new List<byte> { (byte)'m', (byte)'r', (byte)'f', (byte)'x', (byte)transformation });
        }


        /// <summary>
        /// Creates a JPEG-LS Start Of Scan (SOS) segment.
        /// </summary>
        /// <param name="componentIndex">The component index of the scan segment or the start index if component count > 1.</param>
        /// <param name="componentCount">The number of components in the scan segment. Can only be > 1 when the components are interleaved.</param>
        /// <param name="allowedLossyError">The allowed lossy error. 0 means lossless.</param>
        /// <param name="interleaveMode">The interleave mode of the components.</param>
        public static JpegMarkerSegment CreateStartOfScanSegment(
            int componentIndex,
            int componentCount,
            int allowedLossyError,
            InterleaveMode interleaveMode)
        {
            Debug.Assert(componentIndex >= 0);
            Debug.Assert(componentCount > 0);

            // Create a Scan Header as defined in T.87, C.2.3 and T.81, B.2.3
            var content = new List<byte>();

            content.Add((byte)componentCount);
            for (var i = 0; i < componentCount; ++i)
            {
                content.Add((byte)(componentIndex + i));
                content.Add(0); // Mapping table selector (0 = no table)
            }

            content.Add((byte)allowedLossyError); // NEAR parameter
            content.Add((byte)interleaveMode); // ILV parameter
            content.Add(0); // transformation

            return new JpegMarkerSegment(JpegMarkerCode.StartOfScan, content);
        }

        private static void Add(IList<byte> values, ushort value)
        {
            values.Add((byte)(value / 0x100));
            values.Add((byte)(value % 0x100));
        }
    }
}
