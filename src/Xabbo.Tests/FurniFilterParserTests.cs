using Xabbo.Core;
using Xabbo.Utility;
using Xabbo.ViewModels;

namespace Xabbo.Tests;

public class FurniFilterParserTests
{
    [Fact]
    public void Parse_FloorFilterWithNumericAndBooleanComparisons_MatchesExpectedItem()
    {
        FurniViewModel floor = CreateFloorViewModel();
        FurniViewModel wall = CreateWallViewModel();

        var filter = FurniFilterParser.Parse("type == floor && id == 1 && ownerId == 99 && x >= 3 && y == 4 && hidden == false");

        Assert.True(filter(floor));
        Assert.False(filter(wall));
    }

    [Fact]
    public void Parse_StringOperatorsAndBareBoolean_MatchExpectedItem()
    {
        FurniViewModel floor = CreateFloorViewModel();
        FurniViewModel wall = CreateWallViewModel();

        var filter = FurniFilterParser.Parse("owner contains alice && identifier startsWith chair && !hidden");

        Assert.True(filter(floor));
        Assert.False(filter(wall));
    }

    [Fact]
    public void Parse_NullComparisonForWallSpecificCoordinates_MatchesFloorItem()
    {
        FurniViewModel floor = CreateFloorViewModel();
        FurniViewModel wall = CreateWallViewModel();

        var filter = FurniFilterParser.Parse("lx == null && ly == null");

        Assert.True(filter(floor));
        Assert.False(filter(wall));
    }

    [Fact]
    public void Parse_WallFilterWithParentheses_MatchesExpectedItem()
    {
        FurniViewModel wall = CreateWallViewModel();
        FurniViewModel floor = CreateFloorViewModel();

        var filter = FurniFilterParser.Parse("(isWallItem && isLeft) && wx == 2 && data contains \"poster\"");

        Assert.True(filter(wall));
        Assert.False(filter(floor));
    }

    [Fact]
    public void Parse_UnknownProperty_ThrowsHelpfulError()
    {
        var ex = Assert.Throws<FurniFilterParser.FurniFilterParseException>(() => FurniFilterParser.Parse("unknownProp == 1"));

        Assert.Contains("Unknown property", ex.Message);
        Assert.Contains("Supported properties", ex.Message);
    }

    private static FurniViewModel CreateFloorViewModel()
    {
        FloorItem item = new()
        {
            Id = 1,
            Kind = 123,
            Identifier = "chair_plasto",
            OwnerId = 99,
            OwnerName = "Alice",
            IsHidden = false,
            Location = new Tile(3, 4, 1.5f),
            Direction = 2,
            Data = new LegacyData { Value = "42" }
        };

        return new FurniViewModel(item) { Count = 2 };
    }

    private static FurniViewModel CreateWallViewModel()
    {
        WallItem item = new()
        {
            Id = 2,
            Kind = 321,
            Identifier = "poster",
            OwnerId = 7,
            OwnerName = "Bob",
            IsHidden = true,
            Location = WallLocation.ParseString(":w=2,3 l=4,5 l"),
            Data = "poster data"
        };

        return new FurniViewModel(item) { Count = 1 };
    }
}
