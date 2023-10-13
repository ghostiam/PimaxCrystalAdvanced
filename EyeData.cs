using System.Text.Json;
using System.Text.Json.Serialization;

namespace VRCFT_Tobii_Advanced;

public struct EyeData
{
    [JsonConverter(typeof(Vector2MethodConverter))]
    public struct Vector2
    {
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X;
        public float Y;
    }


    public struct Eye
    {
        [JsonInclude]
        [JsonPropertyName("gaze_direction_is_valid")]
        public bool GazeDirectionIsValid;

        [JsonInclude]
        [JsonPropertyName("gaze_direction")]
        public Vector2 GazeDirection;

        [JsonInclude]
        [JsonPropertyName("pupil_diameter_is_valid")]
        public bool PupilDiameterIsValid;

        [JsonInclude]
        [JsonPropertyName("pupil_diameter_mm")]
        public float PupilDiameterMm;

        [JsonInclude]
        [JsonPropertyName("openness_is_valid")]
        public bool OpennessIsValid;

        [JsonInclude]
        [JsonPropertyName("openness")]
        public float Openness;
    }

    [JsonInclude]
    [JsonPropertyName("left")]
    public Eye Left;
    [JsonInclude]
    [JsonPropertyName("right")]
    public Eye Right;

    public EyeData(Eye left, Eye right)
    {
        Left = left;
        Right = right;
    }
}


public class Vector2MethodConverter : JsonConverter<EyeData.Vector2>
{
    public override EyeData.Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new EyeData.Vector2();
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Unexpected token {reader.TokenType}");
        }

        var array = JsonSerializer.Deserialize<float[]>(ref reader, options);
        if (array == null || array.Length < 2)
        {
            throw new JsonException($"Expected array of length 2, got {array?.Length}");
        }

        var x = array[0];
        var y = array[1];

        return new EyeData.Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, EyeData.Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
