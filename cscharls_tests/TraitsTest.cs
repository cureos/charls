// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    public class TraitsTest
    {
        [Fact]
        public void TestTraits16bit()
        {
            var traits1 = new DefaultTraitsT<ushort, ushort>(4095, 0);
            var traits2 = new LosslessTraitsT<ushort>(12);

            Assert.True(traits1.LIMIT == traits2.LIMIT);
            Assert.True(traits1.MAXVAL == traits2.MAXVAL);
            Assert.True(traits1.RESET == traits2.RESET);
            Assert.True(traits1.bpp == traits2.bpp);
            Assert.True(traits1.qbpp == traits2.qbpp);

            for (int i = -4096; i < 4096; ++i)
            {
                Assert.True(traits1.ModuloRange(i) == traits2.ModuloRange(i));
                Assert.True(traits1.ComputeErrVal(i) == traits2.ComputeErrVal(i));
            }

            for (int i = -8095; i < 8095; ++i)
            {
                Assert.True(traits1.CorrectPrediction(i) == traits2.CorrectPrediction(i));
                Assert.True(traits1.IsNear(i, 2) == traits2.IsNear(i, 2));
            }
        }

        [Fact]
        public void TestTraits8bit()
        {
            var traits1 = new DefaultTraitsT<byte, byte>(255, 0);
            var traits2 = new LosslessTraitsT<byte>(8);

            Assert.True(traits1.LIMIT == traits2.LIMIT);
            Assert.True(traits1.MAXVAL == traits2.MAXVAL);
            Assert.True(traits1.RESET == traits2.RESET);
            Assert.True(traits1.bpp == traits2.bpp);
            Assert.True(traits1.qbpp == traits2.qbpp);

            for (int i = -255; i < 255; ++i)
            {
                Assert.True(traits1.ModuloRange(i) == traits2.ModuloRange(i));
                Assert.True(traits1.ComputeErrVal(i) == traits2.ComputeErrVal(i));
            }

            for (int i = -255; i < 512; ++i)
            {
                Assert.True(traits1.CorrectPrediction(i) == traits2.CorrectPrediction(i));
                Assert.True(traits1.IsNear(i, 2) == traits2.IsNear(i, 2));
            }
        }
    }
}
