using System.Speech.Synthesis;
using SkyMasterATC.Core.Models;

namespace SkyMasterATC.Core.Services
{
    /// <summary>
    /// Text-to-speech engine using Windows SAPI via <see cref="SpeechSynthesizer"/>.
    /// Calls are serialised internally so simultaneous requests are queued rather than
    /// interrupted (unless cancelled).
    /// </summary>
    public sealed class TtsService : IDisposable
    {
        private readonly SpeechSynthesizer? _synth;
        private readonly SemaphoreSlim _sem = new(1, 1);
        private bool _disposed;

        public TtsService()
        {
            try
            {
                _synth = new SpeechSynthesizer();
                _synth.SetOutputToDefaultAudioDevice();
            }
            catch
            {
                // TTS unavailable (no audio device, wrong platform, etc.)
                _synth = null;
            }
        }

        /// <summary>Speaks <paramref name="text"/> asynchronously, waiting for any current speech to finish.</summary>
        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            if (_synth is null || string.IsNullOrWhiteSpace(text))
                return;

            await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _synth.Speak(text);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _synth.SpeakAsyncCancelAll();
            }
            finally
            {
                _sem.Release();
            }
        }

        /// <summary>Builds a natural ATC phrase from a command and speaks it.</summary>
        public Task SpeakAtcPhrase(AtcCommand command, string callsign,
            CancellationToken cancellationToken = default)
        {
            var phrase = BuildPhrase(command, callsign);
            return SpeakAsync(phrase, cancellationToken);
        }

        /// <summary>Selects the TTS voice by name.</summary>
        public void SetVoice(string voiceName)
        {
            try { _synth?.SelectVoice(voiceName); }
            catch { /* ignore invalid voice */ }
        }

        /// <summary>Returns the names of all installed SAPI voices.</summary>
        public IReadOnlyList<string> GetAvailableVoices()
        {
            if (_synth is null) return [];
            return _synth.GetInstalledVoices()
                         .Select(v => v.VoiceInfo.Name)
                         .ToList();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _synth?.Dispose();
            _sem.Dispose();
            _disposed = true;
        }

        // ── Phrase builder ────────────────────────────────────────────────────────

        private static string BuildPhrase(AtcCommand command, string callsign) =>
            command.CommandType switch
            {
                AtcCommandType.AltitudeAssignment =>
                    $"{callsign}, descend and maintain flight level {(int)(command.Value / 100)}",
                AtcCommandType.HeadingVector =>
                    $"{callsign}, fly heading {(int)command.Value:D3}",
                AtcCommandType.SpeedRestriction =>
                    $"{callsign}, reduce speed to {(int)command.Value} knots",
                AtcCommandType.TakeoffClearance =>
                    $"{callsign}, cleared for takeoff",
                AtcCommandType.LandingClearance =>
                    $"{callsign}, cleared to land",
                AtcCommandType.Pushback =>
                    $"{callsign}, push back and start up approved",
                AtcCommandType.TaxiClearance =>
                    $"{callsign}, taxi to holding point via taxiway alpha",
                AtcCommandType.Scramble =>
                    $"{callsign}, scramble, scramble, scramble",
                _ =>
                    $"{callsign}, roger",
            };
    }
}
