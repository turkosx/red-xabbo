using ReactiveUI;
using Splat;
using Xabbo.Services.Abstractions;

namespace Xabbo.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public string this[string key] => Locator.Current.GetService<ILocalizationService>()?.Get(key) ?? key;
}
