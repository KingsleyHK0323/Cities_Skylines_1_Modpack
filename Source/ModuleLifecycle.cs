// Signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage
using System;

namespace WirelessUtilitiesAndGlobalCoverage
{
    // This state belongs to one loading extension, never to the city's resource managers.
    internal sealed class ModuleLifecycle
    {
        private readonly Action<string> report;
        private Guid sessionId;

        internal ModuleLifecycle(Action<string> report)
        {
            this.report = report;
        }

        internal bool IsGameplayLoaded { get { return sessionId != Guid.Empty; } }
        internal Guid SessionId { get { return sessionId; } }

        internal void EnterGameplay()
        {
            if (IsGameplayLoaded) return;
            try
            {
                sessionId = Guid.NewGuid();
            }
            catch (Exception)
            {
                sessionId = Guid.Empty;
                ReportSafely("Could not create foundation session state. Utility control remains inactive.");
                return;
            }
            ReportSafely("Foundation loaded. All utility control remains inactive.");
        }

        internal void LeaveGameplay()
        {
            bool wasLoaded = IsGameplayLoaded;
            // Clear state before reporting so even a failed logger cannot retain the old session.
            sessionId = Guid.Empty;
            if (wasLoaded) ReportSafely("Foundation session cleared.");
        }

        private void ReportSafely(string message)
        {
            if (report == null) return;
            try
            {
                report(message);
            }
            catch (Exception)
            {
                // Reporting is optional and must not escape into a game loading callback.
            }
        }
    }
}
