using System.Reflection;
using FluentIcons.Common;
using FluentIcons.Avalonia.Fluent;
using Xabbo.Services.Abstractions;
using Xabbo.Utility;

using IconSource = FluentAvalonia.UI.Controls.IconSource;

namespace Xabbo.ViewModels;

public sealed class InfoPageViewModel : PageViewModel
{
    public override string Header => T("page.info");
    public override IconSource? Icon { get; } = new SymbolIconSource { Symbol = Symbol.Info };

    public string AppName => "redxabbo";
    public string ProjectName => "red-xabbo";
    public string DeveloperTag => "@tur.ko";
    public string DeveloperUrl => "https://github.com/turkosx";
    public string CreditApiUrl => "https://xabbo.io";
    public string CreditApiDisplay => "Xabbo APIs";
    public string CreditB7cUrl => "https://github.com/b7c";
    public string CreditB7cDisplay => "b7c";
    public string CreditQDavesUrl => "https://github.com/QDaves";
    public string CreditQDavesDisplay => "QDaves";
    public string CreditCommunityUrl => "https://discord.gg/JRxbfhuc3T";
    public string CreditCommunityDisplay => "Discord Community";
    public string CreditForksUrl => "https://github.com/xabbo/xabbo/forks";
    public string CreditForksDisplay => "Xabbo Forks";
    public string RepositoryUrl => "https://github.com/turkosx/red-xabbo";
    public string RepositoryDisplay => "github.com/turkosx/red-xabbo";
    public string IssuesUrl => "https://github.com/turkosx/red-xabbo/issues/new";

    public string Version { get; }
    public string XabboCommonVersion { get; }
    public string XabboGEarthVersion { get; }
    public string XabboMessagesVersion { get; }
    public string XabboCoreVersion { get; }

    public InfoPageViewModel(ILocalizationService localizationService)
        : base(localizationService)
    {
        Version = Assembly.GetEntryAssembly().GetVersionString();
        XabboCommonVersion = typeof(Xabbo.Client).Assembly.GetVersionString();
        XabboGEarthVersion = typeof(Xabbo.GEarth.GEarthExtension).Assembly.GetVersionString();
        XabboMessagesVersion = typeof(Xabbo.Messages.Flash.Out).Assembly.GetVersionString();
        XabboCoreVersion = typeof(Xabbo.Core.H).Assembly.GetVersionString();
    }
}
