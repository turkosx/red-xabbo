# Stage 1 - HabboApi Marketplace Alignment

Date verified: 2026-04-02  
Timezone: America/Sao_Paulo

## Goal

Align the marketplace integration in `src/Xabbo/Services/HabboApi.cs` with the official Habbo public API documentation and live responses.

## Sources checked

- `https://www.habbo.com.br/api/public/api-docs/`
- `https://www.habbo.com/api/public/api-docs/`
- `https://www.habbo.com.br/api/public/api-docs/swagger-ui-init.js`
- `https://www.habbo.com/api/public/api-docs/swagger-ui-init.js`

## What the official API currently exposes

The official Swagger checked on 2026-04-02 documents:

- `POST /api/public/marketplace/stats/batch`

The documented request shape is:

```json
{
  "roomItems": [{ "item": "chair_plasto" }],
  "wallItems": []
}
```

The documented response shape contains:

- `status`
- `roomItemData`
- `wallItemData`

Each item entry includes:

- `item`
- `statsDate`
- `history`
- `soldItemCount`
- `creditSum`
- `averagePrice`
- `totalOpenOffers`
- `currentOpenOffers`
- `currentPrice`
- `historyLimitInDays`

## Live verification

### Legacy endpoint used by the app before this stage

Tested on 2026-04-02:

- `GET https://www.habbo.com.br/api/public/marketplace/stats/roomItem/chair_plasto`
- Result: `404 Not Found`

### Official batch endpoint

Tested on 2026-04-02:

- `POST https://www.habbo.com.br/api/public/marketplace/stats/batch`
- Body:

```json
{
  "roomItems": [{ "item": "chair_plasto" }],
  "wallItems": []
}
```

- Result: `200 OK`
- Returned `status: "OK"` and valid stats for `chair_plasto`

This confirms the current code was depending on a legacy/undocumented path, while the official batch endpoint is live and working.

## Code changes made

Updated:

- `src/Xabbo/Services/HabboApi.cs`
- `src/Xabbo/Web/Dto/MarketplaceItemStatsBatch.cs`
- `src/Xabbo/Web/Serialization/JsonWebContext.cs`

### Summary

- Replaced the legacy marketplace GET call with the official batch POST call.
- Added request/response DTOs for the official batch contract.
- Kept `IHabboApi.FetchMarketplaceItemStats(...)` unchanged for the rest of the application.
- Mapped the batch response back into the existing `MarketplaceItemStats` DTO expected by the UI/components.

## Validation

Validated after the change:

- `dotnet build Xabbo.sln`

Build result on 2026-04-02:

- success
- existing warning remained for `System.Linq.Dynamic.Core 1.4.5`

## Relevant files

- `src/Xabbo/Services/HabboApi.cs`
- `src/Xabbo/Web/Dto/MarketplaceItemStatsBatch.cs`
- `src/Xabbo/Web/Serialization/JsonWebContext.cs`
