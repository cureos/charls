// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using System;
using System.Reflection;

using static CharLS.util;

namespace CharLS
{
    public static class JlsCodecFactory<TStrategy>
        where TStrategy : ICodecStrategy
    {
        public static TStrategy GetCodec(JlsParameters parameters, JpegLSPresetCodingParameters presets)
        {
            ICodecStrategy strategy;

            if (presets.ResetValue != 0 && presets.ResetValue != BASIC_RESET)
            {
                var traits = new DefaultTraitsT<byte, byte>(
                                 (1 << parameters.bitsPerSample) - 1,
                                 parameters.allowedLossyError,
                                 presets.ResetValue) { MAXVAL = presets.MaximumSampleValue };
                strategy = CreateCodec(traits, parameters);
            }
            else
            {
                strategy = GetCodecImpl(parameters);
            }

            if (strategy != null)
            {
                strategy.SetPresets(presets);
            }

            return (TStrategy)strategy;
        }

        private static ICodecStrategy CreateCodec<TSample, TPixel>(ITraits<TSample, TPixel> t, JlsParameters parameters)
            where TSample : struct
        {
            if (typeof(IDecoderStrategy).GetTypeInfo().IsAssignableFrom(typeof(TStrategy).GetTypeInfo()))
            {
                return new DecoderStrategy<TSample, TPixel>(t, parameters);
            }

            if (typeof(IEncoderStrategy).GetTypeInfo().IsAssignableFrom(typeof(TStrategy).GetTypeInfo()))
            {
                return new EncoderStrategy<TSample, TPixel>(t, parameters);
            }

            return null;
        }


        private static ICodecStrategy GetCodecImpl(JlsParameters parameters)
        {
            if (parameters.interleaveMode == InterleaveMode.Sample && parameters.components != 3) return null;

            // optimized lossless versions common formats
            if (parameters.allowedLossyError == 0)
            {
                if (parameters.interleaveMode == InterleaveMode.Sample)
                {
                    if (parameters.bitsPerSample == 8) return CreateCodec(new LosslessTraits8(), parameters);
                }
                else
                {
                    switch (parameters.bitsPerSample)
                    {
                        case 8:
                            return CreateCodec(new LosslessTraits8(), parameters);
                        case 12:
                            return CreateCodec(new LosslessTraitsT<ushort>(12), parameters);
                        case 16:
                            return CreateCodec(new LosslessTraits16(), parameters);
                        default:
                            break;
                    }
                }
            }

            int maxval = (1 << parameters.bitsPerSample) - 1;

            if (parameters.bitsPerSample <= 8)
            {
                if (parameters.interleaveMode == InterleaveMode.Sample)
                    return CreateCodec(
                        new DefaultTraitsT<byte, Triplet<byte>>(maxval, parameters.allowedLossyError),
                        parameters);

                return
                    CreateCodec(
                        new DefaultTraitsT<byte, byte>(
                            (1 << parameters.bitsPerSample) - 1,
                            parameters.allowedLossyError),
                        parameters);
            }

            if (parameters.bitsPerSample <= 16)
            {
                if (parameters.interleaveMode == InterleaveMode.Sample)
                    return CreateCodec(
                        new DefaultTraitsT<ushort, Triplet<ushort>>(maxval, parameters.allowedLossyError),
                        parameters);

                return CreateCodec(new DefaultTraitsT<ushort, ushort>(maxval, parameters.allowedLossyError), parameters);
            }

            return null;
        }
    }
}
