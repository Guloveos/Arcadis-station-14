using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class EarlyEnglishAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EarlyEnglishAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, EarlyEnglishAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "earlyenglish");

        if (!_random.Prob(component.ForsoothChance))
            return msg;

        // Changes words like "Looked" to "Look'd"
        msg = Regex.Replace(msg, "ed", "'d");
        // "LOOKED" to "LOOK'D"
        msg = Regex.Replace(msg, "ED", "'D");

        var pick = _random.Pick(component.EarlyEnglishWords);
        // Reverse sanitize capital
        msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        msg = Loc.GetString(pick) + " " + msg;

        return msg;
    }

    private void OnAccentGet(EntityUid uid, EarlyEnglishAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
