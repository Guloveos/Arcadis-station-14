using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Administration.UI.Tabs.AtmosTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public sealed partial class SetTemperatureWindow : DefaultWindow
    {
        private List<EntityUid>? _data;

        protected override void EnteredTree()
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var playerManager = IoCManager.Resolve<IPlayerManager>();

            var gridQuery = entManager.AllEntityQueryEnumerator<MapGridComponent>();
            _data ??= new List<EntityUid>();
            _data.Clear();

            while (gridQuery.MoveNext(out var uid, out _))
            {
                var player = playerManager.LocalPlayer?.ControlledEntity;
                var playerGrid = entManager.GetComponentOrNull<TransformComponent>(player)?.GridUid;
                GridOptions.AddItem($"{uid} {(playerGrid == uid ? " (Current)" : "")}");
                _data.Add(uid);
            }

            GridOptions.OnItemSelected += eventArgs => GridOptions.SelectId(eventArgs.Id);
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_data == null)
                return;

            var selectedGrid = _data[GridOptions.SelectedId];
            IoCManager.Resolve<IClientConsoleHost>()
                .ExecuteCommand($"settemp {TileXSpin.Value} {TileYSpin.Value} {selectedGrid} {TemperatureSpin.Value}");
        }
    }
}
