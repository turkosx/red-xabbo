using Xabbo.Messages.Flash;
using Xabbo.Extension;
using Xabbo.Messages;
using Xabbo.Core;
using Xabbo.Core.Game;
using Xabbo.Core.Messages.Outgoing;
using Xabbo.Core.Messages.Incoming;
using Xabbo.Services.Abstractions;
using Xabbo.Configuration;
using Xabbo.Controllers;

namespace Xabbo.Components;

[Intercept]
public partial class ClickToController(
    IExtension extension,
    IConfigProvider<AppConfig> config,
    XabbotComponent xabbot,
    RoomModerationController moderation,
    ProfileManager profileManager,
    FriendManager friendManager,
    RoomManager roomManager)
:
    ControllerBase(extension)
{
    private readonly XabbotComponent _xabbot = xabbot;
    private readonly IConfigProvider<AppConfig> _config = config;
    private readonly RoomModerationController _moderation = moderation;
    private readonly ProfileManager _profileManager = profileManager;
    private readonly FriendManager _friendManager = friendManager;
    private readonly RoomManager _roomManager = roomManager;
    private readonly HashSet<Id> _blockedUserIds = [];
    private readonly HashSet<Id> _silencedUserIds = [];
    private Id _lastClickToUserId = -1;
    private long _lastClickToTimestamp;
    private static readonly Identifier[] BlockUserIdentifiers = [
        (Direction.Out, "BlockUser"),
        (Direction.Out, "BlockUserMessageComposer"),
    ];
    private static readonly Identifier[] UnblockUserIdentifiers = [
        (Direction.Out, "UnblockUser"),
        (Direction.Out, "UnblockUserMessageComposer"),
    ];

    private AppConfig Config => _config.Value;
    private const int ClickToDedupMs = 300;

    [Reactive] public bool Enabled { get; set; }

    [Reactive] public bool Mute { get; set; } = true;
    [Reactive] public int MuteValue { get; set; } = 1;
    [Reactive] public bool MuteInMinutes { get; set; }
    [Reactive] public bool MuteInHours { get; set; } = true;

    [Reactive] public bool Kick { get; set; }
    [Reactive] public bool Ban { get; set; }
    [Reactive] public bool BanHour { get; set; } = true;
    [Reactive] public bool BanDay { get; set; }
    [Reactive] public bool BanPerm { get; set; }

    [Reactive] public bool Bounce { get; set; }
    [Reactive] public bool BlockToggle { get; set; }
    [Reactive] public bool SilenceToggle { get; set; }

    [Intercept(~ClientType.Shockwave)]
    [InterceptOut(nameof(Out.GetSelectedBadges))]
    void OnGetSelectedBadges(Intercept e)
    {
        if (!Enabled)
            return;

        HandleClickUserById(e.Packet.Read<Id>());
    }

    [Intercept(~ClientType.Shockwave)]
    [InterceptOut(nameof(Out.GetExtendedProfile))]
    void OnGetExtendedProfile(Intercept e)
    {
        if (!Enabled || e.Packet.Length < 5)
            return;

        Id userId = e.Packet.Read<Id>();
        bool open = e.Packet.Read<bool>();

        if (open)
            return;

        HandleClickUserById(userId);
    }

    [Intercept(~ClientType.Shockwave)]
    [InterceptOut(nameof(Out.GetRelationshipStatusInfo))]
    void OnGetRelationshipStatusInfo(Intercept e)
    {
        if (!Enabled || e.Packet.Length < 4)
            return;

        HandleClickUserById(e.Packet.Read<Id>());
    }

    private bool CanClickTo(IUser user) =>
        user.Id != _profileManager.UserData?.Id &&
        user.Name != _profileManager.UserData?.Name &&
        (!Config.General.ClickToIgnoresFriends || !_friendManager.IsFriend(user.Id));

    private void HandleClickUserById(Id userId)
    {
        if (userId < 0 || !_roomManager.EnsureInRoom(out IRoom? room))
            return;

        long now = Environment.TickCount64;
        if (_lastClickToUserId == userId &&
            now - _lastClickToTimestamp >= 0 &&
            now - _lastClickToTimestamp < ClickToDedupMs)
        {
            return;
        }

        _lastClickToUserId = userId;
        _lastClickToTimestamp = now;
        HandleClickUser(room.GetAvatarById<IUser>(userId));
    }

    // Use LookTo on Shockwave as it doesn't have GetSelectedBadges
    // TODO: allow [Intercept(ClientType.Shockwave)] in Xabbo.Common.Generator
    // to filter a message to certain client types
    [Intercept]
    void HandleLookTo(Intercept e, LookToMsg lookTo)
    {
        if (!Enabled || Session.Is(ClientType.Modern)) return;

        if (!_roomManager.EnsureInRoom(out IRoom? room))
            return;

        var usersOnTile = room.Avatars
            .OfType<IUser>()
            .Where(user =>
                user.Location == lookTo.Point &&
                CanClickTo(user)
            )
            .ToArray();

        if (usersOnTile is [ IUser user ])
        {
            e.Block();
            HandleClickUser(user);
        }
        else if (usersOnTile.Length > 1)
        {
            _xabbot.ShowMessage($"Cannot click-to: more than one user on tile.");
        }
    }

    private void HandleClickUser(IUser? user)
    {
        if (!Enabled) return;

        if (!_roomManager.EnsureInRoom(out IRoom? room))
            return;

        // Ignore self.
        if (user is null ||
            user.Id == _profileManager.UserData?.Id ||
            user.Name == _profileManager.UserData?.Name)
        {
            return;
        }

        // Ignore friends if configured.
        if (Config.General.ClickToIgnoresFriends && _friendManager.IsFriend(user.Id))
            return;

        if (BlockToggle)
        {
            ToggleUserBlock(user);
        }
        else if (SilenceToggle)
        {
            ToggleUserSilence(user);
        }
        else if (Mute)
        {
            if (!_moderation.CanMute)
                return;

            int muteMinutes = MuteValue;
            if (MuteInHours)
                muteMinutes *= 60;

            if (muteMinutes < 0)
                muteMinutes = 0;
            if (muteMinutes > 30000)
                muteMinutes = 30000;

            SendInfoMessage(GetClickToMuteInfoMessage(), user.Index);
            Send(new MuteUserMsg(user.Id, room.Id, muteMinutes));
        }
        else if (Kick)
        {
            if (!_roomManager.CanKick)
                return;

            SendInfoMessage(this["general.clickTo.info.kicking"], user.Index);
            Send(new KickUserMsg(user.Id, user.Name));
        }
        else if (Ban)
        {
            if (!_roomManager.CanBan)
                return;

            BanDuration duration;
            string banInfoKey;

            if (BanDay)
            {
                duration = BanDuration.Day;
                banInfoKey = "general.clickTo.info.banningDay";
            }
            else if (BanHour)
            {
                duration = BanDuration.Hour;
                banInfoKey = "general.clickTo.info.banningHour";
            }
            else if (BanPerm)
            {
                duration = BanDuration.Permanent;
                banInfoKey = "general.clickTo.info.banningPermanent";
            }
            else
                return;

            SendInfoMessage(this[banInfoKey], user.Index);
            Send(new BanUserMsg(user.Id, user.Name, room.Id, duration));
        }
        else if (Bounce)
        {
            if (!_roomManager.IsOwner)
                return;

            Task.Run(async () => {
                SendInfoMessage(this["general.clickTo.info.bouncing"], user.Index);
                Send(new BanUserMsg(user.Id, user.Name, room.Id, BanDuration.Hour));
                if (Config.General.BounceUnbanDelay > 0)
                    await Task.Delay(Config.General.BounceUnbanDelay);
                Send(Out.UnbanUserFromRoom, user.Id, room.Id);
            });
        }

    }

    private void ToggleUserBlock(IUser user)
    {
        // Block/Unblock packets are only available for modern clients.
        if (Session.Is(ClientType.Shockwave) || user.Id < 0)
            return;

        if (_blockedUserIds.Contains(user.Id))
        {
            if (!TrySendClickToPacket(UnblockUserIdentifiers, user.Id))
            {
                SendInfoMessage(this["general.clickTo.info.blockUnavailable"], user.Index);
                return;
            }

            _blockedUserIds.Remove(user.Id);
            SendInfoMessage(this["general.clickTo.info.unblocking"], user.Index);
        }
        else
        {
            if (!TrySendClickToPacket(BlockUserIdentifiers, user.Id))
            {
                SendInfoMessage(this["general.clickTo.info.blockUnavailable"], user.Index);
                return;
            }

            _blockedUserIds.Add(user.Id);
            SendInfoMessage(this["general.clickTo.info.blocking"], user.Index);
        }
    }

    private void ToggleUserSilence(IUser user)
    {
        // Ignore/Unignore packets are only available for modern clients.
        if (Session.Is(ClientType.Shockwave) || user.Id < 0)
            return;

        if (_silencedUserIds.Remove(user.Id))
        {
            SendInfoMessage(this["general.clickTo.info.unsilencing"], user.Index);
            Send(Out.UnignoreUser, user.Id);
        }
        else
        {
            _silencedUserIds.Add(user.Id);
            SendInfoMessage(this["general.clickTo.info.silencing"], user.Index);
            Send(Out.IgnoreUser, user.Id);
        }
    }

    private bool TrySendClickToPacket(ReadOnlySpan<Identifier> identifiers, Id userId)
    {
        foreach (Identifier identifier in identifiers)
        {
            if (Ext.Messages.TryGetHeader(identifier, out Header header))
            {
                Send(header, userId);
                return true;
            }
        }

        return false;
    }

    private string GetClickToMuteInfoMessage()
    {
        string unit = MuteInMinutes
            ? (MuteValue == 1 ? this["general.clickTo.info.minute"] : this["general.clickTo.info.minutes"])
            : (MuteValue == 1 ? this["general.clickTo.info.hour"] : this["general.clickTo.info.hours"]);

        return string.Format(this["general.clickTo.info.mutingFor"], MuteValue, unit);
    }

    private void SendInfoMessage(string message, int avatarIndex = -1) => Send(new AvatarWhisperMsg(message, avatarIndex));
}
