using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public sealed class InventoryPageViewModel(
    InventoryViewModel inventory,
    ILocalizationService localizationService
) : PageViewModel(localizationService)
{
    public override string Header => T("page.inventory");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Box };

    public InventoryViewModel Inventory { get; } = inventory;
}
