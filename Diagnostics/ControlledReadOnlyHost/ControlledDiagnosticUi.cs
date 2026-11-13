// Signature: OpenAI-Codex_lifecycle_scope_review
// UI state is a snapshot; Unity thread affinity and persistence across unload are unverified.
using System;
using System.Threading;
using ColossalFramework.UI;
using ICities;

namespace RealisticCityTime.Diagnostics
{
    internal sealed class ControlledDiagnosticUi
    {
        private const int StatusLimit = 2048;
        private const int ResultLimit = 8192;
        private const string Disclosure = " City identity and lifecycle continuity are unverified; loading flags may change and change back (ABA). "
            + "Relative results are game state. Limit: 8 frame and 8 display records. "
            + "The first display record is a baseline; reaching either limit does not stop recording. "
            + "Counter frequency is not recorded. UI hosts or extensions may retain or forward results. "
            + "The separate existing GameMod reads an absolute game date if active.";
        private const string EmptyResult = "No results shown. Start manually, confirm Stop, then Refresh." + Disclosure;
        private readonly ControlledReadOnlyHost host;
        private readonly ControlledDiagnosticPresentation presentation;
        private readonly int ownerThread;
        private UITextField status;
        private UITextField result;
        private bool shown;
        private bool ready;
        private bool actionInProgress;
        private bool statusReliable = true;
        private bool resultReliable = true;
        private int viewEpoch;

        internal ControlledDiagnosticUi(ControlledReadOnlyHost host)
        {
            if (host == null) throw new ArgumentNullException("host");
            this.host = host;
            presentation = host.CreatePresentation();
            ownerThread = Thread.CurrentThread.ManagedThreadId;
        }

        internal bool StatusDisplayReliable { get { return statusReliable; } }

        private bool IsOwnerThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == ownerThread; }
        }

        internal bool Show(UIHelperBase helper)
        {
            if (shown || actionInProgress) return false;
            if (!IsOwnerThread || helper == null)
            {
                shown = true;
                host.RevokeAfterFailedView();
                return false;
            }
            shown = true;
            actionInProgress = true;
            bool completed = false;
            try
            {
                viewEpoch = host.BeginView();
                if (!host.IsCurrentView(viewEpoch)) return false;
                status = helper.AddTextfield("Clock diagnostic status",
                    "Diagnostic status is not confirmed. Use Check status." + Disclosure,
                    delegate(string ignored) { }, null) as UITextField;
                if (!CanAccessField(false)) return false;
                result = helper.AddTextfield("Clock diagnostic results", EmptyResult,
                    delegate(string ignored) { }, null) as UITextField;
                if (status == null || result == null || !host.IsCurrentView(viewEpoch)) return false;
                if (!PrepareField(status, StatusLimit) || !PrepareField(result, ResultLimit)) return false;
                if (!WriteText(result, EmptyResult, ResultLimit, false) || !RenderStatus()) return false;
                if (helper.AddButton("Start diagnostic", StartClicked) == null || !CanAccessField(false)) return false;
                if (helper.AddButton("Stop diagnostic", StopClicked) == null || !CanAccessField(false)) return false;
                if (helper.AddButton("Refresh diagnostic", RefreshClicked) == null || !CanAccessField(false)) return false;
                if (helper.AddButton("Retry revocation", RetryClicked) == null || !CanAccessField(false)) return false;
                if (helper.AddButton("Check status", CheckStatusClicked) == null || !CanAccessField(false)) return false;
                if (!host.IsCurrentView(viewEpoch)) return false;
                ready = true;
                completed = true;
                return true;
            }
            catch (Exception)
            {
                statusReliable = false;
                return false;
            }
            finally
            {
                actionInProgress = false;
                // A failed current page permanently closes admission, including an in-flight Start.
                if (!completed && host.IsCurrentView(viewEpoch)) host.RevokeAfterFailedView();
            }
        }

        private bool CanAccessField(bool requireResultPresentation)
        {
            if (!IsOwnerThread || !host.IsCurrentView(viewEpoch)) return false;
            return !requireResultPresentation || (host.CanRefreshFromView(viewEpoch)
                && presentation.CanContinue(viewEpoch));
        }

        private bool PrepareField(UITextField field, int limit)
        {
            try
            {
                if (!CanAccessField(false)) return false;
                field.maxLength = limit;
                if (!CanAccessField(false)) return false;
                field.numericalOnly = false;
                if (!CanAccessField(false)) return false;
                field.submitOnFocusLost = true;
                if (!CanAccessField(false)) return false;
                field.readOnly = true;
                if (!CanAccessField(false)) return false;
                bool readOnly = field.readOnly;
                if (!CanAccessField(false)) return false;
                bool numericalOnly = field.numericalOnly;
                if (!CanAccessField(false)) return false;
                int maxLength = field.maxLength;
                return CanAccessField(false) && readOnly && !numericalOnly && maxLength >= limit;
            }
            catch (Exception) { return false; }
        }

        private bool WriteText(UITextField field, string value, int limit, bool requireResultPresentation)
        {
            if (field == null || value == null || value.Length >= limit) return false;
            try
            {
                if (!CanAccessField(requireResultPresentation)) return false;
                bool readOnly = field.readOnly;
                if (!CanAccessField(requireResultPresentation)) return false;
                bool numericalOnly = field.numericalOnly;
                if (!CanAccessField(requireResultPresentation)) return false;
                int maxLength = field.maxLength;
                if (!CanAccessField(requireResultPresentation) || !readOnly || numericalOnly || maxLength < limit) return false;
                field.text = value;
                if (!CanAccessField(requireResultPresentation)) return false;
                string actual = field.text;
                if (!CanAccessField(requireResultPresentation)) return false;
                readOnly = field.readOnly;
                if (!CanAccessField(requireResultPresentation)) return false;
                numericalOnly = field.numericalOnly;
                if (!CanAccessField(requireResultPresentation)) return false;
                maxLength = field.maxLength;
                return CanAccessField(requireResultPresentation) && String.Equals(actual, value, StringComparison.Ordinal)
                    && readOnly && !numericalOnly && maxLength >= limit;
            }
            catch (Exception) { return false; }
        }

        private bool RenderStatus()
        {
            if (!host.IsCurrentView(viewEpoch)) return false;
            ControlledHostState before = host.State;
            string text = "Diagnostic status: " + before.ToString()
                + ". This is a snapshot; use Check status. In-flight host work: "
                + host.InFlightTickets.ToString() + "."
                + (resultReliable ? "" : " Result display unavailable or unconfirmed.") + Disclosure;
            bool success = WriteText(status, text, StatusLimit, false);
            if (!host.IsCurrentView(viewEpoch)) return false;
            if (success && host.State != before)
                success = WriteText(status, "State changed during display. Use Check status."
                    + (resultReliable ? "" : " Result display unavailable or unconfirmed.") + Disclosure, StatusLimit, false);
            if (!success) statusReliable = false;
            return success;
        }

        private bool EnterAction()
        {
            if (!IsOwnerThread || !ready || actionInProgress || !host.IsCurrentView(viewEpoch)) return false;
            actionInProgress = true;
            return true;
        }

        private void StartClicked()
        {
            if (!EnterAction()) return;
            bool started = false;
            bool presenting = false;
            try
            {
                if (!statusReliable || !WriteText(status, "Start requested; confirmation pending." + Disclosure, StatusLimit, false))
                {
                    statusReliable = false;
                    return;
                }
                presenting = presentation.TryBegin(viewEpoch);
                if (!presenting || !presentation.CanContinue(viewEpoch))
                {
                    if (host.IsCurrentView(viewEpoch)) RenderStatus();
                    return;
                }
                started = host.StartFromView(viewEpoch);
                if (started && host.IsCurrentView(viewEpoch) && !RenderStatus()) host.StopFromView(viewEpoch);
                else if (!started && host.IsCurrentView(viewEpoch)) RenderStatus();
            }
            catch (Exception)
            {
                statusReliable = false;
                if (started) host.StopFromView(viewEpoch);
            }
            finally
            {
                if (presenting) presentation.End();
                actionInProgress = false;
            }
        }

        private void StopClicked()
        {
            if (!EnterAction()) return;
            try { host.StopFromView(viewEpoch); RenderStatus(); }
            catch (Exception) { statusReliable = false; }
            finally { actionInProgress = false; }
        }

        private void RefreshClicked()
        {
            if (!EnterAction()) return;
            bool presenting = false;
            try
            {
                if (!host.CanRefreshFromView(viewEpoch)) { RenderStatus(); return; }
                presenting = presentation.TryBegin(viewEpoch);
                if (!presenting || !host.CanRefreshFromView(viewEpoch))
                {
                    resultReliable = false;
                    RenderStatus();
                    return;
                }
                string text = presentation.Refresh(viewEpoch);
                if (text == null || !host.CanRefreshFromView(viewEpoch) || !presentation.CanContinue(viewEpoch))
                {
                    resultReliable = false;
                    RenderStatus();
                    return;
                }
                resultReliable = WriteText(result, text + Disclosure, ResultLimit, true);
                RenderStatus();
            }
            catch (Exception) { statusReliable = false; }
            finally
            {
                if (presenting) presentation.End();
                actionInProgress = false;
            }
        }

        private void RetryClicked()
        {
            if (!EnterAction()) return;
            try { host.RetryRevocationFromView(viewEpoch); RenderStatus(); }
            catch (Exception) { statusReliable = false; }
            finally { actionInProgress = false; }
        }

        private void CheckStatusClicked()
        {
            if (!EnterAction()) return;
            try { RenderStatus(); }
            catch (Exception) { statusReliable = false; }
            finally { actionInProgress = false; }
        }
    }
}
