// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Linq;

namespace CharLS
{
    public class JpegStreamReader
    {
        private static readonly byte[] jfifID = { (byte)'J', (byte)'F', (byte)'I', (byte)'F', (byte)'\0' };

        private readonly ByteStreamInfo _byteStream;

        private JlsParameters _params;

        private JlsRect _rect;

        public JpegStreamReader(ByteStreamInfo byteStreamInfo)
        {
            _byteStream = byteStreamInfo;
        }

        public JlsParameters GetMetadata()
        {
            return _params;
        }

        public JpegLSPresetCodingParameters GetCustomPreset()
        {
            return _params.custom;
        }

        public void Read(ByteStreamInfo rawPixels)
        {
            ReadHeader();

            var result = CheckParameterCoherent(_params);
            if (result != ApiResult.OK) throw new charls_error(result);

            if (_rect.Width <= 0)
            {
                _rect.Width = _params.width;
                _rect.Height = _params.height;
            }

            var bytesPerPlane = _rect.Width * _rect.Height * ((_params.bitsPerSample + 7) / 8);

            if (!rawPixels.Require(true, bytesPerPlane * _params.components)) throw new charls_error(ApiResult.UncompressedBufferTooSmall);

            int componentIndex = 0;

            while (componentIndex < _params.components)
            {
                ReadStartOfScan(componentIndex == 0);

                var currentPosition = rawPixels.Position;
                var qcodec = JlsCodecFactory<IDecoderStrategy>.GetCodec(_params, _params.custom);
                var processLine = qcodec.CreateProcess(rawPixels);
                qcodec.DecodeScan(processLine, _rect, _byteStream);
                rawPixels.Position = currentPosition + bytesPerPlane;

                if (_params.interleaveMode != InterleaveMode.None) return;

                componentIndex += 1;
            }
        }

        public void ReadHeader()
        {
            if (ReadNextMarker() != JpegMarkerCode.StartOfImage) throw new charls_error(ApiResult.InvalidCompressedData);

            for (;;)
            {
                JpegMarkerCode marker = ReadNextMarker();
                if (marker == JpegMarkerCode.StartOfScan) return;

                int cbyteMarker = ReadWord();
                int bytesRead = ReadMarker(marker) + 2;

                int paddingToRead = cbyteMarker - bytesRead;
                if (paddingToRead < 0) throw new charls_error(ApiResult.InvalidCompressedData);

                for (int i = 0; i < paddingToRead; ++i)
                {
                    ReadByte();
                }
            }
        }

        public void SetInfo(JlsParameters parameters)
        {
            _params = parameters;
        }

        public void SetRect(JlsRect rect)
        {
            _rect = rect;
        }

        public void ReadStartOfScan(bool firstComponent)
        {
            if (!firstComponent)
            {
                if (ReadByte() != 0xFF) throw new charls_error(ApiResult.MissingJpegMarkerStart);
                if ((JpegMarkerCode)ReadByte() != JpegMarkerCode.StartOfScan) throw new charls_error(ApiResult.InvalidCompressedData); // TODO: throw more specific error code.
            }
            int length = ReadWord(); // TODO: do something with 'length' or remove it.

            int componentCount = ReadByte();
            if (componentCount != 1 && componentCount != _params.components) throw new charls_error(ApiResult.ParameterValueNotSupported);

            for (int i = 0; i < componentCount; ++i)
            {
                ReadByte();
                ReadByte();
            }
            _params.allowedLossyError = ReadByte();
            _params.interleaveMode = (InterleaveMode)ReadByte();
            if (
                !(_params.interleaveMode == InterleaveMode.None || _params.interleaveMode == InterleaveMode.Line
                  || _params.interleaveMode == InterleaveMode.Sample)) throw new charls_error(ApiResult.InvalidCompressedData); // TODO: throw more specific error code.
            if (ReadByte() != 0) throw new charls_error(ApiResult.InvalidCompressedData); // TODO: throw more specific error code.

            if (_params.stride == 0)
            {
                int width = _rect.Width != 0 ? _rect.Width : _params.width;
                int components = _params.interleaveMode == InterleaveMode.None ? 1 : _params.components;
                _params.stride = components * width * ((_params.bitsPerSample + 7) / 8);
            }
        }

        public byte ReadByte()
        {
            if (!_byteStream.Require(true, 1)) throw new charls_error(ApiResult.CompressedBufferTooSmall);
            return _byteStream.ReadByte();
        }

        private JpegMarkerCode ReadNextMarker()
        {
            var marker = ReadByte();
            if (marker != 0xFF)
            {
                string message = $"Expected JPEG Marker start byte 0xFF but the byte value was 0x{marker:X2}";
                throw new charls_error(ApiResult.MissingJpegMarkerStart, message);
            }

            // Read all preceding 0xFF fill values until a non 0xFF value has been found. (see T.81, B.1.1.2)
            do
            {
                marker = ReadByte();
            }
            while (marker == 0xFF);

            return (JpegMarkerCode)marker;
        }

        private int ReadPresetParameters()
        {
            var type = ReadByte();

            switch (type)
            {
                case 1:
                    {
                        _params.custom.MaximumSampleValue = ReadWord();
                        _params.custom.Threshold1 = ReadWord();
                        _params.custom.Threshold2 = ReadWord();
                        _params.custom.Threshold3 = ReadWord();
                        _params.custom.ResetValue = ReadWord();
                        return 11;
                    }
            }

            return 1;
        }

        private int ReadComment()
        {
            return 0;
        }

        private int ReadStartOfFrame()
        {
            _params.bitsPerSample = ReadByte();
            _params.height = ReadWord();
            _params.width = ReadWord();
            _params.components = ReadByte();
            return 6;
        }

        private ushort ReadWord()
        {
            var i = ReadByte() * 256;
            return (ushort)(i + ReadByte());
        }

        private byte[] ReadBytes(int byteCount)
        {
            if (!_byteStream.Require(true, byteCount)) throw new charls_error(ApiResult.CompressedBufferTooSmall);

            var bytes = new byte[byteCount];
            _byteStream.Read(bytes, 0, byteCount);
            return bytes;
        }

        private int ReadMarker(JpegMarkerCode marker)
        {
            // ISO/IEC 14495-1, ITU-T Recommendation T.87, C.1.1. defines the following markers valid for a JPEG-LS byte stream:
            // SOF55, LSE, SOI, EOI, SOS, DNL, DRI, RSTm, APPn, COM.
            // All other markers shall not be present.
            switch (marker)
            {
                case JpegMarkerCode.StartOfFrameJpegLS:
                    return ReadStartOfFrame();

                case JpegMarkerCode.Comment:
                    return ReadComment();

                case JpegMarkerCode.JpegLSPresetParameters:
                    return ReadPresetParameters();

                case JpegMarkerCode.ApplicationData0:
                    return 0;

                case JpegMarkerCode.ApplicationData7:
                    return ReadColorSpace();

                case JpegMarkerCode.ApplicationData8:
                    return ReadColorXForm();

                case JpegMarkerCode.StartOfFrameBaselineJpeg:
                case JpegMarkerCode.StartOfFrameExtendedSequential:
                case JpegMarkerCode.StartOfFrameProgressive:
                case JpegMarkerCode.StartOfFrameLossless:
                case JpegMarkerCode.StartOfFrameDifferentialSequential:
                case JpegMarkerCode.StartOfFrameDifferentialProgressive:
                case JpegMarkerCode.StartOfFrameDifferentialLossless:
                case JpegMarkerCode.StartOfFrameExtendedArithemtic:
                case JpegMarkerCode.StartOfFrameProgressiveArithemtic:
                case JpegMarkerCode.StartOfFrameLosslessArithemtic:
                    {
                        string message = $"JPEG encoding with marker {marker} is not supported.";
                        throw new charls_error(ApiResult.UnsupportedEncoding, message);
                    }

                // Other tags not supported (among which DNL DRI)
                default:
                    {
                        var message = $"Unknown JPEG marker {marker} encountered.";
                        throw new charls_error(ApiResult.UnknownJpegMarker, message);
                    }
            }
        }


        private void ReadJfif()
        {
            for (int i = 0; i < jfifID.Length; i++)
            {
                if (jfifID[i] != ReadByte()) return;
            }
            _params.jfif.version = ReadWord();

            // DPI or DPcm
            _params.jfif.units = ReadByte();
            _params.jfif.Xdensity = ReadWord();
            _params.jfif.Ydensity = ReadWord();

            // thumbnail
            _params.jfif.Xthumbnail = ReadByte();
            _params.jfif.Ythumbnail = ReadByte();
            if (_params.jfif.Xthumbnail > 0 && _params.jfif.thumbnail != null)
            {
                ReadBytes(3 * _params.jfif.Xthumbnail * _params.jfif.Ythumbnail);
            }
        }


        // Color Transform Application Markers & Code Stream (HP extension)
        private int ReadColorSpace()
        {
            return 0;
        }

        private int ReadColorXForm()
        {
            var sourceTag = ReadBytes(4);
            if (!string.Equals(StringConvert(sourceTag), "mrfx")) return 4;

            var xform = ReadByte();
            switch (xform)
            {
                case (byte)ColorTransformation.None:
                case (byte)ColorTransformation.HP1:
                case (byte)ColorTransformation.HP2:
                case (byte)ColorTransformation.HP3:
                    _params.colorTransformation = (ColorTransformation)xform;
                    return 5;

                case 4: // RgbAsYuvLossy (The standard lossy RGB to YCbCr transform used in JPEG.)
                case 5: // Matrix (transformation is controlled using a matrix that is also stored in the segment.
                    throw new charls_error(ApiResult.ImageTypeNotSupported);
                default:
                    throw new charls_error(ApiResult.InvalidCompressedData);
            }
        }

        private static ApiResult CheckParameterCoherent(JlsParameters parameters)
        {
            if (parameters.bitsPerSample < 2 || parameters.bitsPerSample > 16) return ApiResult.ParameterValueNotSupported;

            if (parameters.interleaveMode < InterleaveMode.None || parameters.interleaveMode > InterleaveMode.Sample) return ApiResult.InvalidCompressedData;

            switch (parameters.components)
            {
                case 4:
                    return parameters.interleaveMode == InterleaveMode.Sample
                               ? ApiResult.ParameterValueNotSupported
                               : ApiResult.OK;
                case 3:
                    return ApiResult.OK;
                case 0:
                    return ApiResult.InvalidJlsParameters;

                default:
                    return parameters.interleaveMode != InterleaveMode.None
                               ? ApiResult.ParameterValueNotSupported
                               : ApiResult.OK;
            }
        }

        private static string StringConvert(byte[] bytes)
        {
            return string.Join("", bytes.Select(b => (char)b));
        }
    }
}
