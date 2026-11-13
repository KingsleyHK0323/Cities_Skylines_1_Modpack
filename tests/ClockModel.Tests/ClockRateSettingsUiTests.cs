// Signature: OpenAI-Codex
using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using ICities;
using RealisticCityTime.Game;

internal static class ClockRateSettingsUiTests
{
    private sealed class FakeStore : IClockRateStore
    {
        internal string Value;
        internal int Writes;
        internal bool FailWrite;
        public string Read() { return Value; }
        public ClockRateStoreWriteStatus Write(string value)
        {
            Writes++;
            if (FailWrite) throw new InvalidOperationException("Synthetic write failure.");
            Value = value;
            return ClockRateStoreWriteStatus.Completed;
        }
    }

    private sealed class FakeHelper : UIHelperBase
    {
        internal readonly List<UITextField> Fields = new List<UITextField>();
        internal readonly List<OnButtonClicked> Buttons = new List<OnButtonClicked>();
        internal readonly List<string> Labels = new List<string>();
        internal bool FailSecondField;
        internal bool HasSubmittedCallback;
        internal int TemplateMaxLength = 1024;
        internal bool TemplateSubmitOnFocusLost;
        internal bool TemplateNumericalOnly;
        internal bool TemplateReadOnly;
        internal Func<string, string> StatusFilter;

        public override object AddTextfield(string text, string defaultContent,
            OnTextChanged changed, OnTextSubmitted submitted)
        {
            if (String.IsNullOrEmpty(text) || changed == null) throw new Exception("Invalid fake text field arguments.");
            HasSubmittedCallback |= submitted != null;
            if (FailSecondField && Fields.Count == 1) return null;
            UITextField field = new UITextField();
            field.maxLength = TemplateMaxLength;
            field.submitOnFocusLost = TemplateSubmitOnFocusLost;
            field.numericalOnly = TemplateNumericalOnly;
            field.readOnly = TemplateReadOnly;
            if (Fields.Count == 1) field.Filter = StatusFilter;
            field.text = defaultContent;
            field.Changed = changed;
            Fields.Add(field); Labels.Add(text);
            return field;
        }

        public override object AddButton(string text, OnButtonClicked clicked)
        {
            if (String.IsNullOrEmpty(text) || clicked == null) throw new Exception("Invalid fake button arguments.");
            Buttons.Add(clicked);
            return new object();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Temporary rate UI: " + message);
    }

    private static bool Contains(string text, string part)
    {
        return text != null && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void TemporaryInputUsesDisplayedValue()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "memory store starts missing");
        FakeHelper helper = new FakeHelper();
        new ClockRateSettingsUi(settings).Show(helper);
        Check(helper.Fields.Count == 2 && helper.Buttons.Count == 1, "input, status and explicit apply are exposed");
        Check(!helper.HasSubmittedCallback && helper.Fields[1].readOnly, "focus submission is not an apply callback");
        Check(helper.Fields[0].maxLength == 128 && Contains(helper.Labels[0], "use '.'"),
            "field uses a bounded rejection sentinel and names the decimal separator");
        Check(Contains(helper.Labels[0], "temporary") && Contains(helper.Fields[1].text, "not saved")
            && Contains(helper.Fields[1].text, "clock"), "temporary non-persistent non-controlling status is visible");
        UITextField input = helper.Fields[0];
        input.text = "2.50";
        Check(settings.Multiplier == 1m && store.Writes == 0, "editing does not apply or write");
        input.SimulateCancelWithoutChangeEvent("1.00");
        helper.Buttons[0]();
        Check(settings.Multiplier == 1m && store.Writes == 0, "apply uses visible text after a silent cancel");
        input.text = "3.25";
        helper.Buttons[0]();
        Check(settings.Multiplier == 3.25m && store.Writes == 1, "explicit button applies temporary value");
        Check(Contains(helper.Fields[1].text, "last submitted") && Contains(helper.Fields[1].text, "not saved") && Contains(helper.Fields[1].text, "clock"),
            "applied result still states its limits");
        input.text = "10.01";
        helper.Buttons[0]();
        Check(settings.Multiplier == 3.25m && store.Writes == 1 && Contains(helper.Fields[1].text, "invalid"),
            "invalid visible text is rejected with feedback");
        input.text = new string('0', 127) + "11";
        helper.Buttons[0]();
        Check(settings.Multiplier == 3.25m && store.Writes == 1 && Contains(helper.Fields[1].text, "invalid"),
            "a long truncated numeric prefix is rejected instead of becoming valid");
        input.text = "7.50";
#if VERIFIED_UI_CANCEL
        helper.Buttons[1]();
        Check(input.text == "3.25" && settings.Multiplier == 3.25m && store.Writes == 1,
            "cancel restores the temporary active value without writing");
#endif
        FakeHelper reopened = new FakeHelper();
        new ClockRateSettingsUi(settings).Show(reopened);
        Check(reopened.Fields[0].text == "3.25" && Contains(reopened.Fields[1].text, "not saved"),
            "reopened panel shows the temporary active value");
        reopened.Fields[0].text = "4.00";
        reopened.Buttons[0]();
        Check(settings.Multiplier == 4m && Contains(helper.Fields[1].text, "Press Apply"),
            "other panel updates do not make this panel claim a current value");
    }

    private static void MissingControlAndStoreFailure()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "failure fixture starts missing");
        FakeHelper incomplete = new FakeHelper();
        incomplete.FailSecondField = true;
        new ClockRateSettingsUi(settings).Show(incomplete);
        Check(incomplete.Buttons.Count == 0 && store.Writes == 0, "incomplete UI exposes no apply action");
        FakeHelper helper = new FakeHelper();
        new ClockRateSettingsUi(settings).Show(helper);
        store.FailWrite = true;
        helper.Fields[0].text = "2.00";
        helper.Buttons[0]();
        Check(settings.Multiplier == 1m && Contains(helper.Fields[1].text, "unavailable"), "failed temporary update reports unavailable");
        helper.Fields[0].text = "3.00";
        Check(Contains(helper.Fields[1].text, "unavailable"), "editing does not hide unresolved review status");
        helper.Buttons[0]();
        Check(store.Writes == 1 && Contains(helper.Fields[1].text, "unavailable"), "unknown store failure is not retried blindly");
#if VERIFIED_UI_CANCEL
        helper.Buttons[1]();
        Check(Contains(helper.Fields[1].text, "unavailable"), "cancelling does not hide unresolved review status");
#endif
        helper.Fields[0].text = "invalid";
        helper.Buttons[0]();
        Check(Contains(helper.Fields[1].text, "invalid") && Contains(helper.Fields[1].text, "unavailable"),
            "invalid input must not hide unresolved review status");
    }

    private static void TemplateConstraintsAreNeutralized()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "template fixture starts missing");
        FakeHelper helper = new FakeHelper();
        helper.TemplateMaxLength = 0;
        helper.TemplateSubmitOnFocusLost = false;
        helper.TemplateNumericalOnly = true;
        helper.TemplateReadOnly = true;
        new ClockRateSettingsUi(settings).Show(helper);
        Check(helper.Fields[0].text == "1.00", "initial text is restored after template truncation");
        Check(Contains(helper.Fields[1].text, "not saved") && Contains(helper.Fields[1].text, "clock"),
            "status text is restored after template truncation");
        Check(helper.Fields[0].submitOnFocusLost && !helper.HasSubmittedCallback,
            "focus loss does not cancel input or trigger an apply callback");
        Check(!helper.Fields[0].numericalOnly && !helper.Fields[0].readOnly,
            "template cannot force locale-dependent numeric filtering or a read-only editor");
        Check(helper.Fields[1].submitOnFocusLost, "status focus loss cannot silently restore an old warning");
        helper.Fields[0].text = "2.50";
        if (!helper.Fields[0].submitOnFocusLost) helper.Fields[0].SimulateCancelWithoutChangeEvent("1.00");
        helper.Buttons[0]();
        Check(settings.Multiplier == 2.5m, "explicit apply retains the value entered before focus moved");
    }

    internal static void RunTemplateRed() { TemplateConstraintsAreNeutralized(); }
    internal static void RunQuarantineRed() { MissingControlAndStoreFailure(); }

    private static void TwoPanelsDoNotClaimAnOldValueIsCurrent()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "two-panel store starts missing");
        FakeHelper first = new FakeHelper();
        FakeHelper second = new FakeHelper();
        new ClockRateSettingsUi(settings).Show(first);
        new ClockRateSettingsUi(settings).Show(second);
        first.Fields[0].text = "2.00";
        first.Buttons[0]();
        second.Fields[0].text = "3.00";
        second.Buttons[0]();
        Check(settings.Multiplier == 3m && Contains(first.Fields[1].text, "last submitted 2.00"),
            "older panel status identifies its own submission instead of claiming the current setting");
    }

    private static void FilteredStatusDisablesApply()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "filtered-status store starts missing");
        FakeHelper helper = new FakeHelper();
        helper.StatusFilter = delegate(string text)
        {
            return Contains(text, "Press Apply") ? "filtered warning" : text;
        };
        new ClockRateSettingsUi(settings).Show(helper);
        Check(helper.Buttons.Count == 1, "initial status was accepted by the fake filter");
        helper.Fields[0].text = "2.00";
        helper.Buttons[0]();
        Check(store.Writes == 0 && settings.Multiplier == 1m,
            "a filtered status blocks later apply instead of hiding the warning");
    }

    internal static void Run()
    {
        Type ui = typeof(ClockRateSettingsUiTests).Assembly.GetType("RealisticCityTime.Game.ClockRateSettingsUi");
        if (ui == null) throw new Exception("ClockRateSettingsUi is missing; no clearly temporary input candidate exists.");
        TemporaryInputUsesDisplayedValue();
        MissingControlAndStoreFailure();
        TemplateConstraintsAreNeutralized();
        TwoPanelsDoNotClaimAnOldValueIsCurrent();
        FilteredStatusDisablesApply();
        FakeHelper realEntry = new FakeHelper();
        new RealisticCityTimeMod().OnSettingsUI(realEntry);
        Check(realEntry.Fields.Count == 2 && realEntry.Buttons.Count == 1, "mod settings entry delegates to temporary UI");
        Console.WriteLine("PASS temporary rate UI fixtures with explicit apply; no game or real storage accessed");
    }
}
