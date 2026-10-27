namespace Example.Settings
{
    /// <summary>
    /// Settings that outlive the Settings screen, so reopening it shows what was chosen last.
    /// </summary>
    public sealed class GameSettings
    {
        public float Volume { get; set; } = 0.5f;
        public bool Muted { get; set; }
    }
}
