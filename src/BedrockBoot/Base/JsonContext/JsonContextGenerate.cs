using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Game;

namespace BedrockBoot.Base.JsonContext
{
	[JsonSourceGenerationOptions(
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.ConfigEntry))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.WindowInfo))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.GameFolderInfo))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.Game.GameFileInfo))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.Game.VersionInfo))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.Game.VersionConfig))]
	[JsonSerializable(typeof(BedrockBoot.Base.Enum.Game.GameBuildType))]
	[JsonSerializable(typeof(BedrockBoot.Base.Enum.Game.GameVersionType))]
	[JsonSerializable(typeof(BedrockLauncher.Core.VersionType))] // 注意: 这个类型来自外部引用
	[JsonSerializable(typeof(System.Collections.Generic.List<BedrockBoot.Base.Entry.GameFolderInfo>))]
	[JsonSerializable(typeof(System.Collections.Generic.List<BedrockBoot.Base.Entry.Game.GameFileInfo>))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.Game.VersionConfig.VersionInfo))]
	[JsonSerializable(typeof(BedrockBoot.Base.Entry.Game.VersionConfig.VersionConfigEntry))]
	public partial class BedrockBootJsonContext : JsonSerializerContext
	{
	}
	public class GameVersionTypeJsonConverter : JsonConverter<GameVersionType>
	{
		public override GameVersionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				var stringValue = reader.GetString();
				return stringValue?.ToLowerInvariant() switch
				{
					"release" => GameVersionType.Release,
					"preview" => GameVersionType.Preview,
					"beta" => GameVersionType.Beta,
					_ => throw new JsonException($"Invalid GameVersionType value: {stringValue}")
				};
			}

			throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		public override void Write(Utf8JsonWriter writer, GameVersionType value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString().ToLowerInvariant());
		}
	}

	// GameBuildType 枚举转换器
	public class GameBuildTypeJsonConverter : JsonConverter<GameBuildType>
	{
		public override GameBuildType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				var stringValue = reader.GetString();
				return stringValue?.ToUpperInvariant() switch
				{
					"UWP" => GameBuildType.Uwp,
					"GDK" => GameBuildType.Gdk,
					_ => throw new JsonException($"Invalid GameBuildType value: {stringValue}")
				};
			}

			throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		public override void Write(Utf8JsonWriter writer, GameBuildType value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString().ToUpperInvariant());
		}
	}
}
