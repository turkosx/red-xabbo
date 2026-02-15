using System.Reactive;
using ReactiveUI;

using Xabbo.Messages.Flash;
using Xabbo.Extension;
using Xabbo.Core;
using Xabbo.Core.Game;
using Xabbo.Core.Events;
using Xabbo.Core.Messages.Outgoing;

namespace Xabbo.Components;

[Intercept]
public partial class MimicComponent : Component
{
    private enum MimicState
    {
        Idle,
        Selecting,
        Active
    }

    private readonly ProfileManager _profileManager;
    private readonly RoomManager _roomManager;

    private MimicState _state = MimicState.Idle;

    private Id _targetId = -1;

    private AvatarSign _lastSign = AvatarSign.None;
    private bool? _lastSitting;
    private int _lastEffect = int.MinValue;
    private string? _activeSpecialEffectCommand;

    [Reactive] public bool Figure { get; set; } = true;
    [Reactive] public bool Motto { get; set; } = true;
    [Reactive] public bool Action { get; set; } = true;
    [Reactive] public bool Dance { get; set; } = true;
    [Reactive] public bool Sign { get; set; } = true;
    [Reactive] public bool Effect { get; set; } = true;
    [Reactive] public bool Sit { get; set; } = true;
    [Reactive] public bool Follow { get; set; }
    [Reactive] public bool Typing { get; set; } = true;
    [Reactive] public bool Talk { get; set; } = true;
    [Reactive] public bool Shout { get; set; } = true;
    [Reactive] public bool Whisper { get; set; } = true;

    private string? _targetName;
    public string? TargetName
    {
        get => _targetName;
        private set
        {
            Set(ref _targetName, value);
            HasTarget = !string.IsNullOrWhiteSpace(value);
            this.RaisePropertyChanged(nameof(ShowNoTarget));
            this.RaisePropertyChanged(nameof(ShowSelectingHint));
        }
    }

    private bool _hasTarget;
    public bool HasTarget
    {
        get => _hasTarget;
        private set => Set(ref _hasTarget, value);
    }

    public bool IsIdle => _state is MimicState.Idle;
    public bool IsSelecting => _state is MimicState.Selecting;
    public bool IsActive => _state is MimicState.Active;

    public bool ShowNoTarget => !HasTarget && !IsSelecting;
    public bool ShowSelectingHint => !HasTarget && IsSelecting;

    public ReactiveCommand<Unit, Unit> StartCmd { get; }
    public ReactiveCommand<Unit, Unit> CancelCmd { get; }
    public ReactiveCommand<Unit, Unit> StopCmd { get; }

    public MimicComponent(
        IExtension extension,
        ProfileManager profileManager,
        RoomManager roomManager)
        : base(extension)
    {
        _profileManager = profileManager;
        _roomManager = roomManager;

        StartCmd = ReactiveCommand.Create(StartSelecting);
        CancelCmd = ReactiveCommand.Create(CancelSelecting);
        StopCmd = ReactiveCommand.Create(StopMimic);

        _roomManager.Left += OnLeftRoom;
        _roomManager.AvatarAdded += OnAvatarAdded;
        _roomManager.AvatarUpdated += OnAvatarUpdated;
        _roomManager.AvatarChanged += OnAvatarChanged;
        _roomManager.AvatarNameChanged += OnAvatarNameChanged;
        _roomManager.AvatarAction += OnAvatarAction;
        _roomManager.AvatarDance += OnAvatarDance;
        _roomManager.AvatarEffect += OnAvatarEffect;
        _roomManager.AvatarTyping += OnAvatarTyping;
        _roomManager.AvatarChat += OnAvatarChat;
    }

    protected override void OnConnected(ConnectedEventArgs e)
    {
        if (Session.Is(ClientType.Shockwave))
        {
            IsAvailable = false;
            return;
        }

        base.OnConnected(e);
    }

    protected override void OnDisconnected()
    {
        Reset();
        base.OnDisconnected();
    }

    private void SetState(MimicState state)
    {
        if (_state == state)
            return;

        _state = state;
        this.RaisePropertyChanged(nameof(IsIdle));
        this.RaisePropertyChanged(nameof(IsSelecting));
        this.RaisePropertyChanged(nameof(IsActive));
        this.RaisePropertyChanged(nameof(ShowNoTarget));
        this.RaisePropertyChanged(nameof(ShowSelectingHint));
    }

    private void StartSelecting()
    {
        if (!IsAvailable || !_roomManager.IsInRoom)
            return;

        TargetName = null;
        _targetId = -1;

        _lastSign = AvatarSign.None;
        _lastSitting = null;
        _lastEffect = int.MinValue;
        _activeSpecialEffectCommand = null;

        SetState(MimicState.Selecting);
    }

    private void CancelSelecting() => Reset();
    private void StopMimic() => Reset();

    private void Reset()
    {
        _targetId = -1;
        TargetName = null;

        _lastSign = AvatarSign.None;
        _lastSitting = null;
        _lastEffect = int.MinValue;
        _activeSpecialEffectCommand = null;

        SetState(MimicState.Idle);
    }

    private bool IsTarget(IAvatar avatar) =>
        IsAvailable &&
        IsActive &&
        _targetId >= 0 &&
        avatar.Id == _targetId;

    private IUser? GetTargetUser()
    {
        if (!IsActive || !_roomManager.EnsureInRoom(out IRoom? room) || _targetId < 0)
            return null;
        return room.GetAvatarById<IUser>(_targetId);
    }

    private void SelectTarget(IUser target)
    {
        _targetId = target.Id;
        TargetName = target.Name;

        _lastSign = AvatarSign.None;
        _lastSitting = null;
        _lastEffect = int.MinValue;
        _activeSpecialEffectCommand = null;

        SetState(MimicState.Active);
        ApplyProfile(target);
    }

    private void ApplyProfile(IUser target)
    {
        if (Figure)
            Ext.Send(new UpdateAvatarMsg(target.Gender, target.Figure));
        if (Motto)
            Ext.Send(new UpdateMottoMsg(target.Motto));
    }

    [Intercept(~ClientType.Shockwave)]
    [InterceptOut(nameof(Out.GetSelectedBadges))]
    private void HandleGetSelectedBadges(Intercept e)
    {
        if (!IsAvailable || !IsSelecting || !_roomManager.EnsureInRoom(out IRoom? room))
            return;

        Id clickedId = e.Packet.Read<Id>();

        if (clickedId == _profileManager.UserData?.Id)
            return;

        if (room.GetAvatarById<IUser>(clickedId) is not IUser target)
            return;

        SelectTarget(target);
    }

    [Intercept(~ClientType.Shockwave)]
    [InterceptOut(nameof(Out.LookTo))]
    private void HandleLookTo(Intercept e)
    {
        if (IsSelecting)
            e.Block();
    }

    private void OnLeftRoom() => Reset();

    private void OnAvatarAdded(AvatarEventArgs e)
    {
        if (e.Avatar is IUser user && IsTarget(user))
        {
            TargetName = user.Name;
            ApplyProfile(user);
        }
    }

    private void OnAvatarNameChanged(AvatarNameChangedEventArgs e)
    {
        if (IsTarget(e.Avatar))
            TargetName = e.Avatar.Name;
    }

    private void OnAvatarChanged(AvatarChangedEventArgs e)
    {
        if (!IsTarget(e.Avatar) || e.Avatar is not IUser user)
            return;

        if (Figure && (e.FigureUpdated || e.GenderUpdated))
            Ext.Send(new UpdateAvatarMsg(user.Gender, user.Figure));

        if (Motto && e.MottoUpdated)
            Ext.Send(new UpdateMottoMsg(user.Motto));
    }

    private void OnAvatarUpdated(AvatarEventArgs e)
    {
        if (!IsTarget(e.Avatar) || e.Avatar.CurrentUpdate is not { } update)
            return;

        if (Follow)
        {
            Ext.Send(new LookToMsg(e.Avatar.X, e.Avatar.Y));

            if (update.MovingTo is not null)
                Ext.Send(new WalkMsg(e.Avatar.X, e.Avatar.Y));
        }

        if (Sit)
        {
            bool isSitting = update.Stance is AvatarStance.Sit or AvatarStance.Lay;
            if (_lastSitting != isSitting)
            {
                Ext.Send(Out.ChangePosture, isSitting ? 1 : 0);
                _lastSitting = isSitting;
            }
        }

        if (Sign)
        {
            AvatarSign sign = update.Sign;

            if (sign is AvatarSign.None)
            {
                _lastSign = AvatarSign.None;
            }
            else if (sign != _lastSign)
            {
                Ext.Send(Out.Sign, (int)sign);
                _lastSign = sign;
            }
        }
    }

    private void OnAvatarAction(AvatarActionEventArgs e)
    {
        if (!Action || !IsTarget(e.Avatar) || e.Action is AvatarAction.None)
            return;

        Ext.Send(new ActionMsg(e.Action));
    }

    private void OnAvatarDance(AvatarDanceEventArgs e)
    {
        if (!Dance || !IsTarget(e.Avatar))
            return;

        Ext.Send(new DanceMsg(e.Avatar.Dance));
    }

    private void OnAvatarEffect(AvatarEffectEventArgs e)
    {
        if (!IsTarget(e.Avatar))
            return;

        int effectId = e.Avatar.Effect;
        if (effectId == _lastEffect)
            return;

        _lastEffect = effectId;

        HandleSpecialEffect(effectId);

        if (!Effect)
            return;

        if (effectId <= 0)
        {
            Ext.Send(Out.AvatarEffectActivated, -1);
            Ext.Send(Out.AvatarEffectSelected, -1);
        }
        else if (effectId is not (140 or 196 or 136))
        {
            Ext.Send(Out.AvatarEffectActivated, effectId);
            Ext.Send(Out.AvatarEffectSelected, effectId);
        }
    }

    private void HandleSpecialEffect(int effectId)
    {
        if (effectId is 140 or 196 or 136)
        {
            string command = effectId switch
            {
                140 => ":habnam",
                196 => ":YYXXABXA",
                136 => ":moonwalk",
                _ => ""
            };

            if (!string.IsNullOrEmpty(command))
            {
                Ext.Send(new TalkMsg(command, 0, -1));
                _activeSpecialEffectCommand = command;
            }
        }
        else if (effectId <= 0 && _activeSpecialEffectCommand is { } activeCommand)
        {
            Ext.Send(new TalkMsg(activeCommand, 0, -1));
            _activeSpecialEffectCommand = null;
        }
    }

    private void OnAvatarTyping(AvatarTypingEventArgs e)
    {
        if (!Typing || !IsTarget(e.Avatar))
            return;

        Ext.Send(e.Avatar.IsTyping ? Out.StartTyping : Out.CancelTyping);
    }

    private void OnAvatarChat(AvatarChatEventArgs e)
    {
        if (!IsTarget(e.Avatar))
            return;

        switch (e.ChatType)
        {
            case ChatType.Talk when Talk:
                Ext.Send(new TalkMsg(e.Message, e.BubbleStyle, -1));
                break;

            case ChatType.Shout when Shout:
                Ext.Send(new ShoutMsg(e.Message, e.BubbleStyle));
                break;

            case ChatType.Whisper when Whisper:
                if (!string.IsNullOrWhiteSpace(TargetName))
                    Ext.Send(new WhisperMsg(TargetName, e.Message, e.BubbleStyle));
                break;
        }
    }
}
