using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenDive.BCS;
using System;
using System.Collections.Generic;
using System.Linq;

public class SequenceConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Sequence);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Read the array from JSON
        JArray jArray = JArray.Load(reader);

        // For simple string arrays
        if (jArray.Count > 0 && jArray[0].Type == JTokenType.String)
        {
            // Convert all items to BString objects
            var bStrings = jArray.Select(item => new BString(item.ToString())).ToArray();
            return new Sequence(bStrings);
        }
        // For array of arrays (like the 'b' field)
        else if (jArray.Count > 0 && jArray[0].Type == JTokenType.Array)
        {
            // Create an array of Sequence objects
            var sequenceArray = new List<ISerializable>();

            foreach (var innerArray in jArray)
            {
                // Convert the inner array to BString objects
                var innerBStrings = innerArray.Select(item => new BString(item.ToString())).ToArray();
                // Create a Sequence for this inner array
                sequenceArray.Add(new Sequence(innerBStrings));
            }

            // Create a Sequence containing Sequences
            return new Sequence(sequenceArray.ToArray());
        }

        // Default: empty sequence
        return new Sequence(new ISerializable[0]);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Writing Sequence to JSON is not implemented.");
    }
}