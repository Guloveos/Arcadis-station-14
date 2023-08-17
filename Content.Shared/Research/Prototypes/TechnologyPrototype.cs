﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a technology that can be unlocked.
/// </summary>
[Prototype("technology")]
public sealed class TechnologyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The name of the technology.
    /// Supports locale strings
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// An icon used to visually represent the technology in UI.
    /// </summary>
    [DataField("icon", required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;

    /// <summary>
    /// What research discipline this technology belongs to.
    /// </summary>
    [DataField("discipline", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TechDisciplinePrototype>))]
    public string Discipline { get; private set; } = default!;

    /// <summary>
    /// What tier research is this?
    /// The tier governs how much lower-tier technology
    /// needs to be unlocked before this one.
    /// </summary>
    [DataField("tier", required: true)]
    public int Tier { get; private set; }

    /// <summary>
    /// Hidden tech is not ever available at the research console.
    /// </summary>
    [DataField("hidden")]
    public bool Hidden { get; private set; }

    /// <summary>
    /// How much research is needed to unlock.
    /// </summary>
    [DataField("cost")]
    public int Cost { get; private set; } = 10000;

    /// <summary>
    /// A list of <see cref="TechnologyPrototype"/>s that need to be unlocked in order to unlock this technology.
    /// </summary>
    [DataField("technologyPrerequisites", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
    public IReadOnlyList<string> TechnologyPrerequisites { get; private set; } = new List<string>();

    /// <summary>
    /// A list of <see cref="LatheRecipePrototype"/>s that are unlocked by this technology
    /// </summary>
    [DataField("recipeUnlocks", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
    public IReadOnlyList<string> RecipeUnlocks { get; private set; } = new List<string>();

    /// <summary>
    /// A list of non-standard effects that are done when this technology is unlocked.
    /// </summary>
    [DataField("genericUnlocks")]
    public IReadOnlyList<GenericUnlock> GenericUnlocks { get; private set; } = new List<GenericUnlock>();
}

[DataDefinition]
public partial record struct GenericUnlock()
{
    /// <summary>
    /// What event is raised when this is unlocked?
    /// Used for doing non-standard logic.
    /// </summary>
    [DataField("purchaseEvent")]
    public object? PurchaseEvent { get; private set; } = null;

    /// <summary>
    /// A player facing tooltip for what the unlock does.
    /// Supports locale strings.
    /// </summary>
    [DataField("unlockDescription")]
    public string UnlockDescription { get; private set; } = string.Empty;
}
