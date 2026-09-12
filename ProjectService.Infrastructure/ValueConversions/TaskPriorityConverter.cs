using Newtonsoft.Json;
using ProjectService.Domain.Enums;

namespace ProjectService.Infrastructure.ValueConversions;

/// <summary>
/// JSON converter for TaskPriority Value Object.
/// Serializes/deserializes by integer value.
/// </summary>
public class TaskPriorityConverter : JsonConverter<TaskPriority>
{
    public override TaskPriority ReadJson(
        JsonReader reader,
        Type objectType,
        TaskPriority existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return TaskPriority.FromValue(Convert.ToInt32(reader.Value!));
        }

        if (reader.TokenType == JsonToken.String && reader.Value is string stringValue)
        {
            return TaskPriority.FromName(stringValue);
        }

        throw new JsonSerializationException(
            $"Unexpected token type: {reader.TokenType}");
    }

    public override void WriteJson(
        JsonWriter writer,
        TaskPriority? value,
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
