using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Core.Game;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public class RoomPageViewModel(
    ILocalizationService localizationService,
    RoomManager roomManager,
    RoomInfoViewModel info,
    RoomAvatarsViewModel avatars,
    RoomVisitorsViewModel visitors,
    RoomBansViewModel bans,
    RoomFurniViewModel furni
)
    : PageViewModel(localizationService)
{
    public override string Header => T("page.room");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Home };

    public RoomManager RoomManager { get; } = roomManager;
    public RoomInfoViewModel Info { get; } = info;
    public RoomAvatarsViewModel Avatars { get; } = avatars;
    public RoomVisitorsViewModel Visitors { get; } = visitors;
    public RoomBansViewModel Bans { get; } = bans;
    public RoomFurniViewModel Furni { get; } = furni;
}
