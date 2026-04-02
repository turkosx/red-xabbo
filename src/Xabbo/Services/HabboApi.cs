using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Net.Http.Json;

using Xabbo.Core;
using Xabbo.Services.Abstractions;
using Xabbo.Web.Dto;
using Xabbo.Web.Serialization;

namespace Xabbo.Services;

public sealed class HabboApi : IHabboApi
{
    private readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = {
            { "User-Agent", "xabbo" }
        }
    };

    private static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return JsonWebContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new Exception($"Failed to get type info for '{typeof(T)}'.");
    }

    private async Task<T> GetRequiredDataAsync<T>(Hotel hotel, string path, CancellationToken cancellationToken = default)
    {
        if (!path.StartsWith('/'))
            throw new ArgumentException("Path must start with '/'.", nameof(path));

        var typeInfo = GetTypeInfo<T>();

        var res = await _http.GetAsync($"https://{hotel.WebHost}{path}", cancellationToken);
        res.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<T>(
            res.Content.ReadAsStream(cancellationToken), typeInfo, cancellationToken)
            ?? throw new Exception($"Failed to deserialize {typeInfo.Type.Name}.");
    }

    private async Task<TResponse> PostRequiredDataAsync<TRequest, TResponse>(
        Hotel hotel, string path, TRequest request, CancellationToken cancellationToken = default)
    {
        if (!path.StartsWith('/'))
            throw new ArgumentException("Path must start with '/'.", nameof(path));

        var requestTypeInfo = GetTypeInfo<TRequest>();
        var responseTypeInfo = GetTypeInfo<TResponse>();

        var res = await _http.PostAsJsonAsync(
            $"https://{hotel.WebHost}{path}", request, requestTypeInfo, cancellationToken);
        res.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<TResponse>(
            res.Content.ReadAsStream(cancellationToken), responseTypeInfo, cancellationToken)
            ?? throw new Exception($"Failed to deserialize {responseTypeInfo.Type.Name}.");
    }

    public Task<Web.Dto.MarketplaceItemStats> FetchMarketplaceItemStats(Hotel hotel, ItemType type, string identifier, CancellationToken cancellationToken = default)
    {
        MarketplaceItemStatsBatchRequest request = type switch
        {
            ItemType.Floor => new() { RoomItems = [new() { Item = identifier }] },
            ItemType.Wall => new() { WallItems = [new() { Item = identifier }] },
            _ => throw new Exception($"Invalid item type: {type}.")
        };

        return FetchMarketplaceItemStatsBatchAsync(hotel, type, identifier, request, cancellationToken);
    }

    private async Task<Web.Dto.MarketplaceItemStats> FetchMarketplaceItemStatsBatchAsync(
        Hotel hotel,
        ItemType type,
        string identifier,
        MarketplaceItemStatsBatchRequest request,
        CancellationToken cancellationToken)
    {
        MarketplaceItemStatsBatchResponse response = await PostRequiredDataAsync<MarketplaceItemStatsBatchRequest, MarketplaceItemStatsBatchResponse>(
            hotel, "/api/public/marketplace/stats/batch", request, cancellationToken);

        MarketplaceItemStatsBatchEntry? entry = type switch
        {
            ItemType.Floor => response.RoomItemData.FirstOrDefault(x => x.Item.Equals(identifier, StringComparison.OrdinalIgnoreCase)),
            ItemType.Wall => response.WallItemData.FirstOrDefault(x => x.Item.Equals(identifier, StringComparison.OrdinalIgnoreCase)),
            _ => null
        };

        if (entry is null)
            throw new Exception($"Marketplace stats were not returned for '{identifier}'.");

        return new Web.Dto.MarketplaceItemStats
        {
            History = entry.History,
            StatsDate = entry.StatsDate,
            SoldItemCount = entry.SoldItemCount,
            CreditSum = entry.CreditSum,
            AveragePrice = entry.AveragePrice,
            TotalOpenOffers = entry.TotalOpenOffers,
            HistoryLimitInDays = entry.HistoryLimitInDays
        };
    }

    public Task<Web.Dto.PhotoData> FetchPhotoDataAsync(Hotel hotel, string photoId, CancellationToken cancellationToken = default)
    {
        string encodedPhotoId = Uri.EscapeDataString(photoId);
        return GetRequiredDataAsync<Web.Dto.PhotoData>(
            hotel, $"/photodata/public/furni/{encodedPhotoId}", cancellationToken);
    }
}
