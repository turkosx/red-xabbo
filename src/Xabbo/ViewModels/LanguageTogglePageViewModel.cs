using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public sealed class LanguageTogglePageViewModel : PageViewModel
{
    public override string Header => T("settings.language.header");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Globe };
    public override bool SelectsOnInvoked => false;

    public LanguageTogglePageViewModel(ILocalizationService localizationService)
        : base(localizationService)
    {
    }
}
