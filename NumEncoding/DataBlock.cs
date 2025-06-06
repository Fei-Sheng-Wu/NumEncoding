using System;
using System.Text;
using System.Runtime.CompilerServices;
using NumEncoding.Base;

namespace NumEncoding.Base
{
    /// <summary>
    /// Represents a described data block that is used to form a portion of each data entry.
    /// </summary>
    /// <param name="index">The index of the data block in a data entry.</param>
    /// <param name="propertyName">The name of the property that the data block is associated with.</param>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class DataBlock([CallerLineNumber] int index = 0, [CallerMemberName] string propertyName = "") : Attribute
    {
        /// <summary>
        /// Gets the index of this data block in a data entry.
        /// </summary>
        public int Index { get; } = index;

        /// <summary>
        /// Gets the name of the property that this data block is associated with.
        /// </summary>
        public string PropertyName { get; } = propertyName;

        /// <summary>
        /// Gets or sets the compression that is specifically used by this data block.
        /// </summary>
        public DataBlockCompression? Compression { get; set; } = null;

        /// <summary>
        /// Gets the number of bytes that this data block uses, where <see langword="null"/> represents a variable length.
        /// </summary>
        public abstract int? ByteLength { get; }

        /// <summary>
        /// Gets the encoder of this data block, which takes in the original object and returns the encoded byte array.
        /// </summary>
        public abstract Func<object, byte[]> Encoder { get; }

        /// <summary>
        /// Gets the decoder of this data block, which takes in the encoded byte array and returns the original object.
        /// </summary>
        public abstract Func<byte[], object> Decoder { get; }
    }
}

namespace NumEncoding.DataBlocks
{
    /// <summary>
    /// Represents a described data block with a predefined length that supports <see cref="byte"/>, <see cref="sbyte"/>, <see cref="bool"/>, <see cref="char"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, and <see cref="float"/> types.
    /// </summary>
    /// <typeparam name="T">The type of the data being stored, which must exactly match the type of the provided objects.</typeparam>
    /// <param name="index">The index of the data block in a data entry.</param>
    /// <param name="propertyName">The name of the property that the data block is associated with.</param>
    public class NumericDataBlock<T>([CallerLineNumber] int index = 0, [CallerMemberName] string propertyName = "") : DataBlock(index, propertyName)
    {
        public override int? ByteLength { get; } = default(T) switch
        {
            byte => sizeof(byte),
            sbyte => sizeof(sbyte),
            bool => sizeof(bool),
            char => sizeof(char),
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

        public override Func<object, byte[]> Encoder { get; } = default(T) switch
        {
            byte => value => [(byte)value],
            sbyte => value => [(byte)value],
            bool => value => BitConverter.GetBytes((bool)value),
            char => value => BitConverter.GetBytes((char)value),
            short => value => BitConverter.GetBytes((short)value),
            ushort => value => BitConverter.GetBytes((ushort)value),
            int => value => BitConverter.GetBytes((int)value),
            uint => value => BitConverter.GetBytes((uint)value),
            long => value => BitConverter.GetBytes((long)value),
            ulong => value => BitConverter.GetBytes((ulong)value),
            double => value => BitConverter.GetBytes((double)value),
            float => value => BitConverter.GetBytes((float)value),
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };

        public override Func<byte[], object> Decoder { get; } = default(T) switch
        {
            byte => data => data[0],
            sbyte => data => (sbyte)data[0],
            bool => data => BitConverter.ToBoolean(data),
            char => data => BitConverter.ToChar(data),
            short => data => BitConverter.ToInt16(data),
            ushort => data => BitConverter.ToUInt16(data),
            int => data => BitConverter.ToInt32(data),
            uint => data => BitConverter.ToUInt32(data),
            long => data => BitConverter.ToInt64(data),
            ulong => data => BitConverter.ToUInt64(data),
            double => data => BitConverter.ToDouble(data),
            float => data => BitConverter.ToSingle(data),
            _ => throw new NotSupportedException("The provided data type is not supported.")
        };
    }

    /// <summary>
    /// Represents a described data block with a variable length that directly operates on byte arrays.
    /// </summary>
    /// <param name="index">The index of the data block in a data entry.</param>
    /// <param name="propertyName">The name of the property that the data block is associated with.</param>
    public class ByteArrayDataBlock([CallerLineNumber] int index = 0, [CallerMemberName] string propertyName = "") : DataBlock(index, propertyName)
    {
        public override int? ByteLength { get; } = null;
        public override Func<object, byte[]> Encoder { get; } = value => (byte[])value;
        public override Func<byte[], object> Decoder { get; } = data => data;
    }

    /// <summary>
    /// Represents a described data block with a variable length that supports <see cref="string"/> objects.
    /// </summary>
    /// <param name="encoding">The encoding to be used for conversion.</param>
    /// <param name="index">The index of the data block in a data entry.</param>
    /// <param name="propertyName">The name of the property that the data block is associated with.</param>
    public class StringDataBlock(StringDataBlock.StringEncoding encoding, [CallerLineNumber] int index = 0, [CallerMemberName] string propertyName = "") : DataBlock(index, propertyName)
    {
        /// <summary>
        /// Gets the encoding to be used for this data block.
        /// </summary>
        public StringEncoding ValueEncoding { get; } = encoding;

        public override int? ByteLength { get; } = null;

        public override Func<object, byte[]> Encoder { get; } = encoding switch
        {
            StringEncoding.ASCII => value => Encoding.ASCII.GetBytes((string)value),
            StringEncoding.Latin1 => value => Encoding.Latin1.GetBytes((string)value),
            StringEncoding.Unicode => value => Encoding.Unicode.GetBytes((string)value),
            StringEncoding.BigEndianUnicode => value => Encoding.BigEndianUnicode.GetBytes((string)value),
            StringEncoding.UTF8 => value => Encoding.UTF8.GetBytes((string)value),
            StringEncoding.UTF32 => value => Encoding.UTF32.GetBytes((string)value),
            _ => throw new NotSupportedException("The provided encoding is not supported.")
        };

        public override Func<byte[], object> Decoder { get; } = encoding switch
        {
            StringEncoding.ASCII => Encoding.ASCII.GetString,
            StringEncoding.Latin1 => Encoding.Latin1.GetString,
            StringEncoding.Unicode => Encoding.Unicode.GetString,
            StringEncoding.BigEndianUnicode => Encoding.BigEndianUnicode.GetString,
            StringEncoding.UTF8 => Encoding.UTF8.GetString,
            StringEncoding.UTF32 => Encoding.UTF32.GetString,
            _ => throw new NotSupportedException("The provided encoding is not supported.")
        };

        /// <summary>
        /// Specifies the encoding to use for a given <see cref="string"/>.
        /// </summary>
        public enum StringEncoding
        {
            /// <summary>
            /// An encoding for the ASCII (7-bit) character set.
            /// </summary>
            ASCII,

            /// <summary>
            /// An encoding for the Latin1 character set (ISO-8859-1).
            /// </summary>
            Latin1,

            /// <summary>
            /// An encoding for the UTF-16 format that uses the little endian byte order.
            /// </summary>
            Unicode,

            /// <summary>
            /// An encoding for the UTF-16 format that uses the big endian byte order.
            /// </summary>
            BigEndianUnicode,

            /// <summary>
            /// An encoding for the UTF-8 format.
            /// </summary>
            UTF8,

            /// <summary>
            /// An encoding for the UTF-32 format that uses the little endian byte order.
            /// </summary>
            UTF32
        }
    }
}
