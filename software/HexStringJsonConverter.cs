// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class HexStringJsonConverter : JsonConverter<UInt16>
{
    public override UInt16 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Convert.ToUInt16(reader.GetString(), 16);
    public override void Write(Utf8JsonWriter writer, UInt16 value, JsonSerializerOptions options) => writer.WriteStringValue($"0x{value.ToString("X")}");
}