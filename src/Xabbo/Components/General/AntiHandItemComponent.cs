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
    private readonly ProfileManager _profileManager = profileManager;
    private readonly RoomManager _roomManager = roomManager;

    private readonly SemaphoreSlim semaphore = new(1, 1);
    private DateTime lastUpdate = DateTime.MinValue;
    private DateTime _lastReceivedAt = DateTime.MinValue;
    private int _lastReceivedSource = -1;

    [Reactive] public bool DropHandItem { get; set; }
    [Reactive] public bool ReturnHandItem { get; set; }
    [Reactive] public bool ShouldMaintainDirection { get; set; }

    [InterceptIn(nameof(In.HandItemReceived))]
    private void HandleHandItemReceived(Intercept e)
    {
        if (!DropHandItem && !ReturnHandItem && !ShouldMaintainDirection)
            return;

        int packetPosition = e.Packet.Position;
        int source = -1;

        try
        {
            if (DropHandItem || ReturnHandItem)
                source = e.Packet.Read<int>();

            if (ReturnHandItem)
            {
                Id targetId = ResolveUserId(source);
                if (targetId == -1 && source > 0)
                    targetId = source;

                if (targetId > 0)
                {
                    _lastReceivedAt = DateTime.Now;
                    _lastReceivedSource = source;
                    e.Block();
                    Ext.Send(Out.PassCarryItem, targetId);
                }
            }
            else if (DropHandItem)
            {
                _lastReceivedAt = DateTime.Now;
                _lastReceivedSource = source;
                e.Block();
                Ext.Send(Out.DropCarryItem);
            }
        }
        finally
        {
            if (!e.IsBlocked)
                e.Packet.Position = packetPosition;
        }

        if (ShouldMaintainDirection)
        {
            lastUpdate = DateTime.Now;
            Task.Run(TryMaintainDirection);
        }
    }

    [InterceptIn(nameof(In.CarryObject))]
    private void HandleCarryObject(Intercept e)
    {
        if (!DropHandItem && !ReturnHandItem && !ShouldMaintainDirection)
            return;

        Id selfId = _profileManager.UserData?.Id ?? -1;
        if (_roomManager.Room is null ||
            !_roomManager.Room.TryGetUserById(selfId, out IUser? self))
        {
            return;
        }

        int packetPosition = e.Packet.Position;
        try
        {
            int index = e.Packet.Read<int>();
            int item = e.Packet.Read<int>();

            if (index != self.Index || item <= 0)
                return;

            if (ShouldMaintainDirection)
            {
                lastUpdate = DateTime.Now;
                Task.Run(TryMaintainDirection);
            }

            if ((DateTime.Now - _lastReceivedAt).TotalMilliseconds > 2000)
            {
                if (DropHandItem)
                    Ext.Send(Out.DropCarryItem);
                else if (ReturnHandItem)
                {
                    Id targetId = ResolveUserId(_lastReceivedSource);
                    if (targetId == -1 && _lastReceivedSource > 0)
                        targetId = _lastReceivedSource;

                    if (targetId > 0)
                        Ext.Send(Out.PassCarryItem, targetId);
                }
            }
        }
        finally
        {
            e.Packet.Position = packetPosition;
        }
    }

    private Id ResolveUserId(int source)
    {
        if (source <= 0 || _roomManager.Room is null)
            return -1;

        if (_roomManager.Room.TryGetUserById(source, out IUser? byId))
            return byId.Id;

        if (_roomManager.Room.TryGetUserByIndex(source, out IUser? byIndex))
            return byIndex.Id;

        return -1;
    }

    private async Task TryMaintainDirection()
    {
        if (await semaphore.WaitAsync(0))
        {
            try
            {
                if (_roomManager.Room is null ||
                    !_roomManager.Room.TryGetUserById(_profileManager.UserData?.Id ?? -1, out IUser? user))
                {
                    return;
                }

                int dir = user.Direction;

                while ((DateTime.Now - lastUpdate).TotalSeconds < 1.0)
                {
                    do { await Task.Delay(100); }
                    while ((DateTime.Now - lastUpdate).TotalSeconds < 1.0);

                    (int x, int y) = H.GetMagicVector(dir);
                    (int invX, int invY) = H.GetMagicVector(dir + 4);

                    await Task.Delay(100);
                    Ext.Send(Out.LookTo, invX, invY);
                    await Task.Delay(100);
                    Ext.Send(Out.LookTo, x, y);
                }
            }
            finally { semaphore.Release(); }
        }
    }
}
