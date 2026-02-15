using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
    protected ILocalizationService Localizer { get; }

    protected PageViewModel(ILocalizationService localizationService)
    {
        Localizer = localizationService;
        Localizer.LanguageChanged += HandleLanguageChanged;
    }

    protected string T(string key) => Localizer.Get(key);

    private void HandleLanguageChanged() => OnLanguageChanged();
    protected virtual void OnLanguageChanged()
    {
        this.RaisePropertyChanged(nameof(Header));
        this.RaisePropertyChanged("Item[]");
    }

    public abstract string Header { get; }
    public abstract IconSource? Icon { get; }
}
