using System;
using System.IO;
using NumEncoding.Base;

namespace NumEncoding
{
    /// <summary>
    /// Represents a decoder that works with a <see cref="Stream"/> object.
    /// </summary>
    /// <param name="structure">The data structure to be used for the decoder.</param>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    public class StreamDecoder(DataStructure structure, Stream stream) : DataDecoder(structure), IDisposable
    {
        /// <summary>
        /// The <see cref="BinaryReader"/> object that this decoder uses.
        /// </summary>
        public BinaryReader Reader { get; } = new(stream);

        public override bool CanRead() => Position < Reader.BaseStream.Length;
        public override byte[] ReadBytes(int length) => Reader.ReadBytes(length);

        /// <summary>
        /// Gets the position within the given <see cref="Stream"/>.
        /// </summary>
        public long Position => Reader.BaseStream.Position;

        /// <summary>
        /// Sets the position within the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the given <see cref="Stream"/>.</returns>
        public long Seek(long offset, SeekOrigin origin) => Reader.BaseStream.Seek(offset, origin);

        public void Dispose()
        {
            Reader.Close();
            Reader.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
