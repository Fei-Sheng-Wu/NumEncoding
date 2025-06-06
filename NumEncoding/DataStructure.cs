using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Collections.Generic;
using NumEncoding.Base;
using NumEncoding.DataStructureInfo;

namespace NumEncoding
{
    /// <summary>
    /// Represents a described data structure that can be used for encoding and decoding, with a specific collection of data blocks defined for each data entry.
    /// </summary>
    /// <param name="version">The unique version number to be used as an identifier of the structure.</param>
    /// <param name="dataBlocks">The collection of data blocks to be used for each data entry.</param>
    public class DataStructure(byte version, IReadOnlyCollection<DataBlock> dataBlocks)
    {
        /// <summary>
        /// Gets the version number of this data structure.
        /// </summary>
        public virtual byte Version { get; } = version;

        /// <summary>
        /// Gets the collection of data blocks defined for this data structure.
        /// </summary>
        public virtual IReadOnlyCollection<DataBlock> DataBlocks { get; } = dataBlocks;

        /// <summary>
        /// Gets or sets the general compression info to be consistently used for this data structure.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual DataStructureCompressionInfo? Compression { get; set; }

        /// <summary>
        /// Gets or sets the custom info to be put into the heading of this data structure.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual DataStructureCustomInfo? CustomInfo { get; set; }

        /// <summary>
        /// Determines whether the provided version is acceptable by this data structure.
        /// </summary>
        /// <param name="version">The version to be validated.</param>
        /// <returns><see langword="true"/> if the provided version is acceptable; otherwise, <see langword="false"/>.</returns>
        public virtual bool ValidateVersion(byte version) => version == Version;

        /// <summary>
        /// Converts the data represented by a custom <see langword="class"/> to a generic <see cref="DataEntry"/> object that represents the raw data, using the defined data blocks in this data structure.
        /// </summary>
        /// <typeparam name="T">The type of the custom <see langword="class"/>.</typeparam>
        /// <param name="data">The instance of the custom <see langword="class"/> to be converted.</param>
        /// <returns>The converted <see cref="DataEntry"/> object that represents the data required by the definition of this data structure.</returns>
        public DataEntry CastData<T>(T data) where T : class
        {
            DataEntry result = new(DataBlocks.Count);

            int index = 0;
            foreach (DataBlock block in DataBlocks)
            {
                result[index++] = (typeof(T).GetProperty(block.PropertyName) ?? throw new Exception($"Unable to retrieve the property named \"{block.PropertyName}\"."))
                    .GetValue(data) ?? throw new Exception($"Unable to retrieve the value of the property named \"{block.PropertyName}\".");
            }

            return result;
        }

        /// <summary>
        /// Converts a custom <see langword="class"/> of a described data structure to a <see cref="DataStructure"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the custom <see langword="class"/>.</typeparam>
        /// <returns>The converted instance of <see cref="DataStructure"/>.</returns>
        public static DataStructure CreateFromClass<T>() where T : class => new(
            (typeof(T).GetCustomAttribute(typeof(DataStructureVersion)) as DataStructureVersion)?.Version ?? byte.MinValue,
            [.. typeof(T).GetProperties().Where(x => x.IsDefined(typeof(DataBlock)))
            .Select(x => {
                DataBlock block = x.GetCustomAttribute(typeof(DataBlock)) as DataBlock ?? throw new Exception($"Unable to retrieve the attribute from the property named \"{x.Name}\".");
                block.Compression = x.GetCustomAttribute(typeof(DataBlockCompression)) as DataBlockCompression;
                return block;
            })
            .OrderBy(x => x.Index)])
        {
            Compression = typeof(T).GetCustomAttribute(typeof(DataStructureCompressionInfo)) as DataStructureCompressionInfo,
            CustomInfo = typeof(T).GetCustomAttribute(typeof(DataStructureCustomInfo)) as DataStructureCustomInfo
        };

        /// <summary>
        /// Converts a JSON text of a described data structure to a <see cref="DataStructure"/> instance.
        /// </summary>
        /// <param name="json">The JSON text to be converted.</param>
        /// <param name="options">The options to control the behavior during conversion.</param>
        /// <param name="customDeserializer">The optional callback to parse custom defined <see cref="DataBlock"/> and <see cref="DataBlockCompression"/> objects, where the first parameter is the name of the object's type, the second parameter is the additional info found for the object, and the return value is the parsed object.</param>
        /// <returns>The converted instance of <see cref="DataStructure"/>.</returns>
        public static DataStructure CreateFromJson(string json, JsonSerializerOptions? options = null, Func<string, string[], object?>? customDeserializer = null)
        {
            options ??= new JsonSerializerOptions();
            options.Converters.Add(new DataBlockJsonConverter()
            {
                CustomDeserializer = customDeserializer
            });
            return JsonSerializer.Deserialize<DataStructure>(json, options) ?? throw new Exception("Unable to deserialize the provided JSON text.");
        }

        /// <summary>
        /// Converts this data structure to a JSON text.
        /// </summary>
        /// <param name="options">The options to control the behavior during conversion.</param>
        /// <param name="customSerializer">The optional callback to parse custom defined <see cref="DataBlock"/> and <see cref="DataBlockCompression"/> objects, where the first parameter is the object to be parsed and the return value is the additional info to be saved along for deserialization purposes.</param>
        /// <returns>The converted JSON text.</returns>
        public string ToJson(JsonSerializerOptions? options = null, Func<object, string[]?>? customSerializer = null)
        {
            options ??= new JsonSerializerOptions();
            options.Converters.Add(new DataBlockJsonConverter()
            {
                CustomSerializer = customSerializer
            });
            return JsonSerializer.Serialize(this, options);
        }
    }

    /// <summary>
    /// Represents a collection of <see cref="DataStructure"/> instances, with the ability to automatically or manually switch between the described data structures based on their versions.
    /// </summary>
    /// <param name="structures">The collection of <see cref="DataStructure"/> instances.</param>
    public class MultiVersionDataStructure(IEnumerable<DataStructure> structures) : DataStructure(byte.MinValue, [])
    {
        /// <summary>
        /// Gets all of the <see cref="DataStructure"/> instances that is existent in this collection of data structure.
        /// </summary>
        public IEnumerable<DataStructure> AllStructures { get; } = structures;

        public override byte Version => selectedVersion ?? throw new Exception("No version is selected.");
        public override IReadOnlyCollection<DataBlock> DataBlocks => selectedStructures ?? throw new Exception("No version is selected.");
        public override bool ValidateVersion(byte version) => UseVersion(version);

        private byte? selectedVersion = null;
        private IReadOnlyCollection<DataBlock>? selectedStructures = null;

        /// <summary>
        /// Manually switches the data structure to use based on the target version.
        /// </summary>
        /// <param name="version">The version to use.</param>
        /// <returns><see langword="true"/> if an acceptable data structure is found and selected; otherwise, <see langword="false"/>.</returns>
        public bool UseVersion(byte version)
        {
            foreach (DataStructure structure in AllStructures)
            {
                if (structure.ValidateVersion(version))
                {
                    selectedVersion = structure.Version;
                    selectedStructures = structure.DataBlocks;
                    return true;
                }
            }
            return false;
        }
    }
}
