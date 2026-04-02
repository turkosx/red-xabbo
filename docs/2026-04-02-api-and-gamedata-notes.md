# API and Game Data Verification Notes

Date verified: 2026-04-02  
Timezone used for notes: America/Sao_Paulo

## Scope

These notes document the external references checked while investigating the intermittent `307 Temporary Redirect` failure shown by `redxabbo v1.1.6` during game data loading.

## URLs verified

- `https://xabbo.io/api/Xabbo`
- `https://sulek.dev/api`
- `https://www.habbo.com.br/api/public/api-docs/`
- `https://www.habbo.com/api/public/api-docs/`
- `https://www.habbo.com.br/gamedata/furnidata/1`
- `https://www.habbo.com.br/gamedata/furnidata_json/1`
- `https://www.habbo.com.br/gamedata/hashes2`

## Findings

### 1. `xabbo.io/api/Xabbo`

- The page is a generated API reference for Xabbo .NET types.
- It is useful as a library reference for types such as `Hotel`, `Session`, `ConnectedEventArgs`, and related namespaces.
- It is not a REST API surface for Habbo data.

### 2. `sulek.dev/api`

- The URL currently resolves to a JavaScript SPA shell.
- The landing HTML alone does not expose a Swagger/OpenAPI document or a clearly stable backend contract.
- For this investigation, it should be treated as an auxiliary public reference/tool rather than the source of truth for extension behavior.

### 3. Official Habbo Swagger docs

- `https://www.habbo.com.br/api/public/api-docs/`
- `https://www.habbo.com/api/public/api-docs/`

As of 2026-04-02, both Swagger pages expose the same public API groups.

Documented path groups observed:

- achievements
- badge owners
- groups and group members
- hotlooks
- matches
- derby minigame
- ping
- rooms
- skills and skills leaderboard
- users, profile, badges, friends, groups, rooms
- marketplace stats batch

Important note for this repository:

- The current code in `src/Xabbo/Services/HabboApi.cs` calls `GET /api/public/marketplace/stats/{type}/{identifier}`.
- The official Swagger checked on 2026-04-02 lists `POST /api/public/marketplace/stats/batch`, but does not list the path format currently used by the app.
- That does not prove the current endpoint is broken, but it does mean the extension depends on an endpoint that is not documented in the current official Swagger.

### 4. Game data redirect behavior on `www.habbo.com.br`

Observed on 2026-04-02:

- `GET https://www.habbo.com.br/gamedata/furnidata/1`
  - returns `307 Temporary Redirect`
  - redirects to `https://www.habbo.com.br/gamedata/furnidata/2c52fffcf732819970adb6210e58485c852e04ac`
  - final response is `200 OK`
  - final content type is `application/xml;charset=UTF-8`

- `GET https://www.habbo.com.br/gamedata/furnidata_json/1`
  - returns `307 Temporary Redirect`
  - redirects to `https://www.habbo.com.br/gamedata/furnidata_json/2c52fffcf732819970adb6210e58485c852e04ac`
  - final response is `200 OK`
  - final content type is `application/json;charset=UTF-8`

- `GET https://www.habbo.com.br/gamedata/hashes2`
  - returns `200 OK`
  - exposes the current hashes used by the modern game data loader

Why this matters:

- The loader in `Xabbo.Core` uses redirect-disabled `HttpClient` intentionally so it can inspect redirect targets and extract the hash.
- Before the fix made on 2026-04-02, the downloader only handled one redirect hop.
- If the request chain returned another redirect before a final `200 OK`, `EnsureSuccessStatusCode()` could still throw on `307`.
- The old code also kept the old cache file name even after the server revealed a newer hash, which made cache reuse less reliable.

## Code change documented

The game data loader was updated on 2026-04-02 to:

- follow multiple redirects with a safety limit
- update the game data hash from redirect targets and final `ETag`
- save the downloaded file using the final hash path
- keep the cache aligned with the hash that the server actually resolved

## Test coverage added

New unit coverage was added for a multi-redirect game data download flow to confirm:

- redirects are followed
- the final hash is returned
- the cache file is written under the final hash, not the original one

## Relevant local files

- `lib/core/src/Xabbo.Core/GameData/GameDataLoader.cs`
- `lib/core/src/Xabbo.Core.Tests/GameDataLoaderTests.cs`
- `src/Xabbo/Services/HabboApi.cs`
