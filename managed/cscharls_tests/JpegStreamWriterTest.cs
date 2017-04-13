// Copyright (c) 2017 cscharls contributors.
// Licensed under the BSD-3 license.

using Xunit;

namespace CharLS
{
    public class JpegStreamWriterTest
    {
        [Fact]
        public void LengthWillbeZeroAfterCreate()
        {
            var writer = new JpegStreamWriter();
            Assert.Equal((ulong)0, writer.GetLength());
        }
    }
}
