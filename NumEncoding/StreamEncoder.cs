using System;
using System.IO;
using NumEncoding.Base;

namespace NumEncoding
{
    /// <summary>
    /// Represents an encoder that works with a <see cref="Stream"/> object.
    /// </summary>
    /// <param name="structure">The data structure to be used for the encoder.</param>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    public class StreamEncoder(DataStructure structure, Stream stream) : DataEncoder(structure), IDisposable
    {
        /// <summary>
        /// The <see cref="BinaryWriter"/> object that this encoder uses.
        /// </summary>
        public BinaryWriter Writer { get; } = new(stream);

        public override void WriteBytes(byte[] bytes) => Writer.Write(bytes);

        /// <summary>
        /// Gets the position within the given <see cref="Stream"/>.
        /// </summary>
        public long Position => Writer.BaseStream.Position;

        /// <summary>
        /// Sets the position within the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the given <see cref="Stream"/>.</returns>
        public long Seek(long offset, SeekOrigin origin) => Writer.BaseStream.Seek(offset, origin);

        public void Dispose()
        {
            Writer.Flush();
            Writer.Close();
            Writer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
