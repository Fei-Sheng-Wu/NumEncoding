using System;
using System.Collections.Generic;

namespace NumEncoding.Base
{
    /// <summary>
    /// Represents a decoder that is based on a defined data structure.
    /// </summary>
    /// <param name="structure">The data structure to be used for the decoder.</param>
    public abstract class DataDecoder(DataStructure structure)
    {
        /// <summary>
        /// The data structure that is associated with this decoder. 
        /// </summary>
        public DataStructure DataStructure { get; } = structure;

        /// <summary>
        /// Determines if this decoder is able to read more bytes.
        /// </summary>
        /// <returns></returns>
        public abstract bool CanRead();

        /// <summary>
        /// Reads a given number of bytes.
        /// </summary>
        /// <param name="length">The number of bytes.</param>
        /// <returns>The bytes read.</returns>
        public abstract byte[] ReadBytes(int length);

        /// <summary>
        /// Reads the next data entry.
        /// </summary>
        /// <param name="lastData">The last data entry to be used for available decompressions.</param>
        /// <returns>The data entry read.</returns>
        public DataEntry? ReadNext(DataEntry? lastData = null)
        {
            DataEntry result = new(DataStructure.DataBlocks.Count);

            int index = 0;
            foreach (DataBlock block in DataStructure.DataBlocks)
            {
                byte[] bytes;
                if (block.Compression != null && lastData != null)
                {
                    bytes = ReadBytes(block.Compression.PFrameByteLength);
                    if (bytes.Length != block.Compression.PFrameByteLength)
                    {
                        return null;
                    }
                    result[index] = block.Compression.Decompressor(lastData[index], bytes);
                    index++;
                }
                else if (block.ByteLength.HasValue)
                {
                    bytes = ReadBytes(block.ByteLength.Value);
                    if (bytes.Length != block.ByteLength.Value)
                    {
                        return null;
                    }
                    result[index++] = block.Decoder(bytes);
                }
                else
                {
                    List<byte> variableLengthBytes = [];
                    byte[] nextByte = ReadBytes(1);
                    while (nextByte.Length > 0 && nextByte[^1] != 0)
                    {
                        variableLengthBytes.AddRange(nextByte);
                        nextByte = ReadBytes(1);
                    }
                    bytes = [.. variableLengthBytes];
                    result[index++] = block.Decoder(bytes);
                }
            }

            return result;
        }

        /// <summary>
        /// Reads all data entries with the leading heading of the data structure.
        /// </summary>
        /// <param name="validateVersion">Whether to validate the version found.</param>
        /// <param name="customInfoCallback">The optional callback to process the custom info read, where the custom info is passed in as a byte array of the length defined in the data structure.</param>
        /// <returns>The data entries read.</returns>
        public IEnumerable<DataEntry> ReadAllWithHeading(bool validateVersion = true, Action<byte[]>? customInfoCallback = null)
        {
            byte[] version = ReadBytes(1);
            if (validateVersion)
            {
                if (version.Length < 1 || !DataStructure.ValidateVersion(version[0]))
                {
                    throw new Exception("The version in the encoded data does not match the version of the structure.");
                }
            }

            if (DataStructure.CustomInfo != null)
            {
                byte[] data = ReadBytes(DataStructure.CustomInfo.ByteLength);
                customInfoCallback?.Invoke(data);
            }

            if (DataStructure.Compression != null)
            {
                int index = 0;
                DataEntry? lastData = null;
                while (CanRead() && ReadNext(index < 1 ? null : lastData) is DataEntry data)
                {
                    yield return data;

                    if (++index >= DataStructure.Compression.IFrameInterval)
                    {
                        index = 0;
                    }
                    lastData = data;
                }
            }
            else
            {
                while (CanRead() && ReadNext() is DataEntry data)
                {
                    yield return data;
                }
            }
        }
    }
}
