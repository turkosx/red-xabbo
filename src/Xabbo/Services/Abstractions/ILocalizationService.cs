using Xabbo.Configuration;

namespace Xabbo.Services.Abstractions;

public interface ILocalizationService
{
    UiLanguage CurrentLanguage { get; }

    event Action? LanguageChanged;

    string this[string key] { get; }

    string Get(string key);
    void SetLanguage(UiLanguage language);
}
