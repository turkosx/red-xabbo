using System.Text.Json.Serialization;

namespace Xabbo.Web.Dto;

public sealed class MarketplaceItemStatsBatchRequest
{
    [JsonPropertyName("roomItems")]
    public List<MarketplaceItemStatsBatchItemRequest> RoomItems { get; set; } = [];

    [JsonPropertyName("wallItems")]
    public List<MarketplaceItemStatsBatchItemRequest> WallItems { get; set; } = [];
}

public sealed class MarketplaceItemStatsBatchItemRequest
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = "";
}

public sealed class MarketplaceItemStatsBatchResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("roomItemData")]
    public List<MarketplaceItemStatsBatchEntry> RoomItemData { get; set; } = [];

    [JsonPropertyName("wallItemData")]
    public List<MarketplaceItemStatsBatchEntry> WallItemData { get; set; } = [];
}

public sealed class MarketplaceItemStatsBatchEntry
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = "";

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }

    [JsonPropertyName("history")]
    public List<MarketplaceItemHistoryEntry> History { get; set; } = [];

    [JsonPropertyName("statsDate")]
    public string StatsDate { get; set; } = "";

    [JsonPropertyName("soldItemCount")]
    public int SoldItemCount { get; set; }

    [JsonPropertyName("creditSum")]
    public int CreditSum { get; set; }

    [JsonPropertyName("averagePrice")]
    public int AveragePrice { get; set; }

    [JsonPropertyName("totalOpenOffers")]
    public int TotalOpenOffers { get; set; }

    [JsonPropertyName("historyLimitInDays")]
    public int HistoryLimitInDays { get; set; }

    [JsonPropertyName("currentOpenOffers")]
    public int CurrentOpenOffers { get; set; }

    [JsonPropertyName("currentPrice")]
    public int CurrentPrice { get; set; }
}
