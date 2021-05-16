﻿#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Medical
{
    public class SharedCloningPodComponent : Component
    {
        public override string Name => "CloningPod";

        [Serializable, NetSerializable]
        public class CloningPodBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<int, string?> MindIdName;
            // When this state was created.
            public readonly TimeSpan ReferenceTime;
            // Both of these are in seconds.
            // They're not TimeSpans because of complicated reasons.
            // CurTime of receipt is combined with Progress.
            public readonly float Progress, Maximum;
            // If true, cloning is progressing (predict clone progress)
            public readonly bool Progressing;
            public readonly bool MindPresent;

            public CloningPodBoundUserInterfaceState(Dictionary<int, string?> mindIdName, TimeSpan refTime, float progress, float maximum, bool progressing, bool mindPresent)
            {
                MindIdName = mindIdName;
                ReferenceTime = refTime;
                Progress = progress;
                Maximum = maximum;
                Progressing = progressing;
                MindPresent = mindPresent;
            }
        }


        [Serializable, NetSerializable]
        public enum CloningPodUIKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum CloningPodVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum CloningPodStatus
        {
            Idle,
            Cloning,
            Gore,
            NoMind
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Clone,
            Eject
        }

        [Serializable, NetSerializable]
        public class CloningPodUiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int? ScanId;

            public CloningPodUiButtonPressedMessage(UiButton button, int? scanId)
            {
                Button = button;
                ScanId = scanId;
            }
        }

    }
}
