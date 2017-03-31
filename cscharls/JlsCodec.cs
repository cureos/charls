// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using static CharLS.util;

namespace CharLS
{
    public abstract class JlsCodec
    {
        protected const int ContextsCount = 365;

        protected const int CContextRunModesCount = 2;

        // As defined in the JPEG-LS standard 

        // used to determine how large runs should be encoded at a time. 
        protected static readonly int[] J =
            {
                0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10,
                11, 12, 13, 14, 15
            };

        // Lookup tables to replace code with lookup tables.

        // Lookup table: decode symbols that are smaller or equal to 8 bit (16 tables for each value of k)
        protected static readonly CTable[] decodingTables =
            {
                InitTable(0), InitTable(1), InitTable(2), InitTable(3),
                InitTable(4), InitTable(5), InitTable(6), InitTable(7),
                InitTable(8), InitTable(9), InitTable(10), InitTable(11),
                InitTable(12), InitTable(13), InitTable(14), InitTable(15)
            };

        // Lookup tables: sample differences to bin indexes. 
        protected static readonly sbyte[] rgquant8Ll = CreateQLutLossless(8);

        protected static readonly sbyte[] rgquant10Ll = CreateQLutLossless(10);

        protected static readonly sbyte[] rgquant12Ll = CreateQLutLossless(12);

        protected static readonly sbyte[] rgquant16Ll = CreateQLutLossless(16);

        protected static int GetMappedErrVal(int Errval)
        {
            int mappedError = (Errval >> (INT32_BITCOUNT - 2)) ^ (2 * Errval);
            return mappedError;
        }

        protected static int Sign(int n)
        {
            return (n >> (INT32_BITCOUNT - 1)) | 1;
        }

        private static CTable InitTable(int k)
        {
            CTable table = new CTable();
            for (short nerr = 0; ; nerr++)
            {
                // Q is not used when k != 0
                int merrval = GetMappedErrVal(nerr);
                var paircode = CreateEncodedValue(k, merrval);
                if (paircode.Key > CTable.cbit) break;

                Code code = new Code(nerr, paircode.Key);
                table.AddEntry((byte)paircode.Value, code);
            }

            for (short nerr = -1; ; nerr--)
            {
                // Q is not used when k != 0
                int merrval = GetMappedErrVal(nerr);
                var paircode = CreateEncodedValue(k, merrval);
                if (paircode.Key > CTable.cbit) break;

                Code code = new Code(nerr, paircode.Key);
                table.AddEntry((byte)paircode.Value, code);
            }

            return table;
        }

        // Functions to build tables used to decode short golomb codes.

        private static KeyValuePair<int, int> CreateEncodedValue(int k, int mappedError)
        {
            int highbits = mappedError >> k;
            return new KeyValuePair<int, int>(highbits + k + 1, (1 << k) | (mappedError & (1 << k) - 1));
        }

        private static sbyte QuantizeGratientOrg(JpegLSPresetCodingParameters preset, int NEAR, int Di)
        {
            if (Di <= -preset.Threshold3) return -4;
            if (Di <= -preset.Threshold2) return -3;
            if (Di <= -preset.Threshold1) return -2;
            if (Di < -NEAR) return -1;
            if (Di <= NEAR) return 0;
            if (Di < preset.Threshold1) return 1;
            if (Di < preset.Threshold2) return 2;
            if (Di < preset.Threshold3) return 3;

            return 4;
        }
        private static sbyte[] CreateQLutLossless(int cbit)
        {
            JpegLSPresetCodingParameters preset = ComputeDefault((1 << cbit) - 1, 0);
            int range = preset.MaximumSampleValue + 1;

            var lut = new sbyte[range * 2];

            for (int diff = -range; diff < range; diff++)
            {
                lut[range + diff] = QuantizeGratientOrg(preset, 0, diff);
            }
            return lut;
        }
    }

    public abstract class JlsCodec<TSample, TPixel> : JlsCodec
        where TSample : struct
    {
        private readonly JlsParameters _params;

        // codec parameters
        protected readonly ITraits<TSample, TPixel> _traits;

        protected readonly bool _isPixelTriplet;

        protected JlsRect _rect;

        protected int _width;

        private int T1;

        private int T2;

        private int T3;

        // compression context
        protected JlsContext[] _contexts = new JlsContext[ContextsCount];

        protected CContextRunMode[] _contextRunmode = new CContextRunMode[CContextRunModesCount];

        protected int _RUNindex;

        protected TPixel[] _previousLine; // previous line ptr

        protected TPixel[] _currentLine; // current line ptr

        // quantization lookup table
        private sbyte[] _pquant;

        protected JlsCodec(ITraits<TSample, TPixel> inTraits, JlsParameters parameters)
        {
            _traits = inTraits;
            _isPixelTriplet = Implements<TPixel, ITriplet<TSample>>();

            _params = parameters;
            _rect = new JlsRect();
            _width = parameters.width;
            T1 = 0;
            T2 = 0;
            T3 = 0;
            _RUNindex = 0;
            _previousLine = null;
            _currentLine = null;
            _pquant = null;
        }

        protected abstract void OnLineBegin(int cpixel, byte[] ptypeBuffer, int pixelStride);

        protected abstract void OnLineEnd(int cpixel, byte[] ptypeBuffer, int pixelStride);

        protected abstract void Init(ByteStreamInfo compressedStream);

        protected abstract void EndScan();

        // Encode/decode a single sample. Performancewise the #1 important functions
        protected abstract TSample DoRegular(int Qs, int x, int pred);

        // RunMode: Functions that handle run-length encoding
        protected abstract int DoRunMode(int index);

        protected int ApplySign(int i, int sign)
        {
            return (sign ^ i) - sign;
        }

        private static int GetPredictedValue(int Ra, int Rb, int Rc)
        {
            // sign trick reduces the number of if statements (branches)
            int sgn = BitWiseSign(Rb - Ra);

            // is Ra between Rc and Rb?
            if ((sgn ^ (Rc - Ra)) < 0)
            {
                return Rb;
            }
            if ((sgn ^ (Rb - Rc)) < 0)
            {
                return Ra;
            }

            // default case, valid if Rc element of [Ra,Rb]
            return Ra + Rb - Rc;
        }

        private static int ComputeContextID(int Q1, int Q2, int Q3)
        {
            return (Q1 * 9 + Q2) * 9 + Q3;
        }

        public void SetPresets(JpegLSPresetCodingParameters presets)
        {
            JpegLSPresetCodingParameters presetDefault = ComputeDefault(_traits.MAXVAL, _traits.NEAR);

            InitParams(
                presets.Threshold1 != 0 ? presets.Threshold1 : presetDefault.Threshold1,
                presets.Threshold2 != 0 ? presets.Threshold2 : presetDefault.Threshold2,
                presets.Threshold3 != 0 ? presets.Threshold3 : presetDefault.Threshold3,
                presets.ResetValue != 0 ? presets.ResetValue : presetDefault.ResetValue);
        }

        private bool IsInterleaved()
        {
            if (_params.interleaveMode == InterleaveMode.None) return false;

            if (_params.components == 1) return false;

            return true;
        }

        protected void IncrementRunIndex()
        {
            _RUNindex = Math.Min(31, _RUNindex + 1);
        }

        protected void DecrementRunIndex()
        {
            _RUNindex = Math.Max(0, _RUNindex - 1);
        }

        // Sets up a lookup table to "Quantize" sample difference.

        private int QuantizeGratient(int Di)
        {
            var RANGE = _pquant.Length / 2;
            Debug.Assert(QuantizeGratientOrg(Di) == _pquant[Di + RANGE]);
            return _pquant[Di + RANGE];
        }

        private void InitQuantizationLUT()
        {
            // for lossless mode with default parameters, we have precomputed the luts for bitcounts 8, 10, 12 and 16.
            if (_traits.NEAR == 0 && _traits.MAXVAL == (1 << _traits.bpp) - 1)
            {
                JpegLSPresetCodingParameters presets = ComputeDefault(_traits.MAXVAL, _traits.NEAR);
                if (presets.Threshold1 == T1 && presets.Threshold2 == T2 && presets.Threshold3 == T3)
                {
                    if (_traits.bpp == 8)
                    {
                        _pquant = rgquant8Ll;
                        return;
                    }
                    if (_traits.bpp == 10)
                    {
                        _pquant = rgquant10Ll;
                        return;
                    }
                    if (_traits.bpp == 12)
                    {
                        _pquant = rgquant12Ll;
                        return;
                    }
                    if (_traits.bpp == 16)
                    {
                        _pquant = rgquant16Ll;
                        return;
                    }
                }
            }

            int RANGE = 1 << _traits.bpp;
            _pquant = new sbyte[RANGE * 2];

            for (int i = -RANGE; i < RANGE; ++i)
            {
                _pquant[i + RANGE] = QuantizeGratientOrg(i);
            }
        }

        private sbyte QuantizeGratientOrg(int Di)
        {
            if (Di <= -T3) return -4;
            if (Di <= -T2) return -3;
            if (Di <= -T1) return -2;
            if (Di < -_traits.NEAR) return -1;
            if (Di <= _traits.NEAR) return 0;
            if (Di < T1) return 1;
            if (Di < T2) return 2;
            if (Di < T3) return 3;

            return 4;
        }

        // DoLine: Encodes/Decodes a scanline of samples

        private void DoLine()
        {
            if (_isPixelTriplet)
            {
                DoTripletLine();
            }
            else
            {
                DoSingletLine();
            }
        }

        private void DoSingletLine()
        {
            int index = 0;
            int Rb = (int)(object)_previousLine[index - 1];
            int Rd = (int)(object)_previousLine[index];

            while (index < _width)
            {
                int Ra = (int)(object)_currentLine[index - 1];
                int Rc = Rb;
                Rb = Rd;
                Rd = (int)(object)_previousLine[index + 1];

                int Qs = ComputeContextID(
                    QuantizeGratient(Rd - Rb),
                    QuantizeGratient(Rb - Rc),
                    QuantizeGratient(Rc - Ra));

                if (Qs != 0)
                {
                    _currentLine[index] =
                        (TPixel)(object)DoRegular(Qs, (int)(object)_currentLine[index], GetPredictedValue(Ra, Rb, Rc));
                    index++;
                }
                else
                {
                    index += DoRunMode(index);
                    Rb = (int)(object)_previousLine[index - 1];
                    Rd = (int)(object)_previousLine[index];
                }
            }
        }


        // DoLine: Encodes/Decodes a scanline of triplets in ILV_TSample mode

        private void DoTripletLine()
        {
            int index = 0;
            while (index < _width)
            {
                var Ra = (ITriplet<TSample>)_currentLine[index - 1];
                var Rc = (ITriplet<TSample>)_previousLine[index - 1];
                var Rb = (ITriplet<TSample>)_previousLine[index];
                var Rd = (ITriplet<TSample>)_previousLine[index + 1];

                int Qs1 = ComputeContextID(
                    QuantizeGratient(Rd.v1 - Rb.v1),
                    QuantizeGratient(Rb.v1 - Rc.v1),
                    QuantizeGratient(Rc.v1 - Ra.v1));
                int Qs2 = ComputeContextID(
                    QuantizeGratient(Rd.v2 - Rb.v2),
                    QuantizeGratient(Rb.v2 - Rc.v2),
                    QuantizeGratient(Rc.v2 - Ra.v2));
                int Qs3 = ComputeContextID(
                    QuantizeGratient(Rd.v3 - Rb.v3),
                    QuantizeGratient(Rb.v3 - Rc.v3),
                    QuantizeGratient(Rc.v3 - Ra.v3));

                if (Qs1 == 0 && Qs2 == 0 && Qs3 == 0)
                {
                    index += DoRunMode(index);
                }
                else
                {
                    var x = (ITriplet<TSample>)_currentLine[index];
                    ITriplet <TSample> Rx =
                        new Triplet<TSample>(
                            DoRegular(Qs1, x.v1, GetPredictedValue(Ra.v1, Rb.v1, Rc.v1)),
                            DoRegular(Qs2, x.v2, GetPredictedValue(Ra.v2, Rb.v2, Rc.v2)),
                            DoRegular(Qs3, x.v3, GetPredictedValue(Ra.v3, Rb.v3, Rc.v3)));
                    _currentLine[index] = (TPixel)Rx;
                    index++;
                }
            }
        }

        // DoScan: Encodes or decodes a scan.
        // In ILV_TSample mode, multiple components are handled in DoLine
        // In ILV_LINE mode, a call do DoLine is made for every component
        // In ILV_NONE mode, DoScan is called for each component
        protected void DoScan()
        {
            int pixelstride = _width + 4;
            int components = _params.interleaveMode == InterleaveMode.Line ? _params.components : 1;

            var vectmp = new List<TPixel>(2 * components * pixelstride);
            var rgRUNindex = new List<int>(components);

            for (int line = 0; line < _params.height; ++line)
            {
                _previousLine = &vectmp[1];
                _currentLine = &vectmp[1 + components * pixelstride];
                if ((line & 1) == 1)
                {
                    std::swap(_previousLine, _currentLine);
                }

                OnLineBegin(_width, _currentLine, pixelstride);

                for (int component = 0; component < components; ++component)
                {
                    _RUNindex = rgRUNindex[component];

                    // initialize edge pixels used for prediction
                    _previousLine[_width] = _previousLine[_width - 1];
                    _currentLine[-1] = _previousLine[0];
                    DoLine(); // dummy arg for overload resolution

                    rgRUNindex[component] = _RUNindex;
                    _previousLine += pixelstride;
                    _currentLine += pixelstride;
                }

                if (_rect.Y <= line && line < _rect.Y + _rect.Height)
                {
                    OnLineEnd(_rect.Width, _currentLine + _rect.X - (components * pixelstride), pixelstride);
                }
            }

            EndScan();
        }

        // Factory function for ProcessLine objects to copy/transform unencoded pixels to/from our scanline buffers.
        public IProcessLine CreateProcess(ByteStreamInfo info)
        {
            if (!IsInterleaved())
            {
                return info.rawData
                           ? std::unique_ptr<ProcessLine>(
                               std::make_unique<PostProcesSingleComponent>(
                                   info.rawData,
                                   _params,
                                   sizeof(typename
                TRAITS::TPixel))) :
                std::unique_ptr<ProcessLine>(
                    std::make_unique<PostProcesSingleStream>(
                        info.rawStream,
                        _params,
                        sizeof(typename
                TRAITS::TPixel)))
                ;
            }

            if (_params.colorTransformation == ColorTransformation.None) return new ProcessTransformed<TSample>(info, _params, new TransformNone<TSample>());

            if (_params.bitsPerSample == Marshal.SizeOf(default(TSample)) * 8)
            {
                switch (_params.colorTransformation)
                {
                    case ColorTransformation.HP1:
                        return new ProcessTransformed<TSample>(info, _params, new TransformHp1<TSample>());
                    case ColorTransformation.HP2:
                        return new ProcessTransformed<TSample>(info, _params, new TransformHp2<TSample>());
                    case ColorTransformation.HP3:
                        return new ProcessTransformed<TSample>(info, _params, new TransformHp3<TSample>());
                    default:
                        var message = $"Color transformation {_params.colorTransformation} is not supported.";
                        throw new charls_error(ApiResult.UnsupportedColorTransform, message);
                }
            }

            if (_params.bitsPerSample > 8)
            {
                int shift = 16 - _params.bitsPerSample;
                switch (_params.colorTransformation)
                {
                    case ColorTransformation.HP1:
                        return new ProcessTransformed<ushort>(
                            info,
                            _params,
                            new TransformShifted<ushort, TransformHp1<ushort>>(shift));
                    case ColorTransformation.HP2:
                        return new ProcessTransformed<ushort>(
                            info,
                            _params,
                            new TransformShifted<ushort, TransformHp2<ushort>>(shift));
                    case ColorTransformation.HP3:
                        return new ProcessTransformed<ushort>(
                            info,
                            _params,
                            new TransformShifted<ushort, TransformHp3<ushort>>(shift));
                    default:
                        var message = $"Color transformation {_params.colorTransformation} is not supported.";
                        throw new charls_error(ApiResult.UnsupportedColorTransform, message);
                }
            }

            throw new charls_error(ApiResult.UnsupportedBitDepthForTransform);
        }

        // Initialize the codec data structures. Depends on JPEG-LS parameters like Threshold1-Threshold3.
        private void InitParams(int t1, int t2, int t3, int nReset)
        {
            T1 = t1;
            T2 = t2;
            T3 = t3;

            InitQuantizationLUT();

            int A = Math.Max(2, (_traits.RANGE + 32) / 64);
            for (uint Q = 0; Q < ContextsCount; ++Q)
            {
                _contexts[Q] = new JlsContext(A);
            }

            _contextRunmode[0] = new CContextRunMode(Math.Max(2, (_traits.RANGE + 32) / 64), 0, nReset);
            _contextRunmode[1] = new CContextRunMode(Math.Max(2, (_traits.RANGE + 32) / 64), 1, nReset);
            _RUNindex = 0;
        }
    }
}
