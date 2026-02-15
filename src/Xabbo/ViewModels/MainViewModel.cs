using System.Reactive;
using System.Reactive.Linq;
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
    private bool _isUpdatingLanguage;

    private static readonly IReadOnlyList<LanguageOption> _languageOptions =
    [
        new(UiLanguage.English, "EN"),
        new(UiLanguage.PortugueseBrazil, "PT-BR")
    ];

    public List<PageViewModel> Pages { get; set; }
    public List<PageViewModel> FooterPages { get; set; }
    [Reactive] public PageViewModel? SelectedPage { get; set; }

    public AppConfig? Config => _config?.Value;

    public IReadOnlyList<LanguageOption> LanguageOptions => _languageOptions;
    [Reactive] public LanguageOption? SelectedLanguageOption { get; set; }
    public string LanguageToolTip => T("main.language.tooltip");
    public string ReportIssueText => T("main.error.reportIssueOn");
    public string GitHubText => T("main.error.github");

    [Reactive] public string? AppError { get; set; }

    public ReactiveCommand<Unit, Unit> ReportErrorCmd { get; }

    public MainViewModel()
    {
        Pages = [];
        FooterPages = [];
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
        InfoPageViewModel info,
        SettingsPageViewModel settings,
        ILauncherService launcher)
    {
        _config = config;
        _localizer = localizationService;
        _launcher = launcher;

        Pages = [general, wardrobe, inventory, friends, chat, room, gameData, info, settings];
        FooterPages = [];
        SelectedPage = general;

        SyncSelectedLanguageFromService();

        this.WhenAnyValue(x => x.SelectedLanguageOption)
            .Where(option => option is not null)
            .Select(option => option!.Value)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ApplySelectedLanguage);

        _localizer.LanguageChanged += OnLanguageChanged;

        ReportErrorCmd = ReactiveCommand.Create(ReportError);
    }

    private void ApplySelectedLanguage(UiLanguage language)
    {
        if (_isUpdatingLanguage)
            return;

        _isUpdatingLanguage = true;
        try
        {
            if (_config is not null && _config.Value.General.Language != language)
                _config.Value.General.Language = language;

            _localizer?.SetLanguage(language);
        }
        finally
        {
            _isUpdatingLanguage = false;
        }
    }

    private void SyncSelectedLanguageFromService()
    {
        if (_localizer is null)
            return;

        _isUpdatingLanguage = true;
        try
        {
            SelectedLanguageOption = _languageOptions.FirstOrDefault(x => x.Value == _localizer.CurrentLanguage)
                ?? _languageOptions.First();
        }
        finally
        {
            _isUpdatingLanguage = false;
        }
    }

    private void OnLanguageChanged()
    {
        SyncSelectedLanguageFromService();
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
