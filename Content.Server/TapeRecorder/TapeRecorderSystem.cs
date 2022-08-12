using System.Linq;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.TapeRecorder;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.TapeRecorder
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class TapeRecorderSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapeRecorderComponent, ChatMessageHeardNearbyEvent>(OnChatMessageHeard);
            SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
            SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TapeRecorderComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var tapeRecorder in EntityManager.EntityQuery<TapeRecorderComponent>())
            {

                tapeRecorder.AccumulatedTime += frameTime;

                if (tapeRecorder.InsertedTape == null) //just dont bother without a tape inside
                    return;

                //stop player if tape is at end
                if (tapeRecorder.Enabled && tapeRecorder.InsertedTape.TimeStamp >= tapeRecorder.InsertedTape.TapeMaxTime && tapeRecorder.CurrentMode != TapeRecorderState.Rewind)
                {
                    StopTape(tapeRecorder);
                    tapeRecorder.CurrentMode = TapeRecorderState.Rewind; // go into rewind mode once at end of tape
                }

                //Handle tape playback
                if (tapeRecorder.Enabled && tapeRecorder.CurrentMode == TapeRecorderState.Play)
                {

                    if (tapeRecorder.CurrentMessageIndex >= tapeRecorder.InsertedTape.RecordedMessages.Count || tapeRecorder.InsertedTape.TimeStamp >= tapeRecorder.InsertedTape.TapeMaxTime)
                    {
                        StopTape(tapeRecorder);
                        tapeRecorder.CurrentMode = TapeRecorderState.Rewind; // go into record mode once we reached the end of recorded data
                        return;
                    }

                    //send the current message once our tapes timestamp passes the message timestamp
                    if (tapeRecorder.InsertedTape.TimeStamp > tapeRecorder.InsertedTape.RecordedMessages[tapeRecorder.CurrentMessageIndex].MessageTimeStamp)
                    {
                        _chat.TrySendInGameICMessage(tapeRecorder.Owner, tapeRecorder.InsertedTape.RecordedMessages[tapeRecorder.CurrentMessageIndex].Message, InGameICChatType.Speak, false);
                        tapeRecorder.CurrentMessageIndex++;
                    }

                    tapeRecorder.InsertedTape.TimeStamp += frameTime;
                }

                //Tape rewinding. fast-forwarding later (maybe)
                if (!tapeRecorder.Enabled || tapeRecorder.CurrentMode != TapeRecorderState.Rewind)
                    continue;

                tapeRecorder.InsertedTape.TimeStamp -= frameTime * tapeRecorder.RewindSpeed;

                if (tapeRecorder.InsertedTape.TimeStamp <= 0) //stop rewinding when we get to the beginning of the tape
                {
                    tapeRecorder.InsertedTape.TimeStamp = 0;
                    tapeRecorder.CurrentMessageIndex = 0;
                    StopTape(tapeRecorder);
                    tapeRecorder.CurrentMode = TapeRecorderState.Play; // go into play mode once finished rewinding
                    return;
                }

            }
        }

        private void OnItemSlotChanged(EntityUid uid, TapeRecorderComponent component, ref ItemSlotChangedEvent args)
        {
            StopTape(component);

            if (!_containerSystem.TryGetContainer(uid, "cassette_tape", out var container) || container is not ContainerSlot slot)
                return;

            if (!TryComp<CassetteTapeComponent>(slot.ContainedEntity, out var cassetteTapeComponent))
            {
                component.CurrentMode = TapeRecorderState.Empty;
                component.InsertedTape = null;
                UpdateAppearance(component);
                return;
            }

            component.CurrentMode = TapeRecorderState.Idle;
            UpdateAppearance(component);
            component.InsertedTape = cassetteTapeComponent;
        }

        public void StartRecording(TapeRecorderComponent component)
        {
            if (component.InsertedTape == null)
                return;

            component.RecordingStartTime = component.AccumulatedTime - component.InsertedTape.TimeStamp;
            component.RecordingStartTimestamp = component.InsertedTape.TimeStamp;

            _popupSystem.PopupEntity("The tape recorder starts recording", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }

        public void StartPlaying(TapeRecorderComponent component)
        {
            component.CurrentMessageIndex = GetTapeIndex(component);
            _popupSystem.PopupEntity("The tape recorder starts playback", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }


        /// <summary>
        /// Gets the index in RecordedMessages of the message that will come next on the tape
        /// </summary>
        private static int GetTapeIndex(TapeRecorderComponent component)
        {
            if (component.InsertedTape == null || component.InsertedTape.RecordedMessages.Count == 0)
                return 0;

            //find the first index that comes after the current timestamp
            var index = component.InsertedTape.RecordedMessages.FindIndex(x => x.MessageTimeStamp > component.InsertedTape.TimeStamp);
            if (index == -1)
                return component.InsertedTape.RecordedMessages.Count;
            return index;
        }

        /// <summary>
        /// Flushes recorded buffer to tape memory. Overwrites data if the timestamps overlap
        /// </summary>
        private static void FlushBufferToMemory(TapeRecorderComponent component)
        {
            if (component.InsertedTape == null)
                return;

            component.InsertedTape.TimeStamp = (component.AccumulatedTime - component.RecordingStartTime);

            //Clear the recorded messages between the start and end of our recording timestamps, since we're overwriting this part of the tape
            component.InsertedTape.RecordedMessages.RemoveAll(x => x.MessageTimeStamp > component.RecordingStartTimestamp && x.MessageTimeStamp < component.InsertedTape.TimeStamp);
            component.InsertedTape.RecordedMessages.AddRange(component.RecordedMessageBuffer);

            component.RecordedMessageBuffer.Clear();

            //sort the list by timestamp
            component.InsertedTape.RecordedMessages.Sort((x, y) => x.MessageTimeStamp.CompareTo(y.MessageTimeStamp));

            }

        /// <summary>
        /// Handles Tape Recorder being stopped in any mode
        /// </summary>
        public void StopTape(TapeRecorderComponent component)
        {
            if (!component.Enabled)
                return;

            if (component.CurrentMode == TapeRecorderState.Record && component.Enabled)
                FlushBufferToMemory(component);

            _audioSystem.PlayPvs(component.StopSound, component.Owner);
            component.Enabled = false;
            UpdateAppearance(component);
        }

        private void UpdateAppearance(TapeRecorderComponent component)
        {
            if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
                return;

            if (!component.Enabled && component.InsertedTape != null)
            {
                _appearanceSystem.SetData(component.Owner, TapeRecorderVisuals.Status, TapeRecorderState.Idle);
                return;
            }

            _appearanceSystem.SetData(component.Owner, TapeRecorderVisuals.Status, component.CurrentMode);
        }

        private void OnUseInHand(EntityUid uid, TapeRecorderComponent component, UseInHandEvent args)
        {

            //Use in hand cooldown
            var currentTime = _gameTiming.CurTime;
            if (currentTime < component.CooldownEnd)
                return;
            component.LastUseTime = currentTime;
            component.CooldownEnd = component.LastUseTime + TimeSpan.FromSeconds(component.CooldownTime);


            if (component.Enabled || component.InsertedTape == null)
            {
                StopTape(component);
                return;
            }

            if (!component.Enabled)
                component.Enabled = true;
            UpdateAppearance(component);

            switch (component.CurrentMode) //idk how else to do this
            {
                case TapeRecorderState.Play:
                    StartPlaying(component);
                    break;
                case TapeRecorderState.Record:
                    StartRecording(component);
                    break;
                case TapeRecorderState.Rewind:
                    _audioSystem.PlayPvs(component.StartSound, component.Owner);
                    break;
                case TapeRecorderState.Idle:
                    break;
                case TapeRecorderState.Empty:
                    component.Enabled = false;
                    break;
            }
        }

        /// <summary>
        /// Records heard messages to the message buffer
        /// </summary>
        private void OnChatMessageHeard(EntityUid uid, TapeRecorderComponent component, ChatMessageHeardNearbyEvent args)
        {
            if (component.InsertedTape == null)
                return;

            if (component.CurrentMode != TapeRecorderState.Record || !component.Enabled)
                return;

            if (args.Channel != ChatChannel.Local) //filter out messages that aren't local chat (whispering should be picked up by the recorder, neither should emotes)
                return;

            component.InsertedTape.TimeStamp = (component.AccumulatedTime - component.RecordingStartTime);

            component.RecordedMessageBuffer.Add((component.InsertedTape.TimeStamp, "(" + TimeSpan.FromSeconds(component.InsertedTape.TimeStamp).ToString("mm\\:ss") + ") " + Name(args.Source) + ": " + args.Message));
        }

        private void OnExamined(EntityUid uid, TapeRecorderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (component.InsertedTape == null)
            {
                args.PushMarkup("[color=yellow]There is no cassette tape in the tape recorder.");
                return;
            }

            args.PushMarkup(TimeSpan.FromSeconds(component.InsertedTape.TimeStamp).ToString("mm\\:ss") + " / " + (TimeSpan.FromSeconds(component.InsertedTape.TapeMaxTime).ToString("mm\\:ss")));
        }


        //the verb sewer
        private void OnGetAltVerbs(EntityUid uid, TapeRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (component.CurrentMode == TapeRecorderState.Empty)
                return;

            if (component.CurrentMode != TapeRecorderState.Play)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Play",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Play mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Play;
                    },
                });
            }
            if (component.CurrentMode != TapeRecorderState.Record)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Record",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Record mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Record;
                    },
                });
            }
            if (component.CurrentMode != TapeRecorderState.Rewind)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Rewind",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Rewind mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Rewind;
                    },
                });
            }
        }

        public static VerbCategory RecorderModes = new("Tape Recorder Modes", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
    }
}
