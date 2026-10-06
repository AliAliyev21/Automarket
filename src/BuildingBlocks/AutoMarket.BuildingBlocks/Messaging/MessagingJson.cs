using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoMarket.BuildingBlocks.Messaging;

// Bilinməyən sahələr nəzərə alınmır (ARCHITECTURE §5.5), enum-lar string kimi
internal static class MessagingJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
