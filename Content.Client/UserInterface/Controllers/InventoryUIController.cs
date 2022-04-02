﻿using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController : UIController
{
    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    [Dependency] private readonly IUIWindowManager _uiWindowManager = default!;
    private ClientInventoryComponent? _playerInventory;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();
    private InventoryWindow? _inventoryWindow;

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            //bind open inventory key to OpenInventoryMenu;
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu, InputCmdHandler.FromDelegate(_ => ToggleInventoryMenu()))
                .Register<ClientInventorySystem>();
        }
    }

    private void CreateInventoryWindow(ClientInventoryComponent? clientInv)
    {
        if (clientInv == null) return;
        _inventoryWindow = _uiWindowManager.CreateNamedWindow<InventoryWindow>("Inventory");
        foreach (var (_,data) in clientInv.SlotData)
        {
            if (data.ShowInWindow)
            {
                _inventoryWindow!.InventoryButtons.AddButton(new ItemSlotButton(data), data.ButtonOffset);
            }
        }
    }
    public void ToggleInventoryMenu()
    {
        if (_inventoryWindow != null)
        {
            _inventoryWindow.Dispose();
            _inventoryWindow = null;
            return;
        }
        CreateInventoryWindow(_playerInventory);
    }


    //Neuron Activation
    public override void OnSystemLoaded(IEntitySystem system)
    {
        Logger.Debug("NEURON ACTIVATED");
        switch (system)
        {
            case ClientInventorySystem:
                OnInventorySystemActivate();
                return;
            case HandsSystem:
                OnHandsSystemActivate();
                return;
        }
    }
    //Neuron Deactivation
    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case ClientInventorySystem:
                OnInventorySystemDeactivate();
                return;
            case HandsSystem:
                OnHandsSystemDeactivate();
                return;
        }
    }

    private void OnInventorySystemActivate()
    {
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventory += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
        _inventorySystem.OnSpriteUpdate += SpriteUpdated;
    }

    private void OnInventorySystemDeactivate()
    {
        _inventorySystem.OnSlotAdded -= AddSlot;
        _inventorySystem.OnSlotRemoved -= RemoveSlot;
        _inventorySystem.OnLinkInventory -= LoadSlots;
        _inventorySystem.OnUnlinkInventory -= UnloadSlots;
        _inventorySystem.OnSpriteUpdate -= SpriteUpdated;
    }

    private void OnItemPressed(GUIBoundKeyEventArgs args, ItemSlotControl control)
    {
        var slot = control.SlotName;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _inventorySystem.UIInventoryActivate(control.SlotName);
            return;
        }

        if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _inventorySystem.UIInventoryStorageActivate(control.SlotName);
            return;
        }

        if (_playerInventory == null)
        {
            return;
        }

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _inventorySystem.UIInventoryExamine(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.OpenContextMenu)
        {
            _inventorySystem.UIInventoryOpenContextMenu(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _inventorySystem.UIInventoryActivateItem(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _inventorySystem.UIInventoryAltActivateItem(slot, _playerInventory.Owner);
        }
    }

    private void OnStoragePressed(GUIBoundKeyEventArgs args, ItemSlotControl control)
    {
        _inventorySystem.UIInventoryStorageActivate(control.SlotName);
    }

    private void AddSlot(ClientInventorySystem.SlotData data)
    {
        if(!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        var button = new ItemSlotButton(data);
        button.OnPressed += OnItemPressed;
        button.OnStoragePressed += OnStoragePressed;
        slotGroup.AddChild(button);
        button.SlotName = data.SlotName;
    }

    private void RemoveSlot(ClientInventorySystem.SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        slotGroup.RemoveButton(data.SlotName);
    }

    private void LoadSlots(ClientInventoryComponent clientInv)
    {
        _playerInventory = clientInv;
        foreach (var slotData in clientInv.SlotData.Values)
        {
            AddSlot(slotData);
        }
    }

    private void UnloadSlots()
    {
        _playerInventory = null;
        foreach (var slotGroup in _slotGroups.Values)
        {
            slotGroup.ClearButtons();
        }
    }

    private void SpriteUpdated(string slotGroup, string slotName, ISpriteComponent? sprite, bool showStorageButton)
    {
        if (!_slotGroups.TryGetValue(slotGroup, out var group) ||
            !group.TryGetButton(slotName, out var button))
        {
            return;
        }

        button.SpriteView.Sprite = sprite;
        button.StorageButton.Visible = showStorageButton;
    }

    public void BlockSlot(string slotName, bool blocked)
    {
    }
    public void HighlightSlot(string slotName, bool highlight)
    {
    }
    public bool RegisterSlotGroupContainer(ItemSlotButtonContainer slotContainer)
    {
        return _slotGroups.TryAdd(slotContainer.SlotGroup, slotContainer);
    }

    public void RemoveSlotGroup(string slotGroupName)
    {
        _slotGroups.Remove(slotGroupName);
    }
}
