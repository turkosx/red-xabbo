using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public class NavigatorPageViewModel : PageViewModel
{
    public override string Header => T("page.navigator");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Building };

    public NavigatorPageViewModel(ILocalizationService localizationService)
        : base(localizationService)
    { }
}
