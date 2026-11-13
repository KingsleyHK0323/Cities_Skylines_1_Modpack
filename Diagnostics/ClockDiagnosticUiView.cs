// Signature: OpenAI-Codex_diagnostic_impl
// Detached review candidate. It is not registered as a game mod or extension.
using System;
using System.Threading;
using ColossalFramework.UI;
using ICities;

namespace RealisticCityTime.Diagnostics
{
    // The caller is responsible for a suitable UI thread and existing helper. Comparing managed
    // thread IDs cannot authenticate a thread or prove Unity UI affinity. UI hosts or extensions
    // may retain copies, propagate events or create logs; actual game behavior is unverified.
    internal sealed class ClockDiagnosticUiView
    {
        private const int MaximumTextLength = 8192;
        private const string RiskDisclosure = "Existing GameMod separately reads a game date. Relative observations are game state; "
            + "host UI or extensions may retain copies, propagate events, or create logs.";
        private const string InitialText = "Diagnostic status is not confirmed. No automatic game callback is registered. " + RiskDisclosure;
        private const string Disclosure = "\n" + RiskDisclosure;
        private const string IncompleteText = "Results unavailable or incomplete.";
        private readonly ClockDiagnosticPanelPresenter presenter;
        private readonly int ownerThreadId;
        private UITextField display;
        private bool shown;
        private bool ready;
        private bool usable;
        private bool actionInProgress;
        private long generation;
        private volatile bool stopNotConfirmed = true;

        internal ClockDiagnosticUiView(ClockDiagnosticPanelPresenter presenter)
        {
            if (presenter == null) throw new ArgumentNullException("presenter");
            this.presenter = presenter;
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        // This is local state only; it does not prove that every callback has ended.
        internal bool StopNotConfirmed { get { return stopNotConfirmed; } }

        // A host can request invalidation from any managed thread without touching UI.
        // This is not connected to game unload or Unity lifecycle callbacks.
        internal bool RevokeFromHost() { return presenter.RevokeFromHost(); }

        private bool IsOwnerThreadId
        {
            get { return Thread.CurrentThread.ManagedThreadId == ownerThreadId; }
        }

        // A caller explicitly supplies the UI helper and a local, unauthenticated generation.
        // Registration-time and reentrant callbacks do not arm the diagnostic.
        internal bool Show(UIHelperBase helper, long suppliedGeneration)
        {
            if (!IsOwnerThreadId || shown || helper == null || suppliedGeneration <= 0
                || !presenter.TryBeginViewPresentation()) return false;
            shown = true;
            try
            {
                display = helper.AddTextfield("Clock diagnostic status and results", InitialText,
                    delegate(string ignored) { }, null) as UITextField;
                if (display == null || !presenter.CanContinueViewPresentation) return false;
                display.maxLength = MaximumTextLength;
                if (!presenter.CanContinueViewPresentation) return false;
                display.numericalOnly = false;
                if (!presenter.CanContinueViewPresentation) return false;
                display.submitOnFocusLost = true;
                if (!presenter.CanContinueViewPresentation) return false;
                display.readOnly = true;
                if (!presenter.CanContinueViewPresentation) return false;
                usable = true;
                if (!TrySetText(InitialText)) return false;
                if (helper.AddButton("Start diagnostic", StartClicked) == null || !presenter.CanContinueViewPresentation) return false;
                if (helper.AddButton("Stop diagnostic", StopClicked) == null || !presenter.CanContinueViewPresentation) return false;
                if (helper.AddButton("Refresh diagnostic", RefreshClicked) == null || !presenter.CanContinueViewPresentation) return false;
                generation = suppliedGeneration;
                ready = true;
                return true;
            }
            catch (Exception)
            {
                usable = false;
                return false;
            }
            finally { presenter.EndViewPresentation(); }
        }

        private void TryShowIncomplete()
        {
            if (display == null || !presenter.CanContinueViewPresentation) return;
            try { display.text = IncompleteText; }
            catch (Exception) { }
        }

        private bool TrySetText(string value)
        {
            if (!usable || display == null || !presenter.CanContinueViewPresentation) return false;
            if (value == null || value.Length >= MaximumTextLength)
            {
                usable = false;
                TryShowIncomplete();
                return false;
            }
            try
            {
                bool readOnly = display.readOnly;
                if (!presenter.CanContinueViewPresentation) return false;
                bool numericalOnly = display.numericalOnly;
                if (!presenter.CanContinueViewPresentation) return false;
                int maxLength = display.maxLength;
                if (!presenter.CanContinueViewPresentation) return false;
                if (!readOnly || numericalOnly || maxLength < MaximumTextLength)
                {
                    usable = false;
                    TryShowIncomplete();
                    return false;
                }
                display.text = value;
                if (!presenter.CanContinueViewPresentation) return false;
                string shownText = display.text;
                if (!presenter.CanContinueViewPresentation) return false;
                readOnly = display.readOnly;
                if (!presenter.CanContinueViewPresentation) return false;
                numericalOnly = display.numericalOnly;
                if (!presenter.CanContinueViewPresentation) return false;
                maxLength = display.maxLength;
                if (!presenter.CanContinueViewPresentation) return false;
                if (String.Equals(shownText, value, StringComparison.Ordinal)
                    && readOnly && !numericalOnly && maxLength >= MaximumTextLength) return true;
            }
            catch (Exception) { }
            usable = false;
            TryShowIncomplete();
            return false;
        }

        private bool EnterAction()
        {
            if (!IsOwnerThreadId || !ready || actionInProgress || !presenter.TryBeginViewPresentation()) return false;
            actionInProgress = true;
            return true;
        }

        private void ExitAction()
        {
            actionInProgress = false;
            presenter.EndViewPresentation();
        }

        private void StartClicked()
        {
            if (!EnterAction()) return;
            bool started = false;
            try
            {
                if (!TrySetText("Start requested; status is pending. No automatic game callback is registered. " + RiskDisclosure)) return;
                started = presenter.StartClicked(generation);
                if (!started)
                {
                    stopNotConfirmed = true;
                    TrySetText("Start not confirmed: busy or already used. Stop if needed. " + RiskDisclosure);
                    return;
                }
                stopNotConfirmed = true;
                if (!TrySetText("Diagnostic armed; no automatic game callback is registered. Stop before Refresh. " + RiskDisclosure))
                    stopNotConfirmed = !presenter.StopClicked();
            }
            catch (Exception)
            {
                if (started) stopNotConfirmed = !presenter.StopClicked();
            }
            finally { ExitAction(); }
        }

        private void StopClicked()
        {
            if (!EnterAction()) return;
            try
            {
                bool stopped = presenter.StopClicked();
                stopNotConfirmed = !stopped;
                TrySetText(stopped
                    ? "Diagnostic stopped. Click Refresh to view relative endpoint records. " + RiskDisclosure
                    : "Stop not confirmed. Retry Stop if appropriate. " + RiskDisclosure);
            }
            catch (Exception)
            {
                stopNotConfirmed = true;
                TrySetText("Stop not confirmed. Retry Stop. " + RiskDisclosure);
            }
            finally { ExitAction(); }
        }

        private void RefreshClicked()
        {
            if (!EnterAction()) return;
            try
            {
                if (!usable) { TryShowIncomplete(); return; }
                string result = presenter.RefreshWithinPresentation();
                if (result == null) { TrySetText("Results unavailable or not confirmed."); return; }
                TrySetText(result + Disclosure);
            }
            catch (Exception)
            {
                usable = false;
                TryShowIncomplete();
            }
            finally { ExitAction(); }
        }
    }
}
