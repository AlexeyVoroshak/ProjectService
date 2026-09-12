using Newtonsoft.Json;
using TaskStatus = ProjectService.Domain.Enums.TaskStatus;

namespace ProjectService.Infrastructure.ValueConversions;

/// <summary>
/// JSON converter for TaskStatus Value Object.
/// Serializes/deserializes by integer value.
/// </summary>
public class TaskStatusConverter : JsonConverter<TaskStatus>
{
    public override TaskStatus ReadJson(
        JsonReader reader,
        Type objectType,
        TaskStatus existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return TaskStatus.FromValue(Convert.ToInt32(reader.Value!));
        }

        if (reader.TokenType == JsonToken.String && reader.Value is string stringValue)
        {
            return TaskStatus.FromName(stringValue);
        }

        throw new JsonSerializationException(
            $"Unexpected token type: {reader.TokenType}");
    }

    public override void WriteJson(
        JsonWriter writer,
        TaskStatus? value,
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
