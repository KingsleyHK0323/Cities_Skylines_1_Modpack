// Signature: OpenAI-Codex_diagnostic_tests
// Test-only UI-shaped fakes. No ICities or Unity UI assembly is loaded.
using System;
using System.Collections.Generic;
using System.Threading;

namespace ICities
{
    public delegate void OnTextChanged(string value);
    public delegate void OnTextSubmitted(string value);
    public delegate void OnButtonClicked();
    public abstract class UIHelperBase
    {
        public abstract object AddTextfield(string text, string defaultContent,
            OnTextChanged changed, OnTextSubmitted submitted);
        public abstract object AddButton(string text, OnButtonClicked clicked);
    }
}

namespace ColossalFramework.UI
{
    public sealed class UITextField
    {
        private readonly int owner = Thread.CurrentThread.ManagedThreadId;
        private string value = String.Empty;
        private int maximum = 1024;
        private bool isReadOnly;
        private bool isNumericalOnly;
        private bool focusSubmit;
        public int AccessCount;
        public int WrongThreadAccessCount;
        public bool FailOnRead;
        public bool FailOnWrite;
        public bool IgnoreReadOnly;
        public Func<string, string> Filter;
        public Action OnNextTextWrite;
        public ICities.OnTextChanged Changed;
        // Simulated host copies let tests distinguish new writes from already retained text.
        public readonly List<string> HostTextCopies = new List<string>();

        private void Touch(bool writing)
        {
            Interlocked.Increment(ref AccessCount);
            if (Thread.CurrentThread.ManagedThreadId != owner)
                Interlocked.Increment(ref WrongThreadAccessCount);
            if (writing ? FailOnWrite : FailOnRead)
                throw new InvalidOperationException("Synthetic UI field is unavailable.");
        }

        public bool readOnly
        {
            get { Touch(false); return isReadOnly; }
            set { Touch(true); if (!IgnoreReadOnly) isReadOnly = value; }
        }

        public bool numericalOnly
        {
            get { Touch(false); return isNumericalOnly; }
            set { Touch(true); isNumericalOnly = value; }
        }

        public bool submitOnFocusLost
        {
            get { Touch(false); return focusSubmit; }
            set { Touch(true); focusSubmit = value; }
        }

        public int maxLength
        {
            get { Touch(false); return maximum; }
            set
            {
                Touch(true);
                maximum = Math.Max(0, value);
                if (this.value.Length > maximum) text = this.value.Substring(0, maximum);
            }
        }

        public string text
        {
            get { Touch(false); return value; }
            set
            {
                Touch(true);
                Action action = OnNextTextWrite;
                OnNextTextWrite = null;
                if (action != null) action();
                string visible = value ?? String.Empty;
                if (visible.Length > maximum) visible = visible.Substring(0, maximum);
                if (Filter != null) visible = Filter(visible) ?? String.Empty;
                HostTextCopies.Add(visible);
                if (this.value == visible) return;
                this.value = visible;
                if (Changed != null) Changed(visible);
            }
        }
    }
}

internal sealed class FakeDiagnosticUiHelper : ICities.UIHelperBase
{
    private readonly Dictionary<string, ICities.OnButtonClicked> buttons =
        new Dictionary<string, ICities.OnButtonClicked>(StringComparer.Ordinal);
    internal readonly ColossalFramework.UI.UITextField Field = new ColossalFramework.UI.UITextField();
    internal bool EagerText;
    internal bool EagerButtons;
    internal bool NullField;
    internal int NullButtonAt;
    internal int ButtonRegistrations;
    internal Action<string> AfterEagerButton;
    internal Action AfterEagerText;

    public override object AddTextfield(string text, string defaultContent,
        ICities.OnTextChanged changed, ICities.OnTextSubmitted submitted)
    {
        if (NullField) return null;
        if (changed == null) throw new ArgumentNullException("changed");
        Field.Changed = changed;
        Field.text = defaultContent;
        if (EagerText && changed != null)
        {
            changed(defaultContent);
            if (AfterEagerText != null) AfterEagerText();
        }
        return Field;
    }

    public override object AddButton(string text, ICities.OnButtonClicked clicked)
    {
        ButtonRegistrations++;
        buttons[text] = clicked;
        if (EagerButtons && clicked != null)
        {
            clicked();
            if (AfterEagerButton != null) AfterEagerButton(text);
        }
        return NullButtonAt == ButtonRegistrations ? null : new object();
    }

    internal bool Click(string text)
    {
        ICities.OnButtonClicked clicked;
        if (!buttons.TryGetValue(text, out clicked) || clicked == null) return false;
        clicked();
        return true;
    }
}
