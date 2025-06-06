using System;
using NumEncoding.Base;

namespace NumEncoding.Base
{
    /// <summary>
    /// Represents a compression that is specifically used by a data block.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class DataBlockCompression : Attribute
    {
        /// <summary>
        /// Gets the number of bytes that this compression uses when the data entry is compressed as a P-frame.
        /// </summary>
        public abstract int PFrameByteLength { get; }

        /// <summary>
        /// Gets the compressor to be used for this compression, where the first parameter is the object from the last data entry, the second parameter is the object from the current data entry, and the return value is the encoded byte array.
        /// </summary>
        public abstract Func<object, object, byte[]> Compressor { get; }

        /// <summary>
        /// Gets the decompressor to be used for this compression, where the first parameter is the object from the last data entry, the second parameter is the encoded byte array, and the return value is the original object from the current data entry.
        /// </summary>
        public abstract Func<object, byte[], object> Decompressor { get; }
    }
}

namespace NumEncoding.DataBlockCompressions
{
    /// <summary>
    /// Represents a compression that only stores data in I-frame data entries.
    /// </summary>
    public class IFrameOnlyCompression : DataBlockCompression
    {
        public override int PFrameByteLength { get; } = 0;
        public override Func<object, object, byte[]> Compressor { get; } = (last, current) => [];
        public override Func<object, byte[], object> Decompressor { get; } = (last, current) => last;
    }

    /// <summary>
    /// Represents a compression that only stores the changes in data for P-frame data entries, which allows for a different type than the provided objects and supports <see cref="byte"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, and <see cref="float"/> types.
    /// </summary>
    /// <typeparam name="TOriginal">The type of the original I-frame data being stored, which is used for conversion purposes.</typeparam>
    /// <typeparam name="TDelta">The type of the P-frame data being stored as changes, which does not have to match the type of the provided objects as type conversion is always applied.</typeparam>
    public class NumericDeltaCompression<TOriginal, TDelta> : DataBlockCompression
    {
        public override int PFrameByteLength { get; } = default(TDelta) switch
        {
            byte => sizeof(byte),
            sbyte => sizeof(sbyte),
            short => sizeof(short),
            ushort => sizeof(ushort),
            int => sizeof(int),
            uint => sizeof(uint),
            long => sizeof(long),
            ulong => sizeof(ulong),
            double => sizeof(double),
            float => sizeof(float),
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };

        public override Func<object, object, byte[]> Compressor { get; } = default(TDelta) switch
        {
            byte => (last, current) => [Convert.ToByte(Difference<TOriginal>(last, current))],
            sbyte => (last, current) => [(byte)Convert.ToSByte(Difference<TOriginal>(last, current))],
            short => (last, current) => BitConverter.GetBytes(Convert.ToInt16(Difference<TOriginal>(last, current))),
            ushort => (last, current) => BitConverter.GetBytes(Convert.ToUInt16(Difference<TOriginal>(last, current))),
            int => (last, current) => BitConverter.GetBytes(Convert.ToInt32(Difference<TOriginal>(last, current))),
            uint => (last, current) => BitConverter.GetBytes(Convert.ToUInt32(Difference<TOriginal>(last, current))),
            long => (last, current) => BitConverter.GetBytes(Convert.ToInt64(Difference<TOriginal>(last, current))),
            ulong => (last, current) => BitConverter.GetBytes(Convert.ToUInt64(Difference<TOriginal>(last, current))),
            double => (last, current) => BitConverter.GetBytes(Convert.ToDouble(Difference<TOriginal>(last, current))),
            float => (last, current) => BitConverter.GetBytes(Convert.ToSingle(Difference<TOriginal>(last, current))),
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };

        public override Func<object, byte[], object> Decompressor { get; } = default(TOriginal) switch
        {
            byte => (last, current) => (byte)(Convert.ToByte(last) + Convert.ToByte(current)),
            sbyte => (last, current) => (sbyte)(Convert.ToSByte(last) + Convert.ToSByte(current)),
            short => (last, current) => (short)(Convert.ToInt16(last) + BitConverter.ToInt16(current)),
            ushort => (last, current) => (ushort)(Convert.ToUInt16(last) + BitConverter.ToUInt16(current)),
            int => (last, current) => Convert.ToInt32(last) + BitConverter.ToInt32(current),
            uint => (last, current) => Convert.ToUInt32(last) + BitConverter.ToUInt32(current),
            long => (last, current) => Convert.ToInt64(last) + BitConverter.ToInt64(current),
            ulong => (last, current) => Convert.ToUInt64(last) + BitConverter.ToUInt64(current),
            double => (last, current) => Convert.ToDouble(last) + BitConverter.ToDouble(current),
            float => (last, current) => Convert.ToSingle(last) + BitConverter.ToSingle(current),
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };

        /// <summary>
        /// Gets the difference between two numeric objects.
        /// </summary>
        /// <typeparam name="T">The type of the objects.</typeparam>
        /// <param name="last">The last object.</param>
        /// <param name="current">The current object.</param>
        /// <returns>The difference.</returns>
        public static object Difference<T>(object last, object current) => default(T) switch
        {
            byte => (byte)last - (byte)current,
            sbyte => (sbyte)last - (sbyte)current,
            short => (short)last - (short)current,
            ushort => (ushort)last - (ushort)current,
            int => (int)last - (int)current,
            uint => (uint)last - (uint)current,
            long => (long)last - (long)current,
            ulong => (ulong)last - (ulong)current,
            double => (double)last - (double)current,
            float => (float)last - (float)current,
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };
    }
}
