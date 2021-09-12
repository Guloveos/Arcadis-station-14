using Content.Server.Clothing.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Clothing
{
    public sealed class MagbootsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MagbootsComponent, GetInteractionVerbsEvent>(AddToggleVerb);
        }

        private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetInteractionVerbsEvent args)
        {
            if (args.User == null || !args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new("boot:toggle");
            verb.Text = Loc.GetString("toggle-magboots-verb-get-data-text");
            verb.Act = () => component.On = !component.On;
            // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
            args.Verbs.Add(verb);
        }
    }
}
