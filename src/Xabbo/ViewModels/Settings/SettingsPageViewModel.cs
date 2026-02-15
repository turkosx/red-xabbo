using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Xabbo.Configuration;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

public sealed class SettingsPageViewModel : PageViewModel
{
    private readonly IConfigProvider<AppConfig> _config;

    public override string Header => T("page.settings");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Settings };

    public AppConfig Config => _config.Value;

    public string ChatLogHeader => T("settings.chatlog.header");
    public string TimingHeader => T("settings.timing.header");
    public string TimingDescription => T("settings.timing.description");
    public string RoomFurniViewHeader => T("settings.roomFurni.header");
    public string DebugHeader => T("settings.debug.header");

    public SettingsPageViewModel(
        IConfigProvider<AppConfig> config,
        ILocalizationService localizationService)
        : base(localizationService)
    {
        _config = config;
    }

    protected override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        this.RaisePropertyChanged(nameof(ChatLogHeader));
        this.RaisePropertyChanged(nameof(TimingHeader));
        this.RaisePropertyChanged(nameof(TimingDescription));
        this.RaisePropertyChanged(nameof(RoomFurniViewHeader));
        this.RaisePropertyChanged(nameof(DebugHeader));
    }
}
