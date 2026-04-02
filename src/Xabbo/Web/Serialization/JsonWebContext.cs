using System.Text.Json.Serialization;
using Xabbo.Core;

namespace Xabbo.Web.Serialization;

[JsonSourceGenerationOptions(
    NumberHandling = JsonNumberHandling.AllowReadingFromString
)]
[JsonSerializable(typeof(Dto.MarketplaceItemStats))]
[JsonSerializable(typeof(Dto.MarketplaceItemStatsBatchRequest))]
[JsonSerializable(typeof(Dto.MarketplaceItemStatsBatchResponse))]
[JsonSerializable(typeof(PhotoInfo))]
[JsonSerializable(typeof(Dto.PhotoData))]
public partial class JsonWebContext : JsonSerializerContext;
