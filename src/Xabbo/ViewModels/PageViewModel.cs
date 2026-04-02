using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
    protected PageViewModel(ILocalizationService localizationService)
    {
        InitializeLocalization(localizationService);
    }

    protected override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        this.RaisePropertyChanged(nameof(Header));
    }

    public virtual bool SelectsOnInvoked => true;
    public abstract string Header { get; }
    public abstract IconSource? Icon { get; }
}
