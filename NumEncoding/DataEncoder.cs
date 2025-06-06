using System;
using System.Collections.Generic;

namespace NumEncoding.Base
{
    /// <summary>
    /// Represents an encoder that is based on a defined data structure.
    /// </summary>
    /// <param name="structure">The data structure to be used for the encoder.</param>
    public abstract class DataEncoder(DataStructure structure)
    {
        /// <summary>
        /// The data structure that is associated with this encoder. 
        /// </summary>
        public DataStructure DataStructure { get; } = structure;

        /// <summary>
        /// Writes a given byte array.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        public abstract void WriteBytes(byte[] bytes);

        /// <summary>
        /// Writes the version number of the data structure.
        /// </summary>
        public void WriteVersion() => WriteBytes([DataStructure.Version]);

        /// <summary>
        /// Writes the given custom info or the default info defined in the data structure.
        /// </summary>
        /// <param name="info">The custom info, which has to exactly match the length defined in the data structure.</param>
        public void WriteCustomInfo(byte[]? info = null)
        {
            if (DataStructure.CustomInfo == null)
            {
                return;
            }
            else if (info != null)
            {
                DataStructure.CustomInfo.Info = info;
            }

            if (DataStructure.CustomInfo.Info.Length != DataStructure.CustomInfo.ByteLength)
            {
                throw new Exception($"The provided custom info is different than the reported length of {DataStructure.CustomInfo.ByteLength} bytes.");
            }
            WriteBytes(DataStructure.CustomInfo.Info);
        }

        /// <summary>
        /// Writes the given data entry.
        /// </summary>
        /// <param name="data">The data entry.</param>
        /// <param name="lastData">The last data entry to be used for available compressions.</param>
        public void WriteNext(DataEntry data, DataEntry? lastData = null)
        {
            if (data.Length < DataStructure.DataBlocks.Count)
            {
                throw new ArgumentException("The provided data is insufficient for the structure.", nameof(data));
            }

            int index = 0;
            foreach (DataBlock block in DataStructure.DataBlocks)
            {
                if (block.Compression != null && lastData != null)
                {
                    byte[] bytes = block.Compression.Compressor(lastData[index], data[index++]);
                    if (bytes.Length != block.Compression.PFrameByteLength)
                    {
                        throw new Exception($"Encoded data is different than the reported length of {block.Compression.PFrameByteLength} bytes.");
                    }
                    WriteBytes(bytes);
                }
                else if (block.ByteLength.HasValue)
                {
                    byte[] bytes = block.Encoder(data[index++]);
                    if (bytes.Length != block.ByteLength.Value)
                    {
                        throw new Exception($"Encoded data is different than the reported length of {block.ByteLength.Value} bytes.");
                    }
                    WriteBytes(bytes);
                }
                else
                {
                    WriteBytes(block.Encoder(data[index++]));
                    WriteBytes([0]);
                }
            }
        }

        /// <summary>
        /// Writes all given data entries with the leading heading of the data structure.
        /// </summary>
        /// <param name="data">The data entries.</param>
        /// <param name="customInfo">The optional custom info to be written into the heading, which has to exactly match the length defined in the data structure.</param>
        public void WriteAllWithHeading(IEnumerable<DataEntry> data, byte[]? customInfo = null)
        {
            WriteVersion();
            WriteCustomInfo(customInfo);

            if (DataStructure.Compression != null)
            {
                int index = 0;
                DataEntry? lastData = null;
                foreach (DataEntry entry in data)
                {
                    WriteNext(entry, index < 1 ? null : lastData);

                    if (++index >= DataStructure.Compression.IFrameInterval)
                    {
                        index = 0;
                    }
                    lastData = entry;
                }
            }
            else
            {
                foreach (DataEntry entry in data)
                {
                    WriteNext(entry);
                }
            }
        }
    }
}
