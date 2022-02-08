using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
///     Harmless artifact that broadcast "thoughts" to players nearby.
///     Thoughts are shown as popups and unique for each player.
/// </summary>
[RegisterComponent]
public class TelepathicArtifactComponent : Component
{
    /// <summary>
    ///     Loc string ids of telepathic messages.
    ///     Will be randomly picked and shown to player.
    /// </summary>
    [DataField("messages")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string[] Messages = default!;

    /// <summary>
    ///     Loc string ids of telepathic messages (spooky version).
    ///     Will be randomly picked and shown to player.
    /// </summary>
    [DataField("drastic")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string[] DrasticMessages = default!;

    /// <summary>
    ///     Probability to pick drastic version of message.
    /// </summary>
    [DataField("drasticProb")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DrasticMessageProb = 0.2f;

    /// <summary>
    ///     Radius in which player can receive artifacts messages.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 10f;
}
