using ReactiveUI;
using Splat;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private ILocalizationService? _localizer;

    protected ILocalizationService? Localizer => GetOrInitializeLocalizer();

    public string this[string key] => T(key);

    protected string T(string key) => Localizer?.Get(key) ?? key;

    protected void InitializeLocalization(ILocalizationService? localizationService = null)
    {
        var localizer = localizationService ?? Locator.Current.GetService<ILocalizationService>();
        if (localizer is not null)
            SubscribeToLocalization(localizer);
    }

    private ILocalizationService? GetOrInitializeLocalizer()
    {
        if (_localizer is null)
        {
            var localizer = Locator.Current.GetService<ILocalizationService>();
            if (localizer is not null)
                SubscribeToLocalization(localizer);
        }

        return _localizer;
    }

    private void SubscribeToLocalization(ILocalizationService localizer)
    {
        if (ReferenceEquals(_localizer, localizer))
            return;

        if (_localizer is not null)
            _localizer.LanguageChanged -= HandleLanguageChanged;

        _localizer = localizer;
        _localizer.LanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged() => OnLanguageChanged();

    protected virtual void OnLanguageChanged()
    {
        this.RaisePropertyChanged("Item");
        this.RaisePropertyChanged("Item[]");
    }

    internal void NotifyLocalizationChanged() => OnLanguageChanged();
}
