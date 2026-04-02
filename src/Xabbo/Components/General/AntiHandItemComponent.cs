using System.Diagnostics.CodeAnalysis;

using Xabbo.Messages.Flash;
using Xabbo.Extension;
using Xabbo.Core;
using Xabbo.Core.Game;

namespace Xabbo.Components;

[Intercept]
public partial class AntiHandItemComponent(
    IExtension extension,
    ProfileManager profileManager,
    RoomManager roomManager
)
    : Component(extension)
{
    private static readonly TimeSpan PendingReceivedHandItemWindow = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan MaintainDirectionSettleDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan MaintainDirectionRestoreDelay = TimeSpan.FromMilliseconds(50);

    private readonly ProfileManager _profileManager = profileManager;
    private readonly RoomManager _roomManager = roomManager;

    private readonly SemaphoreSlim _maintainDirectionSemaphore = new(1, 1);
    private DateTime _pendingReceivedAt = DateTime.MinValue;
    private int _pendingReceivedDirection = -1;
    private int _queuedMaintainDirection = -1;
    private long _queuedMaintainDirectionVersion;
    private long _handledMaintainDirectionVersion;

    [Reactive] public bool DropHandItem { get; set; }
    [Reactive] public bool ReturnHandItem { get; set; }
    [Reactive] public bool ShouldMaintainDirection { get; set; }

    [InterceptIn(nameof(In.HandItemReceived))]
    private void HandleHandItemReceived(Intercept e)
    {
        if (!DropHandItem && !ReturnHandItem && !ShouldMaintainDirection)
            return;

        int packetPosition = e.Packet.Position;

        try
        {
            int source = e.Packet.Read<int>();

            if (!TryResolveSourceUser(source, out IUser? sender))
                return;

            if (ShouldMaintainDirection)
            {
                SetPendingPlayerHandItemContext();
                if (_pendingReceivedDirection >= 0)
                    QueueMaintainDirection(_pendingReceivedDirection);
            }

            if (ReturnHandItem)
            {
                e.Block();
                Ext.Send(Out.PassCarryItem, sender.Id);
            }
            else if (DropHandItem)
            {
                e.Block();
                Ext.Send(Out.DropCarryItem);
            }
        }
        finally
        {
            if (!e.IsBlocked)
                e.Packet.Position = packetPosition;
        }
    }

    [InterceptIn(nameof(In.CarryObject))]
    private void HandleCarryObject(Intercept e)
    {
        if (!ShouldMaintainDirection)
            return;

        if (!TryGetSelfUser(out IUser? self))
            return;

        int packetPosition = e.Packet.Position;
        try
        {
            int index = e.Packet.Read<int>();
            int item = e.Packet.Read<int>();

            if (index != self.Index || item <= 0)
                return;

            if (TryConsumePendingPlayerHandItemDirection(out int direction))
                QueueMaintainDirection(direction);
        }
        finally
        {
            e.Packet.Position = packetPosition;
        }
    }

    private bool TryResolveSourceUser(int source, [NotNullWhen(true)] out IUser? user)
    {
        user = null;

        if (source <= 0 || _roomManager.Room is null)
            return false;

        return _roomManager.Room.TryGetUserById(source, out user) ||
            _roomManager.Room.TryGetUserByIndex(source, out user);
    }

    private bool TryGetSelfUser([NotNullWhen(true)] out IUser? user)
    {
        user = null;

        return _roomManager.Room is not null &&
            _roomManager.Room.TryGetUserById(_profileManager.UserData?.Id ?? -1, out user);
    }

    private void SetPendingPlayerHandItemContext()
    {
        if (TryGetSelfUser(out IUser? user))
        {
            _pendingReceivedAt = DateTime.UtcNow;
            _pendingReceivedDirection = user.Direction;
        }
        else
        {
            ClearPendingPlayerHandItemContext();
        }
    }

    private void ClearPendingPlayerHandItemContext()
    {
        _pendingReceivedAt = DateTime.MinValue;
        _pendingReceivedDirection = -1;
    }

    private bool TryConsumePendingPlayerHandItemDirection(out int direction)
    {
        if (_pendingReceivedDirection >= 0 &&
            DateTime.UtcNow - _pendingReceivedAt <= PendingReceivedHandItemWindow)
        {
            direction = _pendingReceivedDirection;
            ClearPendingPlayerHandItemContext();
            return true;
        }

        ClearPendingPlayerHandItemContext();
        direction = -1;
        return false;
    }

    private void QueueMaintainDirection(int direction)
    {
        if (direction < 0)
            return;

        Volatile.Write(ref _queuedMaintainDirection, direction);
        Interlocked.Increment(ref _queuedMaintainDirectionVersion);
        _ = Task.Run(TryMaintainDirectionAsync);
    }

    private async Task TryMaintainDirectionAsync()
    {
        await _maintainDirectionSemaphore.WaitAsync();

        try
        {
            while (true)
            {
                long version = Volatile.Read(ref _queuedMaintainDirectionVersion);
                if (version == Volatile.Read(ref _handledMaintainDirectionVersion))
                    return;

                int direction = Volatile.Read(ref _queuedMaintainDirection);
                if (direction < 0)
                    return;

                await Task.Delay(MaintainDirectionSettleDelay);

                if (version != Volatile.Read(ref _queuedMaintainDirectionVersion))
                    continue;

                (int x, int y) = H.GetMagicVector(direction);
                (int invX, int invY) = H.GetMagicVector((direction + 4) % 8);

                Ext.Send(Out.LookTo, invX, invY);
                await Task.Delay(MaintainDirectionRestoreDelay);

                if (version != Volatile.Read(ref _queuedMaintainDirectionVersion))
                    continue;

                Ext.Send(Out.LookTo, x, y);
                Volatile.Write(ref _handledMaintainDirectionVersion, version);

                if (version == Volatile.Read(ref _queuedMaintainDirectionVersion))
                    return;
            }
        }
        finally
        {
            _maintainDirectionSemaphore.Release();
        }
    }
}
