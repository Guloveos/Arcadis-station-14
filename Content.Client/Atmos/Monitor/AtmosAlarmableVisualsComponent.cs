using Content.Shared.Atmos.Monitor;

namespace Content.Client.Atmos.Monitor;

[RegisterComponent]
public sealed partial class AtmosAlarmableVisualsComponent : Component
{
    [DataField("layerMap")]
    public string LayerMap { get; private set; } = string.Empty;

    [DataField("alarmStates")]
    public Dictionary<AtmosAlarmType, string> AlarmStates { get; private set; } = new();

    [DataField("hideOnDepowered")]
    public List<string>? HideOnDepowered { get; private set; }

    // eh...
    [DataField("setOnDepowered")]
    public Dictionary<string, string>? SetOnDepowered { get; private set; }
}
