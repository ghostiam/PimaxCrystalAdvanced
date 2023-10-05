using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VRCFT_Tobii_Advanced;

public struct EyeData
{
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
        public bool GazeDirectionIsValid;
        [JsonConverter(typeof(Vector2MethodConverter))]
        public Vector2 GazeDirection;

        public bool PupilDiameterIsValid;
        public float PupilDiameterMm;

        public bool OpennessIsValid;
        public float Openness;
    }

    public Eye Left;
    public Eye Right;

    public EyeData(Eye left, Eye right)
    {
        Left = left;
        Right = right;
    }
}

public class Vector2MethodConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is EyeData.Vector2 vector2)
        {
            writer.WriteStartArray();
            writer.WriteValue(vector2.X);
            writer.WriteValue(vector2.Y);
            writer.WriteEndArray();
        }
        else
        {
            throw new JsonWriterException($"Unexpected type {value?.GetType()}");
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return new EyeData.Vector2();
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new JsonReaderException($"Unexpected token {reader.TokenType}");
        }

        var array = JArray.Load(reader);
        if (array.Count < 2)
        {
            throw new JsonReaderException($"Expected array of length 2, got {array.Count}");
        }

        var x = array[0].ToObject<float>();
        var y = array[1].ToObject<float>();

        return new EyeData.Vector2(x, y);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(EyeData.Vector2);
    }
}
