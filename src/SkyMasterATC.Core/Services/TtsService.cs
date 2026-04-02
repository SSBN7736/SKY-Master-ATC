namespace SkyMasterATC.Core.Services
{
    /// <summary>
    /// Stub for the text-to-speech engine that will generate realistic ATC/pilot audio.
    /// Phase 5 will integrate a TTS backend (e.g., Windows SAPI or an online engine).
    /// </summary>
    public sealed class TtsService
    {
        /// <summary>
        /// Speaks the supplied <paramref name="text"/> using the configured TTS engine.
        /// Currently a no-op; will be implemented in Phase 5.
        /// </summary>
        public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            // TODO (Phase 5): invoke TTS backend.
            return Task.CompletedTask;
        }
    }
}
