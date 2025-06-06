using System;
using NumEncoding.DataStructureInfo;
using NumEncoding.DataBlocks;
using NumEncoding.DataBlockCompressions;

namespace NumEncoding.Test
{
    [DataStructureVersion(1)]
    [DataStructureCompressionInfo(IFrameInterval = 10)]
    public class DrawingData(double x, double y, byte thickness)
    {
        public DrawingData() : this(0, 0, 1) { }

        [NumericDataBlock<byte>]
        public byte X { get; set; } = (byte)Math.Clamp(x, byte.MinValue, byte.MaxValue);

        [NumericDataBlock<byte>]
        public byte Y { get; set; } = (byte)Math.Clamp(y, byte.MinValue, byte.MaxValue);

        [NumericDataBlock<byte>]
        [IFrameOnlyCompression]
        public byte Thickness { get; set; } = thickness;
    }
}
