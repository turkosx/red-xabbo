using ReactiveUI;
using Splat;
using Xabbo.Extension;
using Xabbo.Interceptor;
using Xabbo.Messages;
using Xabbo.Services.Abstractions;

namespace Xabbo.Controllers;

public abstract partial class ControllerBase : ReactiveObject, IInterceptorContext
{
    private readonly IExtension _extension;
    private readonly ILocalizationService? _localizer;

    IInterceptor IInterceptorContext.Interceptor => _extension;

    protected Session Session => _extension.Session;

    protected IExtension Ext => _extension;
    public string this[string key] => _localizer?.Get(key) ?? key;

    public ControllerBase(IExtension extension)
    {
        _extension = extension;
        _localizer = Locator.Current.GetService<ILocalizationService>();
        if (_localizer is not null)
            _localizer.LanguageChanged += OnLanguageChanged;
        _extension.Connected += OnConnected;
    }

    protected virtual void OnConnected(ConnectedEventArgs e)
    {
        if (this is IMessageHandler handler)
            handler.Attach(_extension);
    }

    private void OnLanguageChanged() => this.RaisePropertyChanged("Item[]");
}
