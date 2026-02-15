using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public class GameDataPageViewModel(
    ILocalizationService localizationService,
    FurniDataViewModel furniData,
    ExternalTextsViewModel texts,
    ExternalVariablesViewModel variables
)
    : PageViewModel(localizationService)
{
    public override string Header => T("page.gameData");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Database };

    public FurniDataViewModel FurniData { get; } = furniData;
    public ExternalTextsViewModel Texts { get; } = texts;
    public ExternalVariablesViewModel Variables { get; } = variables;
}
