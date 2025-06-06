using System.Collections.Generic;
using NumEncoding.Base;

namespace NumEncoding
{
    /// <summary>
    /// Represents an encoder that directly works with a collection of bytes.
    /// </summary>
    /// <param name="structure">The data structure to be used for the encoder.</param>
    /// <param name="byteArray">The collection of bytes to write to.</param>
    public class BytesEncoder(DataStructure structure, ICollection<byte> byteArray) : DataEncoder(structure)
    {
        public override void WriteBytes(byte[] bytes)
        {
            foreach (byte bits in bytes)
            {
                byteArray.Add(bits);
            }
        }
    }
}
