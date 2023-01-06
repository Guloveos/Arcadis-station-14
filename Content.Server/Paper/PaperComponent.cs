using Content.Shared.Paper;
using Robust.Shared.GameStates;

namespace Content.Server.Paper
{
    [NetworkedComponent, RegisterComponent]
    public sealed class PaperComponent : SharedPaperComponent
    {
        public PaperAction Mode;
        [DataField("content")]
        public string Content { get; set; } = "";

        [DataField("useLocale")]
        public bool UseLocale;

        [DataField("contentSize")]
        public int ContentSize { get; set; } = 1000;

        [DataField("stampedBy")]
        public List<string> StampedBy { get; set; } = new();
        /// <summary>
        ///     Stamp to be displayed on the paper, state from beauracracy.rsi
        /// </summary>
        [DataField("stampState")]
        public string? StampState { get; set; }
    }
}
