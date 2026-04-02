# Stage 2 - Safe Furni Filter Parser

Date verified: 2026-04-02  
Timezone: America/Sao_Paulo

## Goal

Remove the vulnerable dynamic expression dependency from the room furni filter and replace it with a controlled parser that only supports safe, explicit filter operations.

## Problem

Before this stage:

- `src/Xabbo/ViewModels/Room/Furni/RoomFurniViewModel.cs` used `DynamicExpressionParser.ParseLambda(...)`
- `src/Xabbo/Xabbo.csproj` referenced `System.Linq.Dynamic.Core 1.4.5`

The filter text entered by the user through the room furni UI could reach the dynamic parser through the `where:` syntax.

## Code changes made

Updated:

- `src/Xabbo/ViewModels/Room/Furni/RoomFurniViewModel.cs`
- `src/Xabbo/Xabbo.csproj`

Added:

- `src/Xabbo/Utility/FurniFilterParser.cs`
- `src/Xabbo.Tests/Xabbo.Tests.csproj`
- `src/Xabbo.Tests/FurniFilterParserTests.cs`
- `src/Xabbo.Tests/Usings.cs`

## New filter behavior

The `where:` syntax is still supported, but now only through a safe parser with a fixed property/operator whitelist.

### Supported boolean operators

- `&&`
- `||`
- `!`
- `and`
- `or`
- `not`
- parentheses: `(` and `)`

### Supported comparison operators

- `==`
- `=`
- `!=`
- `>`
- `>=`
- `<`
- `<=`
- `contains`
- `startsWith`
- `endsWith`
- aliases: `~=`, `^=`, `$=`

### Supported literal values

- numbers
- quoted strings
- bare text values for string comparisons
- `true`
- `false`
- `null`

### Supported properties

- `id`
- `type`
- `kind`
- `identifier`
- `variant`
- `name`
- `description`
- `owner`
- `ownerId`
- `state`
- `hidden`
- `isHidden`
- `isFloorItem`
- `isWallItem`
- `x`
- `y`
- `z`
- `dir`
- `ltd`
- `wx`
- `wy`
- `lx`
- `ly`
- `isLeft`
- `isRight`
- `data`
- `count`

## Example filters

```text
where: type == floor && x >= 3 && y <= 6
where: owner contains "alice" && !hidden
where: isWallItem && isLeft && wx == 2
where: lx == null && ly == null
```

## Validation

Validated on 2026-04-02:

- `dotnet build Xabbo.sln`
- `dotnet test src/Xabbo.Tests/Xabbo.Tests.csproj`

Result:

- build succeeded
- parser unit tests passed

## Dependency impact

Confirmed after this stage:

- `System.Linq.Dynamic.Core` is no longer referenced by `src/Xabbo/Xabbo.csproj`

Remaining vulnerable transitives still reported for the app project:

- `SkiaSharp 2.88.3`
- `System.Text.Json 8.0.0`

Those are outside the scope of this stage and can be handled in later steps.
