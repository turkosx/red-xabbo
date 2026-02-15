using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public class ProfilePageViewModel : PageViewModel
{
    public override string Header => T("page.profile");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Person };

    public ProfilePageViewModel(ILocalizationService localizationService)
        : base(localizationService)
    { }
}
