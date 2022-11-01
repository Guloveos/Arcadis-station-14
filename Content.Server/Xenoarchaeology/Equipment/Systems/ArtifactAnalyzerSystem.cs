﻿using System.Linq;
using Content.Server.Construction;
using Content.Server.MachineLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Research;
using Content.Server.Research.Components;
using Content.Server.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class ArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveScannedArtifactComponent, MoveEvent>(OnScannedMoved);

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentStartup>(OnAnalyzeStart);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentShutdown>(OnAnalyzeEnd);

        SubscribeLocalEvent<ArtifactAnalyzerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleScanButtonPressedMessage>(OnScanButton);
        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleDestroyButtonPressedMessage>(OnDestroyButton);

        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerSelectedMessage>(UpdateUserInterface, after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, ResearchClientServerDeselectedMessage>(UpdateUserInterface, after: new []{typeof(ResearchSystem)});
        SubscribeLocalEvent<AnalysisConsoleComponent, BeforeActivatableUIOpenEvent>(UpdateUserInterface);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (active, scan) in EntityQuery<ActiveArtifactAnalyzerComponent, ArtifactAnalyzerComponent>())
        {
            if (scan.Console != null)
                UpdateUserInterface(scan.Console.Value);

            if (_timing.CurTime - active.StartTime < (scan.AnalysisDuration * scan.AnalysisDurationMulitplier))
                continue;

            FinishScan(scan.Owner, scan, active);
        }
    }

    [PublicAPI]
    public void ResetAnalyzer(EntityUid uid, ArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastAnalyzedArtifact = null;
        UpdateAnalyzerInformation(uid, component);
    }

    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ArtifactAnalyzerComponent? component = null)
    {
        if (uid == null)
            return null;

        if (!Resolve(uid.Value, ref component))
            return null;

        var ent = _lookup.GetEntitiesIntersecting(uid.Value,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate);

        var validEnts = ent.Where(HasComp<ArtifactComponent>).ToHashSet();
        return validEnts.FirstOrNull();
    }

    private void UpdateAnalyzerInformation(EntityUid uid, ArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.LastAnalyzedArtifact == null)
        {
            component.LastAnalyzedCompletion = null;
            component.LastAnalyzedNode = null;
        }
        else if (TryComp<ArtifactComponent>(component.LastAnalyzedArtifact, out var artifact))
        {
            component.LastAnalyzedNode = artifact.CurrentNode;

            if (artifact.NodeTree != null)
            {
                var discoveredNodes = artifact.NodeTree.AllNodes.Count(x => x.Discovered && x.Triggered);
                component.LastAnalyzedCompletion = (float) discoveredNodes / artifact.NodeTree.AllNodes.Count;
            }
        }
    }

    private void OnNewLink(EntityUid uid, AnalysisConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Receiver, out var analyzer))
            return;

        component.AnalyzerEntity = args.Receiver;
        analyzer.Console = uid;

        UpdateUserInterface(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, AnalysisConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == component.LinkingPort && component.AnalyzerEntity != null)
        {
            if (TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzezr))
                analyzezr.Console = null;
            component.AnalyzerEntity = null;
        }

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, AnalysisConsoleComponent? component = null, object? _ = null)
    {
        if (!Resolve(uid, ref component))
            return;

        EntityUid? artifact = null;
        ArtifactNode? node = null;
        float? completion = null;
        var totalTime = TimeSpan.Zero;
        if (component.AnalyzerEntity != null && TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer))
        {
            artifact = analyzer.LastAnalyzedArtifact;
            node = analyzer.LastAnalyzedNode;
            completion = analyzer.LastAnalyzedCompletion;
            totalTime = analyzer.AnalysisDuration * analyzer.AnalysisDurationMulitplier;
        }

        var analyzerConnected = component.AnalyzerEntity != null;
        var serverConnected = TryComp<ResearchClientComponent>(uid, out var client) && client.ConnectedToServer;
        var canScan = component.AnalyzerEntity != null && GetArtifactForAnalysis(component.AnalyzerEntity) != null;

        var scanning = TryComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity, out var active);
        var remaining = active != null ? _timing.CurTime - active.StartTime : TimeSpan.Zero;

        var state = new AnalysisConsoleScanUpdateState(artifact, analyzerConnected, serverConnected, canScan,
            node?.Id, node?.Depth, node?.Edges.Count, node?.Triggered, node?.Effect.ID, node?.Trigger.ID, completion,
            scanning, remaining, totalTime);

        var bui = _ui.GetUi(uid, ArtifactAnalzyerUiKey.Key);
        _ui.SetUiState(bui, state);
    }

    private void OnServerSelectionMessage(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleServerSelectionMessage args)
    {
        _ui.TryOpen(uid, ResearchClientUiKey.Key, (IPlayerSession) args.Session);
    }

    private void OnScanButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleScanButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (HasComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity))
            return;

        var ent = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (ent == null)
            return;

        var activeComp = EnsureComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity.Value);
        activeComp.StartTime = _timing.CurTime;
        activeComp.Artifact = ent.Value;

        var activeArtifact = EnsureComp<ActiveScannedArtifactComponent>(ent.Value);
        activeArtifact.Scanner = component.AnalyzerEntity.Value;
    }

    private void OnDestroyButton(EntityUid uid, AnalysisConsoleComponent component, AnalysisConsoleDestroyButtonPressedMessage args)
    {
        if (!TryComp<ResearchClientComponent>(uid, out var client) || client.Server == null || component.AnalyzerEntity == null)
            return;

        var entToDestroy = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (entToDestroy == null)
            return;

        if (TryComp<ArtifactAnalyzerComponent>(component.AnalyzerEntity.Value, out var analyzer) &&
            analyzer.LastAnalyzedArtifact == entToDestroy)
        {
            ResetAnalyzer(component.AnalyzerEntity.Value);
        }

        client.Server.Points += _artifact.GetResearchPointValue(entToDestroy.Value);
        EntityManager.DeleteEntity(entToDestroy.Value);

        _audio.PlayPvs(component.DestroySound, component.AnalyzerEntity.Value, AudioParams.Default);
        _popup.PopupEntity(Loc.GetString("analyzer-artifact-destroy-popup"),
            component.AnalyzerEntity.Value, Filter.Pvs(component.AnalyzerEntity.Value), PopupType.Large);

        UpdateUserInterface(uid, component);
    }

    private void OnScannedMoved(EntityUid uid, ActiveScannedArtifactComponent component, ref MoveEvent args)
    {
        if (!HasComp<TransformComponent>(uid))
            return;

        var ents = _lookup.GetEntitiesIntersecting(component.Scanner,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate);

        if (ents.Contains(uid))
            return;

        //Play sfx? idk
        Logger.Debug("thing was moved.");

        RemComp<ActiveArtifactAnalyzerComponent>(component.Scanner);
        RemCompDeferred(uid, component);
    }

    private void FinishScan(EntityUid uid, ArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active))
            return;

        component.LastAnalyzedArtifact = active.Artifact;
        UpdateAnalyzerInformation(uid, component);

        RemComp<ActiveScannedArtifactComponent>(active.Artifact);
        RemComp(uid, active);
        if (component.Console != null)
            UpdateUserInterface(component.Console.Value);
    }

    private void OnRefreshParts(EntityUid uid, ArtifactAnalyzerComponent component, RefreshPartsEvent args)
    {
        var analysisRating = args.PartRatings[component.MachinePartAnalysisDuration];

        component.AnalysisDurationMulitplier = MathF.Pow(component.PartRatingAnalysisDurationMultiplier, analysisRating - 1);
    }

    private void OnCollide(EntityUid uid, ArtifactAnalyzerComponent component, ref StartCollideEvent args)
    {
        var otherEnt = args.OtherFixture.Body.Owner;

        if (!HasComp<ArtifactComponent>(otherEnt))
            return;

        if (component.Console != null)
            UpdateUserInterface(component.Console.Value);
    }

    private void OnAnalyzeStart(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentStartup args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = true;
    }

    private void OnAnalyzeEnd(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentShutdown args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var powa))
            powa.NeedsPower = false;
    }
}

