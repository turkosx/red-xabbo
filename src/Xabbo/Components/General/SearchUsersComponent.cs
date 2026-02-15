using System.Globalization;
using System.Linq;
using System.Reactive;
using ReactiveUI;

using Xabbo.Core;
using Xabbo.Core.Game;
using Xabbo.Core.Tasks;
using Xabbo.Extension;
using Xabbo.Messages.Flash;
using Xabbo.Services.Abstractions;

namespace Xabbo.Components;

[Intercept(ClientType.Modern)]
public partial class SearchUsersComponent : Component
{
    private static readonly string[] _categories = ["popular", "top_promotions", "new_ads"];

    private readonly ILocalizationService _localizer;
    private readonly RoomManager _roomManager;
    private readonly object _sync = new();
    private readonly HashSet<Id> _seenRoomIds = [];
    private readonly List<RoomInfo> _rooms = [];

    private CancellationTokenSource? _searchCancellation;
    private TaskCompletionSource<bool>? _roomScanSignal;
    private bool _capturePackets;
    private bool _captureNavigator;
    private bool _awaitingUsers;

    private Id _currentRoomId = -1;
    private string _currentRoomName = "";
    private string _targetNormalized = "";

    private Id _foundRoomId = -1;
    private string? _statusKey;
    private object[] _statusArgs = [];

    [Reactive] public string TargetUser { get; set; } = "";
    [Reactive] public bool IsSearching { get; private set; }
    [Reactive] public string Status { get; private set; } = "";
    [Reactive] public string FoundUser { get; private set; } = "";
    [Reactive] public string FoundRoom { get; private set; } = "";
    [Reactive] public int RoomsQueued { get; private set; }
    [Reactive] public int RoomsScanned { get; private set; }

    public bool HasResult => !string.IsNullOrWhiteSpace(FoundUser) && !string.IsNullOrWhiteSpace(FoundRoom);

    public ReactiveCommand<Unit, Unit> StartCmd { get; }
    public ReactiveCommand<Unit, Unit> StopCmd { get; }

    public SearchUsersComponent(
        IExtension extension,
        ILocalizationService localizer,
        RoomManager roomManager)
        : base(extension)
    {
        _localizer = localizer;
        _roomManager = roomManager;
        _localizer.LanguageChanged += OnLanguageChanged;

        StartCmd = ReactiveCommand.Create(StartSearch);
        StopCmd = ReactiveCommand.Create(StopSearch);

        ResetResult();
        SetStatus("general.search.status.idle");
    }

    protected override void OnConnected(ConnectedEventArgs e)
    {
        base.OnConnected(e);
        IsAvailable = IsAvailable && Session.Is(ClientType.Modern);
        SetStatus(IsAvailable
            ? "general.search.status.idle"
            : "general.search.status.unavailable");
    }

    protected override void OnDisconnected()
    {
        StopSearch();
        ResetResult();
        SetStatus("general.search.status.idle");
        base.OnDisconnected();
    }

    private void StartSearch()
    {
        if (!IsAvailable || IsSearching)
            return;

        string target = (TargetUser ?? "").Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            SetStatus("general.search.status.enterUser");
            return;
        }

        StopSearch();
        ResetResult();

        _targetNormalized = target.ToLowerInvariant();
        _currentRoomId = -1;
        _currentRoomName = "";
        _foundRoomId = -1;
        RoomsQueued = 0;
        RoomsScanned = 0;
        _awaitingUsers = false;

        lock (_sync)
        {
            _seenRoomIds.Clear();
            _rooms.Clear();
        }

        _capturePackets = true;
        IsSearching = true;

        _searchCancellation = new CancellationTokenSource();
        _ = Task.Run(() => SearchAsync(target, _searchCancellation.Token));
    }

    private void StopSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;

        _capturePackets = false;
        _captureNavigator = false;
        _awaitingUsers = false;
        _roomScanSignal?.TrySetResult(true);

        if (IsSearching)
            SetStatus("general.search.status.stopped");

        IsSearching = false;
    }

    private async Task SearchAsync(string target, CancellationToken cancellationToken)
    {
        Id roomToEnter = -1;

        try
        {
            SetStatus("general.search.status.collecting");

            _captureNavigator = true;
            foreach (string category in _categories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Ext.Send(Out.NewNavigatorSearch, category, "");
                await Task.Delay(2500, cancellationToken);
            }

            await Task.Delay(500, cancellationToken);
            _captureNavigator = false;

            RoomInfo[] roomsToScan;
            lock (_sync)
            {
                roomsToScan = _rooms
                    .Where(room => room.IsOpen && room.Users >= 1)
                    .ToArray();
            }

            RoomsQueued = roomsToScan.Length;
            if (roomsToScan.Length == 0)
            {
                SetStatus("general.search.status.notFound", target);
                return;
            }

            SetStatus("general.search.status.scanning", roomsToScan.Length);

            for (int i = 0; i < roomsToScan.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RoomInfo room = roomsToScan[i];
                _currentRoomId = room.Id;
                _currentRoomName = room.Name;
                RoomsScanned = i + 1;

                SetStatus("general.search.status.scanningRoom", room.Name, RoomsScanned, RoomsQueued);

                _roomScanSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _awaitingUsers = false;

                EnterRoomTask.Result enterResult = await new EnterRoomTask(Ext, room.Id)
                    .ExecuteAsync(10000, cancellationToken);
                if (enterResult is not EnterRoomTask.Result.Success)
                {
                    await Task.Delay(500, cancellationToken);
                    continue;
                }

                if (TryFindUserInCurrentRoom())
                {
                    roomToEnter = _foundRoomId;
                    break;
                }

                _awaitingUsers = true;

                Task completed = await Task.WhenAny(
                    _roomScanSignal.Task,
                    Task.Delay(6000, cancellationToken)
                );
                _awaitingUsers = false;

                if (completed == _roomScanSignal.Task && _foundRoomId > 0)
                {
                    roomToEnter = _foundRoomId;
                    break;
                }

                await Task.Delay(500, cancellationToken);
            }

            if (_foundRoomId > 0)
            {
                SetStatus("general.search.status.found", FoundUser, FoundRoom);
            }
            else
            {
                SetStatus("general.search.status.notFound", target);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when search is stopped manually.
        }
        catch (Exception)
        {
            SetStatus("general.search.status.error");
        }
        finally
        {
            _capturePackets = false;
            _captureNavigator = false;
            _awaitingUsers = false;
            IsSearching = false;
            _roomScanSignal?.TrySetResult(true);
            _roomScanSignal = null;
        }

        if (roomToEnter > 0 && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Re-enter the found room so the client can fully load room data/assets.
                await new EnterRoomTask(Ext, roomToEnter).ExecuteAsync(10000, cancellationToken);
            }
            catch
            {
                // Ignore re-entry failures; search result is already available.
            }
        }
    }

    [InterceptIn(nameof(In.Users))]
    private void HandleUsers(Intercept e)
    {
        if (!IsSearching || !_awaitingUsers)
            return;

        int packetPosition = e.Packet.Position;
        try
        {
            Avatar[] avatars = e.Packet.Read<Avatar[]>();

            User? user = avatars
                .OfType<User>()
                .FirstOrDefault(avatar =>
                    avatar.Name.Equals(_targetNormalized, StringComparison.OrdinalIgnoreCase));

            if (user is not null)
            {
                SetFound(user.Name, _currentRoomId, _currentRoomName);
            }

            _roomScanSignal?.TrySetResult(true);
        }
        finally
        {
            e.Packet.Position = packetPosition;
        }
    }

    [InterceptIn(nameof(In.NavigatorSearchResultBlocks))]
    private void HandleNavigatorSearchResultBlocks(Intercept e)
    {
        if (!IsSearching || !_captureNavigator)
            return;

        int packetPosition = e.Packet.Position;
        try
        {
            NavigatorSearchResults results = e.Packet.Read<NavigatorSearchResults>();
            lock (_sync)
            {
                foreach (RoomInfo room in results.GetRooms().Where(r => r.Users >= 1))
                {
                    if (_seenRoomIds.Add(room.Id))
                        _rooms.Add(room);
                }
            }

            e.Block();
        }
        catch
        {
            // Ignore malformed navigator blocks while searching.
        }
        finally
        {
            e.Packet.Position = packetPosition;
        }
    }

    [InterceptOut(nameof(Out.Chat))]
    private void HandleChatCommand(Intercept e)
    {
        string text = e.Packet.Read<string>();
        string lower = text.Trim().ToLowerInvariant();

        if (lower.StartsWith(":search --user "))
        {
            string target = text[":search --user ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(target))
            {
                TargetUser = target;
                StartSearch();
            }
            else
            {
                SetStatus("general.search.status.enterUser");
            }

            e.Block();
        }
        else if (lower == ":search --stop")
        {
            StopSearch();
            e.Block();
        }
    }

    [InterceptIn(
        "f:" + nameof(In.Objects),
        nameof(In.Items),
        nameof(In.Chat),
        nameof(In.Shout),
        nameof(In.AvatarEffect),
        nameof(In.Whisper),
        nameof(In.UserTyping),
        nameof(In.Dance),
        nameof(In.Sleep),
        nameof(In.ObjectDataUpdate),
        nameof(In.ObjectsDataUpdate),
        nameof(In.CarryObject),
        nameof(In.TraxSongInfo),
        nameof(In.HabboGroupBadges),
        nameof(In.UserUpdate),
        nameof(In.UserRemove)
    )]
    private void HandleAntiLag(Intercept e)
    {
        if (_capturePackets)
            e.Block();
    }

    private bool TryFindUserInCurrentRoom()
    {
        if (!_roomManager.EnsureInRoom(out IRoom? room))
            return false;

        IUser? user = room.Avatars
            .OfType<IUser>()
            .FirstOrDefault(x =>
                x.Name.Equals(_targetNormalized, StringComparison.OrdinalIgnoreCase));

        if (user is null)
            return false;

        string roomName = room.Data?.Name ?? _currentRoomName;
        SetFound(user.Name, _currentRoomId, roomName);
        return true;
    }

    private void SetFound(string userName, Id roomId, string roomName)
    {
        _foundRoomId = roomId;
        FoundUser = userName;
        FoundRoom = string.IsNullOrWhiteSpace(roomName)
            ? $"#{roomId}"
            : $"{roomName} (#{roomId})";
        this.RaisePropertyChanged(nameof(HasResult));
    }

    private void ResetResult()
    {
        FoundUser = "";
        FoundRoom = "";
        this.RaisePropertyChanged(nameof(HasResult));
    }

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        UpdateStatusText();
    }

    private void OnLanguageChanged() => UpdateStatusText();

    private void UpdateStatusText()
    {
        if (string.IsNullOrWhiteSpace(_statusKey))
            return;

        string template = _localizer.Get(_statusKey);
        Status = _statusArgs.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, _statusArgs);
    }
}
