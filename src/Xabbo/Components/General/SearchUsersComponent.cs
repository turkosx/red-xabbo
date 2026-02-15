using System.Globalization;
using System.Linq;
using System.Reactive;
using ReactiveUI;

using Xabbo.Core;
using Xabbo.Core.Game;
using Xabbo.Extension;
using Xabbo.Messages.Flash;
using Xabbo.Services.Abstractions;

namespace Xabbo.Components;

[Intercept(ClientType.Modern)]
public partial class SearchUsersComponent : Component
{
    private static readonly string[] _categories = ["popular", "top_promotions", "new_ads"];

    private readonly ILocalizationService _localizer;
    private readonly object _sync = new();
    private readonly HashSet<Id> _seenRoomIds = [];
    private readonly List<RoomInfo> _rooms = [];

    private CancellationTokenSource? _searchCancellation;
    private TaskCompletionSource<bool>? _roomScanSignal;
    private bool _capturePackets;
    private bool _captureNavigator;

    private Id _currentRoomId = -1;
    private string _currentRoomName = "";
    private string _targetNormalized = "";

    private Id _foundRoomId = -1;

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
        ILocalizationService localizer)
        : base(extension)
    {
        _localizer = localizer;

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
                await Task.Delay(2000, cancellationToken);
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

                Ext.Send(Out.GetGuestRoom, room.Id, 0, 1);

                Task completed = await Task.WhenAny(
                    _roomScanSignal.Task,
                    Task.Delay(3000, cancellationToken)
                );

                if (completed == _roomScanSignal.Task && _foundRoomId > 0)
                {
                    roomToEnter = _foundRoomId;
                    break;
                }
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
            IsSearching = false;
            _roomScanSignal?.TrySetResult(true);
            _roomScanSignal = null;
        }

        if (roomToEnter > 0 && !cancellationToken.IsCancellationRequested)
            Ext.Send(Out.GetGuestRoom, roomToEnter, 0, 1);
    }

    [InterceptIn(nameof(In.Users))]
    private void HandleUsers(Intercept e)
    {
        if (!IsSearching)
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
                _foundRoomId = _currentRoomId;
                FoundUser = user.Name;
                FoundRoom = string.IsNullOrWhiteSpace(_currentRoomName)
                    ? $"#{_currentRoomId}"
                    : $"{_currentRoomName} (#{_currentRoomId})";
                this.RaisePropertyChanged(nameof(HasResult));
            }

            _roomScanSignal?.TrySetResult(true);
            e.Block();
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
        nameof(In.UserRemove),
        nameof(In.GetGuestRoomResult)
    )]
    private void HandleAntiLag(Intercept e)
    {
        if (_capturePackets)
            e.Block();
    }

    private void ResetResult()
    {
        FoundUser = "";
        FoundRoom = "";
        this.RaisePropertyChanged(nameof(HasResult));
    }

    private void SetStatus(string key, params object[] args)
    {
        string template = _localizer.Get(key);
        Status = args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
    }
}
