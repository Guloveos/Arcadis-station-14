﻿using System.Linq;
using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

public sealed class HandsContainer : Control
{
    [Dependency] private IItemSlotManager _itemSlotManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IEntitySystemManager _systemManager = default!;
    public HandsSystem HandsSystem => _handsSystem;
    public HandsComponent? HandsComponent => _playerHands;
    private readonly HandsSystem _handsSystem;
    private HandsComponent? _playerHands = null;
    public List<HandControl> Hands => _hands.Values.ToList();
    private readonly Dictionary<string, HandControl> _hands = new();
    private readonly GridContainer _grid;

    public HandControl? ActiveHand => _activeHand;

    private HandControl? _activeHand = null;
    public HandsContainer()
    {
        IoCManager.InjectDependencies(this);
        _handsSystem = _systemManager.GetEntitySystem<HandsSystem>();
        _handsSystem.TryGetPlayerHands(out _playerHands);
        AddChild(_grid = new GridContainer());
        _grid.Columns = 4;
        _grid.HorizontalAlignment = HAlignment.Center;

    }
    public bool TryGetHand(string name, out HandControl hand)
    {
        return _hands.TryGetValue(name, out hand!);
    }

    public bool TryGetActiveHand(out HandControl? activeHand)
    {
        activeHand = ActiveHand;
        return ActiveHand != null;
    }

    private void OnHandPressed(GUIBoundKeyEventArgs args, string handName)
    {
        if (_playerHands == null) return;//not sure how this would be possible lol
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _handsSystem.UIHandClick(_playerHands, handName);
        }
        else if (TryGetHand(handName, out var hand))
        {
            _itemSlotManager.OnButtonPressed(args, hand.HeldItem);
        }
    }
    private void OnStoragePressed(string handName)
    {
        _handsSystem.UIHandActivate(handName);
    }
    private void UpdatePlayerHandsComponent()
    {
         _handsSystem.TryGetPlayerHands(out _playerHands);
    }

    private void RegisterHand(string name, HandLocation location, EntityUid? heldItem = null)
    {
        var newHand = new HandControl(this, location, _entityManager, _itemSlotManager, heldItem, name);
        newHand.OnPressed += args => OnHandPressed(args, name);
        newHand.OnStoragePressed += args => OnStoragePressed(name);
        if (!_hands.TryAdd(name, newHand)) throw new Exception("Duplicate handName detected!: " + name);
        AddHandToGui(newHand);
    }

    public void SetActiveHand(SharedHandsComponent? handComp)
    {
        if (handComp == null)
        {
             if (_activeHand != null) _activeHand.Active = false;
            return;
        }

        if (handComp.ActiveHand == null)
        {
            if (_activeHand != null) _activeHand.Active = false;
            _activeHand = null;
            return;
        }

        if (_activeHand != null) _activeHand.Active = false;
        _activeHand = _hands[handComp.ActiveHand.Name];
        _activeHand.Active = true;
    }

    public void RemoveHand(string name)
    {
        RemoveHandFromGui(_hands[name]);
        _hands[name].Dispose();
        _hands.Remove(name);
    }

    public void RemoveHand(Hand handData)
    {
        RemoveHand(handData.Name);
        SetActiveHand(HandsComponent);
    }

    public void RegisterHand(Hand handData)
    {
        RegisterHand(handData.Name, handData.Location, handData.HeldEntity);
        SetActiveHand(HandsComponent);
    }

    public void UpdateHandGui(Hand handData)
    {
        _hands[handData.Name].HeldItem = handData.HeldEntity;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var handData in _hands)
        {
            _itemSlotManager.UpdateCooldown(handData.Value, handData.Value.HeldItem);
        }
    }

    public void LoadHands(HandsComponent component)
    {
        _playerHands = component;
        foreach (var handData in _playerHands.Hands)
        {
            Logger.Debug(handData.Key);
            RegisterHand(handData.Value);
        }
    }

    public void UnloadHands()
    {
        foreach (var handData in _hands.Values)
        {
            handData.Dispose();
        }
        _hands.Clear();
        _playerHands = null;
    }

    private void AddHandToGui(HandControl control)
    {
        _grid.AddChild(control);
    }

    private void RemoveHandFromGui(HandControl control)
    {
        _grid.RemoveChild(control);
    }

}
