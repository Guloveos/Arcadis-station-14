using JetBrains.Annotations;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    [UsedImplicitly]
    public sealed class DepartmentTimeRequirement : JobRequirement
    {
        [DataField("department")]
        public string Department = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;
    }
}
