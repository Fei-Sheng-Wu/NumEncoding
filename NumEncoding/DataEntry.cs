using System;
using NumEncoding.Base;

namespace NumEncoding
{
    /// <summary>
    /// Represents a data entry that consists of the raw data in accordance with the data blocks in a described data structure.
    /// </summary>
    /// <param name="length">The number of elements in the data entry.</param>
    public class DataEntry(int length)
    {
        /// <summary>
        /// Gets the array of the raw data stored in this data entry.
        /// </summary>
        public object[] Data { get; } = new object[length];

        /// <summary>
        /// Gets the number of elements stored in this data entry.
        /// </summary>
        public int Length { get; } = length;

        /// <summary>
        /// Gets or sets the element at the given index.
        /// </summary>
        /// <param name="index">The index of the element to access.</param>
        public object this[int index]
        {
            get { return Data[index]; }
            set { Data[index] = value; }
        }

        /// <summary>
        /// Converts the data represented in this data entry to a custom <see langword="class"/>, using the defined data blocks in the given data structure.
        /// </summary>
        /// <typeparam name="T">The type of the custom <see langword="class"/>.</typeparam>
        /// <param name="structure">The described data structure to be used for conversion.</param>
        /// <returns>The converted instance of the custom <see langword="class"/> with the data from this data entry.</returns>
        public T CastData<T>(DataStructure structure) where T : class, new()
        {
            T result = new();

            int index = 0;
            foreach (DataBlock block in structure.DataBlocks)
            {
                (typeof(T).GetProperty(block.PropertyName) ?? throw new Exception($"Unable to retrieve the property named \"{block.PropertyName}\"."))
                    .SetValue(result, this[index++]);
            }

            return result;
        }
    }
}
