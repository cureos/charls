// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System.Collections.Generic;

namespace CharLS
{
    public class JpegStreamReader
    {
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

        public void Read(ByteStreamInfo info)
        {
            ReadHeader();

            const auto result = CheckParameterCoherent(_params);
            if (result != ApiResult.OK)
                throw charls_error(result);

            if (_rect.Width <= 0)
            {
                _rect.Width = _params.width;
                _rect.Height = _params.height;
            }

            const int64_t bytesPerPlane = static_cast<int64_t>(_rect.Width) * _rect.Height * ((_params.bitsPerSample + 7) / 8);

            if (rawPixels.rawData && static_cast<int64_t>(rawPixels.count) < bytesPerPlane * _params.components)
                throw charls_error(ApiResult.UncompressedBufferTooSmall);

            int componentIndex = 0;

            while (componentIndex < _params.components)
            {
                ReadStartOfScan(componentIndex == 0);

                unique_ptr<DecoderStrategy> qcodec = JlsCodecFactory<DecoderStrategy>().GetCodec(_params, _params.custom);
                unique_ptr<ProcessLine> processLine(qcodec->CreateProcess(rawPixels));
                qcodec->DecodeScan(move(processLine), _rect, _byteStream);
                SkipBytes(rawPixels, static_cast<size_t>(bytesPerPlane));

                if (_params.interleaveMode != InterleaveMode.None)
                    return;

                componentIndex += 1;
            }
        }

        public void ReadHeader()
        {
            if (ReadNextMarker() != JpegMarkerCode.StartOfImage)
                throw charls_error(ApiResult.InvalidCompressedData);

            for (;;)
            {
                const JpegMarkerCode marker = ReadNextMarker();
                if (marker == JpegMarkerCode.StartOfScan)
                    return;

                const int32_t cbyteMarker = ReadWord();
                const int bytesRead = ReadMarker(marker) + 2;

                const int paddingToRead = cbyteMarker - bytesRead;
                if (paddingToRead < 0)
                    throw charls_error(ApiResult.InvalidCompressedData);

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
                if (ReadByte() != 0xFF)
                    throw charls_error(ApiResult.MissingJpegMarkerStart);
                if (static_cast<JpegMarkerCode>(ReadByte()) != JpegMarkerCode.StartOfScan)
                    throw charls_error(ApiResult.InvalidCompressedData);// TODO: throw more specific error code.
            }
            int length = ReadByte();
            length = length * 256 + ReadByte(); // TODO: do something with 'length' or remove it.

            const int componentCount = ReadByte();
            if (componentCount != 1 && componentCount != _params.components)
                throw charls_error(ApiResult.ParameterValueNotSupported);

            for (int i = 0; i < componentCount; ++i)
            {
                ReadByte();
                ReadByte();
            }
            _params.allowedLossyError = ReadByte();
            _params.interleaveMode = static_cast<InterleaveMode>(ReadByte());
            if (!(_params.interleaveMode == InterleaveMode.None || _params.interleaveMode == InterleaveMode.Line || _params.interleaveMode == InterleaveMode.Sample))
                throw charls_error(ApiResult.InvalidCompressedData);// TODO: throw more specific error code.
            if (ReadByte() != 0)
                throw charls_error(ApiResult.InvalidCompressedData);// TODO: throw more specific error code.

            if (_params.stride == 0)
            {
                const int width = _rect.Width != 0 ? _rect.Width : _params.width;
                const int components = _params.interleaveMode == InterleaveMode.None ? 1 : _params.components;
                _params.stride = components * width * ((_params.bitsPerSample + 7) / 8);
            }
        }

        public byte ReadByte()
        {
            if (_byteStream.rawStream)
                return static_cast<uint8_t>(_byteStream.rawStream->sbumpc());

            if (_byteStream.count == 0)
                throw charls_error(ApiResult.CompressedBufferTooSmall);

            const uint8_t value = _byteStream.rawData[0];
            SkipBytes(_byteStream, 1);
            return value;
        }

        private JpegMarkerCode ReadNextMarker()
        {
            auto byte = ReadByte();
            if (byte != 0xFF)
            {
                ostringstream message;
                message << setfill('0');
                message << "Expected JPEG Marker start byte 0xFF but the byte value was 0x" << hex << uppercase << setw(2) << static_cast < unsigned int> (byte);
                throw charls_error(ApiResult.MissingJpegMarkerStart, message.str());
            }

            // Read all preceding 0xFF fill values until a non 0xFF value has been found. (see T.81, B.1.1.2)
            do
            {
                byte = ReadByte();
            } while (byte == 0xFF);

            return static_cast<JpegMarkerCode>(byte);
        }

        private int ReadPresetParameters()
        {
            const int type = ReadByte();

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

        private int ReadWord()
        {
            const int i = ReadByte() * 256;
            return i + ReadByte();
        }

        private void ReadNBytes(IList<byte> dst, int byteCount)
        {
            for (int i = 0; i < byteCount; ++i)
            {
                dst.push_back(static_cast<char>(ReadByte()));
            }
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
                        ostringstream message;
                        message << "JPEG encoding with marker " << static_cast < unsigned int> (marker) << " is not supported.";
                        throw charls_error(ApiResult.UnsupportedEncoding, message.str());
                    }

                // Other tags not supported (among which DNL DRI)
                default:
                    {
                        ostringstream message;
                        message << "Unknown JPEG marker " << static_cast < unsigned int> (marker) << " encountered.";
                        throw charls_error(ApiResult.UnknownJpegMarker, message.str());
                    }
            }
        }


        private void ReadJfif()
        {
            for (int i = 0; i < static_cast<int>(sizeof(jfifID)); i++)
            {
                if (jfifID[i] != ReadByte())
                    return;
            }
            _params.jfif.version = ReadWord();

            // DPI or DPcm
            _params.jfif.units = ReadByte();
            _params.jfif.Xdensity = ReadWord();
            _params.jfif.Ydensity = ReadWord();

            // thumbnail
            _params.jfif.Xthumbnail = ReadByte();
            _params.jfif.Ythumbnail = ReadByte();
            if (_params.jfif.Xthumbnail > 0 && _params.jfif.thumbnail)
            {
                vector<char> tempbuff(static_cast<char*>(_params.jfif.thumbnail),
                    static_cast<char*>(_params.jfif.thumbnail)+3 * _params.jfif.Xthumbnail * _params.jfif.Ythumbnail);
                ReadNBytes(tempbuff, 3 * _params.jfif.Xthumbnail * _params.jfif.Ythumbnail);
            }
        }


        // Color Transform Application Markers & Code Stream (HP extension)
        private int ReadColorSpace()
        {
            return 0;
        }

        private int ReadColorXForm()
        {
            vector<char> sourceTag;
            ReadNBytes(sourceTag, 4);

            if (strncmp(sourceTag.data(), "mrfx", 4) != 0)
                return 4;

            const auto xform = ReadByte();
            switch (xform)
            {
                case static_cast<uint8_t>(ColorTransformation.None):
                case static_cast<uint8_t>(ColorTransformation.HP1):
                case static_cast<uint8_t>(ColorTransformation.HP2):
                case static_cast<uint8_t>(ColorTransformation.HP3):
                    _params.colorTransformation = static_cast<ColorTransformation>(xform);
                    return 5;

                case 4: // RgbAsYuvLossy (The standard lossy RGB to YCbCr transform used in JPEG.)
                case 5: // Matrix (transformation is controlled using a matrix that is also stored in the segment.
                    throw new charls_error(ApiResult.ImageTypeNotSupported);
                default:
                    throw new charls_error(ApiResult.InvalidCompressedData);
            }
        }

    }
}
