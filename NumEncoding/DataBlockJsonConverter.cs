using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using NumEncoding.DataBlocks;
using NumEncoding.DataBlockCompressions;

namespace NumEncoding.Base
{
    /// <summary>
    /// Represents a custom JSON converter for <see cref="DataBlock"/> objects.
    /// </summary>
    public class DataBlockJsonConverter : JsonConverter<DataBlock>
    {
        /// <summary>
        /// The optional callback to parse custom defined <see cref="DataBlock"/> and <see cref="DataBlockCompression"/> objects, where the first parameter is the object to be parsed and the return value is the additional info to be saved along for deserialization purposes.
        /// </summary>
        public Func<object, string[]?>? CustomSerializer { get; set; } = null;

        /// <summary>
        /// The optional callback to parse custom defined <see cref="DataBlock"/> and <see cref="DataBlockCompression"/> objects, where the first parameter is the name of the object's type, the second parameter is the additional info found for the object, and the return value is the parsed object.
        /// </summary>
        public Func<string, string[], object?>? CustomDeserializer { get; set; } = null;

        public override DataBlock? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            DataBlock? result = null;

            Dictionary<string, object> properties = new()
            {
                { "Type", string.Empty },
                { "Index", 0 },
                { "PropertyName", string.Empty },
                { "Custom", new List<string>() },
                { "Compression", string.Empty },
                { "CompressionCustom", new List<string>() }
            };
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName
                    || reader.GetString() is not string propertyName || !reader.Read())
                {
                    continue;
                }

                string propertyKey = properties.First(x => ConvertPropertyName(x.Key, options) == propertyName).Key;
                switch (properties[propertyKey])
                {
                    case string:
                        properties[propertyKey] = reader.GetString() ?? string.Empty;
                        break;
                    case int:
                        properties[propertyKey] = reader.GetInt32();
                        break;
                    case List<string> list:
                        ReadArray(ref reader, list);
                        break;
                }
            }

            string block = properties["Type"] as string ?? string.Empty;
            int blockIndex = Convert.ToInt32(properties["Index"]);
            string blockPropertyName = properties["PropertyName"] as string ?? string.Empty;
            string[] custom = (properties["Custom"] as List<string>)?.ToArray() ?? [];
            if (block == typeof(NumericDataBlock<>).Name)
            {
                result = Activator.CreateInstance(typeof(NumericDataBlock<>).MakeGenericType(
                    Type.GetType(custom[0]) ?? throw new Exception($"Unable to deserialize the provided JSON text of the generic type.")),
                    blockIndex, blockPropertyName) as DataBlock;
            }
            else if (block == typeof(ByteArrayDataBlock).Name)
            {
                result = new ByteArrayDataBlock(blockIndex, blockPropertyName);
            }
            else if (block == typeof(StringDataBlock).Name && Enum.TryParse(custom[0], out StringDataBlock.StringEncoding stringEncoding))
            {
                result = new StringDataBlock(stringEncoding, blockIndex, blockPropertyName);
            }
            else if (CustomDeserializer?.Invoke(block, custom) is DataBlock customDataBlock)
            {
                result = customDataBlock;
            }

            if (result == null)
            {
                throw new Exception($"Unable to deserialize the provided JSON text of the data block called \"{block}\".");
            }

            string compression = properties["Compression"] as string ?? string.Empty;
            string[] compressionCustom = (properties["CompressionCustom"] as List<string>)?.ToArray() ?? [];
            if (compression == typeof(IFrameOnlyCompression).Name)
            {
                result.Compression = new IFrameOnlyCompression();
            }
            else if (compression == typeof(NumericDeltaCompression<,>).Name)
            {
                result.Compression = Activator.CreateInstance(typeof(NumericDeltaCompression<,>).MakeGenericType(
                    Type.GetType(compressionCustom[0]) ?? throw new Exception($"Unable to deserialize the provided JSON text of the generic type."),
                    Type.GetType(compressionCustom[1]) ?? throw new Exception($"Unable to deserialize the provided JSON text of the generic type."))) as DataBlockCompression;
            }
            else if (CustomDeserializer?.Invoke(compression, compressionCustom) is DataBlockCompression customDataBlockCompression)
            {
                result.Compression = customDataBlockCompression;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, DataBlock value, JsonSerializerOptions options)
        {
            Type dataBlockType = value.GetType();
            writer.WriteStartObject();

            writer.WritePropertyName(ConvertPropertyName("Type", options));
            writer.WriteStringValue(dataBlockType.Name);
            writer.WritePropertyName(ConvertPropertyName("Index", options));
            writer.WriteNumberValue(value.Index);
            writer.WritePropertyName(ConvertPropertyName("PropertyName", options));
            writer.WriteStringValue(value.PropertyName);

            writer.WritePropertyName(ConvertPropertyName("Custom", options));
            writer.WriteStartArray();
            if (dataBlockType.IsGenericType && dataBlockType.GetGenericTypeDefinition() == typeof(NumericDataBlock<>))
            {
                writer.WriteStringValue(dataBlockType.GetGenericArguments()[0].FullName);
            }
            else if (value is StringDataBlock stringDataBlock)
            {
                writer.WriteStringValue(stringDataBlock.ValueEncoding.ToString());
            }
            else
            {
                WriteArray(writer, CustomSerializer?.Invoke(value));
            }
            writer.WriteEndArray();

            if (value.Compression != null)
            {
                Type compressionType = value.Compression.GetType();
                writer.WritePropertyName(ConvertPropertyName("Compression", options));
                writer.WriteStringValue(compressionType.Name);
                writer.WritePropertyName(ConvertPropertyName("CompressionCustom", options));
                writer.WriteStartArray();
                if (compressionType.IsGenericType && compressionType.GetGenericTypeDefinition() == typeof(NumericDeltaCompression<,>))
                {
                    writer.WriteStringValue(compressionType.GetGenericArguments()[0].FullName);
                    writer.WriteStringValue(compressionType.GetGenericArguments()[1].FullName);
                }
                else
                {
                    WriteArray(writer, CustomSerializer?.Invoke(value.Compression));
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public static string ConvertPropertyName(string name, JsonSerializerOptions options) =>
            options.PropertyNamingPolicy?.ConvertName(name) ?? name;

        public static void ReadArray(ref Utf8JsonReader reader, ICollection<string> values)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.GetString() is string value)
                {
                    values.Add(value);
                }
            }
        }

        public static void WriteArray(Utf8JsonWriter writer, IEnumerable<string>? values)
        {
            if (values == null)
            {
                return;
            }

            foreach (string entry in values)
            {
                writer.WriteStringValue(entry);
            }
        }
    }
}
