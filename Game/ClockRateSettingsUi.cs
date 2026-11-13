// Signature: OpenAI-Codex
using System;
using System.Globalization;
using System.Threading;
using ColossalFramework.UI;
using ICities;

namespace RealisticCityTime.Game
{
    // This store exists only in process memory. It never accesses a game or platform settings file.
    internal sealed class TemporaryClockRateStore : IClockRateStore
    {
        private string value;
        public string Read() { return value; }
        public ClockRateStoreWriteStatus Write(string next)
        {
            value = next;
            return ClockRateStoreWriteStatus.Completed;
        }
    }

    internal static class TemporaryClockRateHost
    {
        internal static readonly ClockRateSettings Settings = Create();
        private static ClockRateSettings Create()
        {
            ClockRateSettings settings = new ClockRateSettings(new TemporaryClockRateStore());
            settings.Load();
            return settings;
        }
    }

    /// <summary>Temporary input only; there is no persistent setting or native clock writer.</summary>
    internal sealed class ClockRateSettingsUi
    {
        private sealed class StatusDisplay
        {
            private readonly UITextField field;
            private int reliable = 1;
            internal StatusDisplay(UITextField field) { this.field = field; }
            internal bool Reliable { get { return Interlocked.CompareExchange(ref reliable, 1, 1) == 1; } }
            internal void Set(string message)
            {
                if (!Reliable) return;
                try
                {
                    field.text = message;
                    if (!String.Equals(field.text, message, StringComparison.Ordinal))
                        Interlocked.Exchange(ref reliable, 0);
                }
                catch (Exception) { Interlocked.Exchange(ref reliable, 0); }
            }
        }

        private const string InitialStatus = "This panel shows a snapshot. Not saved; the game clock is unchanged.";
        private const string UnavailableStatus = "Temporary setting unavailable. Not saved; the game clock is unchanged.";
        private const int EditorMaxLength = 128;
        private readonly ClockRateSettings settings;

        internal ClockRateSettingsUi(ClockRateSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            this.settings = settings;
        }

        internal void Show(UIHelperBase helper)
        {
            if (helper == null) return;
            ClockRateInput draft = new ClockRateInput(settings);
            string initialDraft = draft.Draft;
            UITextField status = null;
            StatusDisplay display = null;
            UITextField editor = helper.AddTextfield("Temporary multiplier (1.00-10.00, use '.', up to 2 decimals)",
                initialDraft, delegate(string text)
                {
                    draft.Edit(text);
                    if (display != null) display.Set(settings.NeedsReview ? UnavailableStatus
                        : "Not saved. Press Apply; the game clock is unchanged.");
                }, null) as UITextField;
            if (editor == null) return;
            // One extra visible character makes an overlong, truncated input rejectable.
            editor.maxLength = EditorMaxLength;
            editor.submitOnFocusLost = true;
            editor.numericalOnly = false;
            editor.readOnly = false;
            editor.text = initialDraft;
            if (!String.Equals(editor.text, initialDraft, StringComparison.Ordinal)) return;
            string initialStatus = settings.NeedsReview ? UnavailableStatus : InitialStatus;
            status = helper.AddTextfield("Temporary setting status", initialStatus,
                delegate(string unused) { }, null) as UITextField;
            if (status == null) return;
            status.maxLength = 512;
            status.submitOnFocusLost = true;
            status.readOnly = true;
            display = new StatusDisplay(status);
            display.Set(initialStatus);
            if (!display.Reliable) return;

            helper.AddButton("Apply temporary multiplier", delegate
            {
                if (!display.Reliable) return;
                // A focus-loss cancel can change visible text without a change callback.
                string displayed = editor.text;
                if (displayed == null || displayed.Length >= EditorMaxLength)
                {
                    display.Set(settings.NeedsReview
                        ? "Invalid multiplier. Temporary setting unavailable; not saved, and the game clock is unchanged."
                        : "Invalid multiplier. Not saved; the game clock is unchanged.");
                    return;
                }
                ClockRateUpdateStatus result = draft.SubmitText(displayed);
                if (result == ClockRateUpdateStatus.Applied || result == ClockRateUpdateStatus.Unchanged)
                {
                    decimal submitted;
                    if (!ClockRateSettings.TryParse(displayed, out submitted))
                    {
                        display.Set(UnavailableStatus);
                        return;
                    }
                    string value = submitted.ToString("0.00", CultureInfo.InvariantCulture);
                    display.Set("This panel last submitted " + value + "x; not saved, and the game clock is unchanged.");
                }
                else if (result == ClockRateUpdateStatus.InvalidInput)
                    display.Set(settings.NeedsReview
                        ? "Invalid multiplier. Temporary setting unavailable; not saved, and the game clock is unchanged."
                        : "Invalid multiplier. Not saved; the game clock is unchanged.");
                else
                    display.Set(UnavailableStatus);
            });
#if VERIFIED_UI_CANCEL
            // Disabled until the game's UI event serialization has been established.
            helper.AddButton("Cancel edit", delegate
            {
                if (!display.Reliable) return;
                if (!draft.TryCancel())
                {
                    display.Set("Temporary setting busy. Not saved; the game clock is unchanged.");
                    return;
                }
                string restored = draft.Draft ?? String.Empty;
                editor.text = restored;
                if (!String.Equals(editor.text, restored, StringComparison.Ordinal))
                {
                    display.Set(UnavailableStatus);
                    return;
                }
                display.Set(settings.NeedsReview ? UnavailableStatus
                    : "Edit cancelled. Not saved; the game clock is unchanged.");
            });
#endif
        }
    }
}
