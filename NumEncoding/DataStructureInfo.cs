using System;
using System.Text.Json.Serialization;

namespace NumEncoding.DataStructureInfo
{
    /// <summary>
    /// Represents the version of a data structure.
    /// </summary>
    /// <param name="version">The version of the data structure.</param>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataStructureVersion(byte version) : Attribute
    {
        /// <summary>
        /// Gets the number identifier that defines this version.
        /// </summary>
        public byte Version { get; } = version;
    }

    /// <summary>
    /// Represents the general compression info to be consistently used for a data structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataStructureCompressionInfo : Attribute
    {
        [JsonIgnore]
        public override object TypeId { get; } = 0;

        /// <summary>
        /// The number of data entries between I-frame data entries, including a leading I-frame data entry.
        /// </summary>
        public int IFrameInterval { get; set; }
    }

    /// <summary>
    /// Represents the custom info to be put into the heading of a data structure.
    /// </summary>
    /// <param name="byteLength">The predefined length of the custom info.</param>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataStructureCustomInfo(int byteLength) : Attribute
    {
        /// <summary>
        /// Represents the custom info to be put into the heading of a data structure.
        /// </summary>
        /// <param name="byteLength">The predefined length of the custom info.</param>
        /// <param name="info">The default value for the custom info.</param>
        [JsonConstructor]
        public DataStructureCustomInfo(int byteLength, byte[] info) : this(byteLength) => Info = info;

        [JsonIgnore]
        public override object TypeId { get; } = 0;

        /// <summary>
        /// Gets the number of bytes that is defined for this custom info.
        /// </summary>
        public int ByteLength { get; } = byteLength;

        /// <summary>
        /// Gets the value to be written for this custom info.
        /// </summary>
        public byte[] Info { get; set; } = new byte[byteLength];
    }
}
