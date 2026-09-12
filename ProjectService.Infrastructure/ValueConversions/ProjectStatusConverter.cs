using Newtonsoft.Json;
using ProjectService.Domain.Enums;

namespace ProjectService.Infrastructure.ValueConversions;

/// <summary>
/// JSON converter for ProjectStatus Value Object.
/// Serializes/deserializes by integer value.
/// </summary>
public class ProjectStatusConverter : JsonConverter<ProjectStatus>
{
    public override ProjectStatus ReadJson(
        JsonReader reader,
        Type objectType,
        ProjectStatus existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return ProjectStatus.FromValue(Convert.ToInt32(reader.Value!));
        }

        if (reader.TokenType == JsonToken.String && reader.Value is string stringValue)
        {
            return ProjectStatus.FromName(stringValue);
        }

        throw new JsonSerializationException(
            $"Unexpected token type: {reader.TokenType}");
    }

    public override void WriteJson(
        JsonWriter writer,
        ProjectStatus? value,
        JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.Value);
        }
    }
}
