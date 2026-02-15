using System.Reactive;
using ReactiveUI;
using Splat;
using Xabbo.Configuration;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

public class MainViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Title => "redxabbo";
#pragma warning restore CA1822 // Mark members as static

    private readonly ILauncherService _launcher;
    private readonly IConfigProvider<AppConfig>? _config;
    private readonly ILocalizationService? _localizer;

    public List<PageViewModel> Pages { get; set; }
    public List<PageViewModel> FooterPages { get; set; }
    [Reactive] public PageViewModel? SelectedPage { get; set; }

    public AppConfig? Config => _config?.Value;

    public string LanguageToolTip => _localizer is null
        ? T("main.language.tooltip")
        : $"{T("main.language.tooltip")} ({(_localizer.CurrentLanguage == UiLanguage.English ? "EN" : "PT-BR")})";
    public string ReportIssueText => T("main.error.reportIssueOn");
    public string GitHubText => T("main.error.github");

    [Reactive] public string? AppError { get; set; }

    public ReactiveCommand<Unit, Unit> ToggleLanguageCmd { get; }
    public ReactiveCommand<Unit, Unit> ReportErrorCmd { get; }

    public MainViewModel()
    {
        Pages = [];
        FooterPages = [];
        ToggleLanguageCmd = null!;
        ReportErrorCmd = null!;
        _launcher = null!;
    }

    [DependencyInjectionConstructor]
    public MainViewModel(
        IConfigProvider<AppConfig> config,
        ILocalizationService localizationService,
        GeneralPageViewModel general,
        WardrobePageViewModel wardrobe,
        InventoryPageViewModel inventory,
        FriendsPageViewModel friends,
        ChatPageViewModel chat,
        RoomPageViewModel room,
        GameDataPageViewModel gameData,
        LanguageTogglePageViewModel languageToggle,
        InfoPageViewModel info,
        SettingsPageViewModel settings,
        ILauncherService launcher)
    {
        _config = config;
        _localizer = localizationService;
        _launcher = launcher;

        Pages = [general, wardrobe, inventory, friends, chat, room, gameData];
        FooterPages = [languageToggle, info, settings];
        SelectedPage = general;

        _localizer.LanguageChanged += OnLanguageChanged;

        ToggleLanguageCmd = ReactiveCommand.Create(ToggleLanguage);
        ReportErrorCmd = ReactiveCommand.Create(ReportError);
    }

    private void ApplySelectedLanguage(UiLanguage language)
    {
        if (_config is not null && _config.Value.General.Language != language)
            _config.Value.General.Language = language;
        _localizer?.SetLanguage(language);
    }

    private void ToggleLanguage()
    {
        UiLanguage current = _localizer?.CurrentLanguage ?? _config?.Value.General.Language ?? UiLanguage.English;
        UiLanguage next = current == UiLanguage.English
            ? UiLanguage.PortugueseBrazil
            : UiLanguage.English;
        ApplySelectedLanguage(next);
    }

    private void OnLanguageChanged()
    {
        this.RaisePropertyChanged(nameof(LanguageToolTip));
        this.RaisePropertyChanged(nameof(ReportIssueText));
        this.RaisePropertyChanged(nameof(GitHubText));
    }

    private string T(string key)
    {
        if (_localizer is null)
            return key;

        return _localizer.Get(key);
    }

    private void ReportError()
    {
        _launcher.Launch("https://github.com/turkosx/red-xabbo/issues/new", new()
        {
            ["body"] = [$"(describe the issue here)\n\n```txt\n{AppError}\n```"]
        });
    }
}
