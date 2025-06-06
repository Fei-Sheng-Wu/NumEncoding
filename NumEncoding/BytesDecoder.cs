using System.Collections.Generic;
using NumEncoding.Base;

namespace NumEncoding
{
    /// <summary>
    /// Represents a decoder that directly works with a collection of bytes.
    /// </summary>
    /// <param name="structure">The data structure to be used for the decoder.</param>
    /// <param name="byteArray">The collection of bytes to read from.</param>
    public class BytesDecoder(DataStructure structure, IEnumerable<byte> byteArray) : DataDecoder(structure)
    {
        /// <summary>
        /// Represents a decoder that directly works with a collection of bytes.
        /// </summary>
        /// <param name="structure">The data structure to be used for the decoder.</param>
        /// <param name="byteArray">The collection of bytes to read from.</param>
        /// <param name="startIndex">The starting index of the collection of bytes.</param>
        public BytesDecoder(DataStructure structure, IEnumerable<byte> byteArray, int startIndex) : this(structure, byteArray)
        {
            for (int i = 0; i < startIndex; i++)
            {
                Enumerator.MoveNext();
            }
        }

        /// <summary>
        /// Gets the enumerator that iterates through the collection of bytes that this decoder reads from.
        /// </summary>
        public IEnumerator<byte> Enumerator { get; } = byteArray.GetEnumerator();

        private bool canRead = true;

        public override bool CanRead() => canRead;
        public override byte[] ReadBytes(int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                if (!Enumerator.MoveNext())
                {
                    result = [];
                    canRead = false;
                    break;
                }
                result[i] = Enumerator.Current;
            }

            return result;
        }
    }
}
