using Content.Client.Station;
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Administration.UI.Tabs.ObjectsTab;

[GenerateTypedNameReferences]
public sealed partial class ObjectsTab : Control
{
    private readonly Color _altColor = Color.FromHex("#292B38");
    private readonly Color _defaultColor = Color.FromHex("#2F2F3B");
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly List<ObjectsTabSelection> _selections = [];
    [Dependency] private readonly IGameTiming _timing = default!;
    private bool _ascending;
    private ObjectsTabHeader.Header _headerClicked = ObjectsTabHeader.Header.ObjectName;

    public ObjectsTab()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        ObjectTypeOptions.OnItemSelected += ev =>
        {
            ObjectTypeOptions.SelectId(ev.Id);
            RefreshObjectList(_selections[ev.Id]);
        };

        foreach (var type in Enum.GetValues(typeof(ObjectsTabSelection)))
        {
            _selections.Add((ObjectsTabSelection) type!);
            ObjectTypeOptions.AddItem(Enum.GetName((ObjectsTabSelection) type)!);
        }

        ListHeader.OnHeaderClicked += HeaderClicked;
        SearchList.SearchBar = SearchLineEdit;
        SearchList.GenerateItem += GenerateButton;
        SearchList.DataFilterCondition += DataFilterCondition;
        SearchList.ItemKeyBindDown += (args, data) => OnEntryKeyBindDown?.Invoke(args, data);
        RefreshListButton.OnPressed += RefreshButtonPressed;

        var defaultSelection = ObjectsTabSelection.Grids;
        ObjectTypeOptions.SelectId((int) defaultSelection);
        RefreshObjectList(defaultSelection);
    }

    public event Action<GUIBoundKeyEventArgs, ListData>? OnEntryKeyBindDown;

    public void RefreshObjectList()
    {
        RefreshObjectList(_selections[ObjectTypeOptions.SelectedId]);
    }

    private void RefreshObjectList(ObjectsTabSelection selection)
    {
        var entities = new List<(string Name, NetEntity Entity)>();
        switch (selection)
        {
            case ObjectsTabSelection.Stations:
                entities.AddRange(_entityManager.EntitySysManager.GetEntitySystem<StationSystem>().Stations);
                break;
            case ObjectsTabSelection.Grids:
            {
                var query = _entityManager.AllEntityQueryEnumerator<MapGridComponent, MetaDataComponent>();
                while (query.MoveNext(out var uid, out _, out var metadata))
                {
                    entities.Add((metadata.EntityName, _entityManager.GetNetEntity(uid)));
                }

                break;
            }
            case ObjectsTabSelection.Maps:
            {
                var query = _entityManager.AllEntityQueryEnumerator<MapComponent, MetaDataComponent>();
                while (query.MoveNext(out var uid, out _, out var metadata))
                {
                    entities.Add((metadata.EntityName, _entityManager.GetNetEntity(uid)));
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(selection), selection, null);
        }

        entities.Sort((a, b) =>
        {
            var valueA = GetComparableValue(a, _headerClicked);
            var valueB = GetComparableValue(b, _headerClicked);
            return _ascending
                ? Comparer<object>.Default.Compare(valueA, valueB)
                : Comparer<object>.Default.Compare(valueB, valueA);
        });

        var listData = new List<ObjectsListData>();
        for (var index = 0; index < entities.Count; index++)
        {
            var info = entities[index];
            listData.Add(new ObjectsListData(info,
                $"{info.Name} {info.Entity}",
                index % 2 == 0 ? _altColor : _defaultColor));
        }

        SearchList.PopulateList(listData);
    }

    private void GenerateButton(ListData data, ListContainerButton button)
    {
        if (data is not ObjectsListData { Info: var info, BackgroundColor: var backgroundColor, })
            return;

        var entry = new ObjectsTabEntry(info.Name,
            info.Entity,
            new StyleBoxFlat { BackgroundColor = backgroundColor, });
        button.ToolTip = $"{info.Name}, {info.Entity}";

        button.AddChild(entry);
    }

    private bool DataFilterCondition(string filter, ListData listData)
    {
        if (listData is not ObjectsListData { FilteringString: var filteringString, })
            return false;

        // If the filter is empty, do not filter out any entries
        if (string.IsNullOrEmpty(filter))
            return true;

        return filteringString.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }

    private object GetComparableValue((string Name, NetEntity Entity) entity, ObjectsTabHeader.Header header)
    {
        return header switch
        {
            ObjectsTabHeader.Header.ObjectName => entity.Name,
            ObjectsTabHeader.Header.EntityID => entity.Entity.ToString(),
            _ => entity.Name,
        };
    }

    private void HeaderClicked(ObjectsTabHeader.Header header)
    {
        if (_headerClicked == header)
            _ascending = !_ascending;
        else
        {
            _headerClicked = header;
            _ascending = true;
        }

        ListHeader.UpdateHeaderSymbols(_headerClicked, _ascending);
        RefreshObjectList();
    }

    private void RefreshButtonPressed(BaseButton.ButtonEventArgs args)
    {
        RefreshObjectList();
    }

    private enum ObjectsTabSelection
    {
        Grids,
        Maps,
        Stations,
    }
}

public record ObjectsListData((string Name, NetEntity Entity) Info, string FilteringString, Color BackgroundColor)
    : ListData;
