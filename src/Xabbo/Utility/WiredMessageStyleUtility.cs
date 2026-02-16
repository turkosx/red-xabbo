using System;
using System.Collections.Generic;
using Xabbo.Core;
using Xabbo.Core.GameData;

namespace Xabbo.Utility;

public static class WiredMessageStyleUtility
{
    public const int LegacyWiredMessageStyle = 34;

    private const string WiredMessageStylePrefix = "wiredfurni.params.show_message.style_selection.";

    public static HashSet<int> GetWiredMessageStyles(ExternalTexts? texts)
    {
        HashSet<int> styles = new() { LegacyWiredMessageStyle };

        if (texts is null)
            return styles;

        foreach (var (key, _) in texts)
        {
            if (key.StartsWith(WiredMessageStylePrefix, StringComparison.Ordinal) &&
                int.TryParse(key[WiredMessageStylePrefix.Length..], out int style))
            {
                styles.Add(style);
            }
        }

        return styles;
    }

    public static bool IsWiredMessage(ChatType chatType, int bubbleStyle, HashSet<int> wiredMessageStyles) =>
        chatType == ChatType.Whisper && wiredMessageStyles.Contains(bubbleStyle);
}
