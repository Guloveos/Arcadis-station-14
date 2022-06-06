using System.Linq;
using Content.Client.Cargo.UI;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Cargo
{
    public sealed class CargoConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CargoConsoleMenu? _menu;

        /// <summary>
        /// This is the separate popup window for individual orders.
        /// </summary>
        [ViewVariables]
        private CargoConsoleOrderMenu? _orderMenu;

        public EntityUid? Station { get; private set; }

        [ViewVariables]
        public string? AccountName { get; private set; }

        [ViewVariables]
        public int BankBalance { get; private set; }

        [ViewVariables]
        public int OrderCapacity { get; private set; }

        [ViewVariables]
        public int OrderCount { get; private set; }

        public List<CargoOrderData> Orders { get; private set; } = new();

        /// <summary>
        /// Currently selected product
        /// </summary>
        private CargoProductPrototype? _product;

        public CargoConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new CargoConsoleMenu(IoCManager.Resolve<IPrototypeManager>());
            _orderMenu = new CargoConsoleOrderMenu();

            _menu.OnClose += Close;

            _menu.OnItemSelected += (args) =>
            {
                if (args.Button.Parent is not CargoProductRow row)
                    return;
                _product = row.Product;
                _orderMenu.Requester.Text = "";
                _orderMenu.Reason.Text = "";
                _orderMenu.Amount.Value = 1;
                _orderMenu.OpenCentered();
            };
            _menu.OnOrderApproved += ApproveOrder;
            _menu.OnOrderCanceled += RemoveOrder;
            _orderMenu.SubmitButton.OnPressed += (_) =>
            {
                if (AddOrder())
                {
                    _orderMenu.Close();
                }
            };

            _menu.OpenCentered();
        }

        private void Populate(StationCargoOrderDatabaseComponent? orderDatabase)
        {
            if (_menu == null) return;

            _menu.PopulateProducts();
            _menu.PopulateCategories();
            _menu.PopulateOrders(GetOrders(orderDatabase));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CargoConsoleInterfaceState cState)
                return;

            var entManager = IoCManager.Resolve<IEntityManager>();

            entManager.TryGetComponent<StationCargoOrderDatabaseComponent>(cState.Station, out var orderDatabase);
            entManager.TryGetComponent<StationBankAccountComponent>(cState.Station, out var bankAccount);

            OrderCapacity = orderDatabase?.Capacity ?? 0;
            OrderCount = orderDatabase != null ? GetOrderCount(orderDatabase) : 0;
            BankBalance = bankAccount?.Balance ?? 0;

            AccountName = "Fuck fuck fuck fuck fuck";

            Populate(orderDatabase);
            _menu?.UpdateCargoCapacity(OrderCount, OrderCapacity);
            _menu?.UpdateBankData(AccountName, BankBalance);
        }

        private IEnumerable<CargoOrderData> GetOrders(StationCargoOrderDatabaseComponent? component)
        {
            if (component == null) return Enumerable.Empty<CargoOrderData>();

            return component.Orders.Values;
        }

        // TODO: Move to the shared system
        private int GetOrderCount(StationCargoOrderDatabaseComponent component)
        {
            var count = 0;

            foreach (var (_, order) in component.Orders)
            {
                count += order.Amount;
            }

            return count;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            _menu?.Dispose();
            _orderMenu?.Dispose();
        }

        private bool AddOrder()
        {
            int orderAmt = _orderMenu?.Amount.Value ?? 0;
            if (orderAmt < 1 || orderAmt > OrderCapacity)
            {
                return false;
            }

            SendMessage(new CargoConsoleAddOrderMessage(
                _orderMenu?.Requester.Text ?? "",
                _orderMenu?.Reason.Text ?? "",
                _product?.ID ?? "",
                orderAmt));

            return true;
        }

        private void RemoveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            SendMessage(new CargoConsoleRemoveOrderMessage(row.Order.OrderNumber));
        }

        private void ApproveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not CargoOrderRow row || row.Order == null)
                return;

            if (OrderCount >= OrderCapacity)
                return;

            SendMessage(new CargoConsoleApproveOrderMessage(row.Order.OrderNumber));
            _menu?.UpdateCargoCapacity(OrderCount + row.Order.Amount, OrderCapacity);
        }
    }
}
