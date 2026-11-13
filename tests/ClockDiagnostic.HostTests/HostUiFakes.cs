// Signature: OpenAI-Codex_native_validation_options
// In-memory host UI. It does not create or access a real Unity UI.
using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using ICities;

internal sealed class HostUiFake : UIHelperBase
{
    private readonly Dictionary<string, OnButtonClicked> buttons =
        new Dictionary<string, OnButtonClicked>(StringComparer.Ordinal);
    internal readonly List<UITextField> Fields = new List<UITextField>();
    internal readonly List<string> Labels = new List<string>();
    internal bool EagerText;
    internal bool EagerButtons;
    internal int NullFieldAt;
    internal Action<string> AfterEagerButton;

    public override object AddTextfield(string label, string initial, OnTextChanged changed,
        OnTextSubmitted submitted)
    {
        if (changed == null) throw new ArgumentNullException("changed");
        if (NullFieldAt == Fields.Count + 1) return null;
        UITextField field = new UITextField();
        field.text = initial ?? String.Empty;
        field.Changed = changed;
        Fields.Add(field);
        Labels.Add(label);
        if (EagerText) changed(initial);
        return field;
    }

    public override object AddButton(string label, OnButtonClicked clicked)
    {
        if (clicked == null) throw new ArgumentNullException("clicked");
        buttons[label] = clicked;
        if (EagerButtons)
        {
            clicked();
            if (AfterEagerButton != null) AfterEagerButton(label);
        }
        return new object();
    }

    internal bool Click(string label)
    {
        OnButtonClicked action;
        if (!buttons.TryGetValue(label, out action)) return false;
        action();
        return true;
    }

    internal bool ContainsNewResult(int fieldIndex, int afterCount)
    {
        List<string> copies = Fields[fieldIndex].HostTextCopies;
        for (int i = afterCount; i < copies.Count; i++)
            if (copies[i] != null && copies[i].IndexOf("Frame[", StringComparison.Ordinal) >= 0) return true;
        return false;
    }
}
