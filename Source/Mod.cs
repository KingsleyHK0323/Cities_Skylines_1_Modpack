using System.Collections.Generic;
using System.Globalization;
using ColossalFramework.UI;
using ICities;
using AutomatedPublicTransportPlanning.Planning;
#if DEVTOOLS
using AutomatedPublicTransportPlanning.Spike;
#endif
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning
{
    /// <summary>
    /// Entry point the game looks for when it scans an assembly in the mods folder.
    /// Instantiated while the main menu loads, long before any city exists, so it
    /// must not touch simulation state.
    /// </summary>
    public sealed class Mod : IUserMod
    {
        public const string ModName = "Automated Public Transport Planning";
        public const string Version = "0.2.0";

        /// <summary>
        /// The name stays in English in every language. It is how the mod is found on
        /// the workshop and how players refer to it to each other; a translated product
        /// name makes it unsearchable for the very players it was translated for.
        /// </summary>
        public string Name
        {
#if DEVTOOLS
            // OpenAI-Codex: the content manager joins this name and the Development description.
            get { return ModName; }
#else
            get { return ModName + " " + Version; }
#endif
        }

        public string Description
        {
#if DEVTOOLS
            get { return "Development Version. This version has DevTool. Be careful; the development version can let your save be broken."; }
#else
            get { return Loc.Get(Strings.ModDescription); }
#endif
        }

        /// <summary>
        /// Every component whose text follows the language, paired with the key that
        /// supplies it.
        ///
        /// The list is cleared at the top of OnSettingsUI, which is the one moment the
        /// game guarantees the previous panel is gone. Holding these references is
        /// otherwise safe: they are components this mod created, handed back by the
        /// game's own API, and setting text on one is exactly what UIHelper does when
        /// it builds them.
        /// </summary>
        private static readonly List<KeyValuePair<object, string>> s_localised =
            new List<KeyValuePair<object, string>>();
        // The read-only preview is shared by Release and Development builds.
        // Signature: OpenAI-Codex_aptp_release_boundary.
        private static UITextField s_planningStatus;
#if DEVTOOLS
        // OpenAI-Codex: keep proposal creation disabled until a current selection is armed.
        private static UIDropDown s_routeProposalDropdown;
        private static UIButton s_routeCreateButton;
        private static bool s_routeCreationArmed;
        private static PlanningPreviewResult s_displayedPreview;
        private static int[] s_routeProposalIndexes = new int[0];
        private static int s_selectedRouteProposalIndex = -1;
#endif

        /// <summary>
        /// Lets go of the tracked components.
        ///
        /// Clearing only at the top of OnSettingsUI was not enough: a player who opens
        /// the settings page once and never returns leaves this list holding wrappers
        /// for components the game has destroyed, across level loads, for the rest of
        /// the session. Small, but it is this mod's own litter and it outlives the
        /// thing it describes. Loader calls this when a level is torn down.
        /// </summary>
        public static void ReleaseTrackedUI()
        {
            s_localised.Clear();
            s_planningStatus = null;
#if DEVTOOLS
            s_routeProposalDropdown = null;
            s_routeCreateButton = null;
            s_routeCreationArmed = false;
            s_displayedPreview = null;
            s_routeProposalIndexes = new int[0];
            s_selectedRouteProposalIndex = -1;
#endif
            PlanningCapture.ClearLatestPreview();
#if DEVTOOLS
            RouteCreationController.ResetForLevelChange();
            RouteEditingController.ResetForLevelChange();
            RouteEditingUi.Reset(); // OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
#endif
        }

        /// <summary>Called on the main thread by PlanningThreadingExtension.OnUpdate.</summary>
        internal static void UpdatePlanningStatus(string text)
        {
#if DEVTOOLS
            RouteEditingUi.Refresh(); // OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
            RefreshRouteProposalOptions(PlanningCapture.LatestPreview, false);
            UpdateRouteCreateButton();
#endif
            UITextField status = s_planningStatus;
            if (status != null)
            {
                string display = text ?? string.Empty;
                display = display.Replace(PlanningEngine.SnapshotStalePlaceholder,
                                         Loc.Get(Strings.SnapshotStaleNote));
                display = display.Replace(PlanningEngine.DemandProxyPlaceholder,
                                         Loc.Get(Strings.DemandProxyNote));
#if DEVTOOLS
                display = display + "\n\n" + RouteCreationController.StatusText;
#endif
                status.text = display;
            }
        }

        /// <summary>
        /// Called when the player opens this mod's entry in the content manager.
        /// UIHelperBase only covers simple option controls; the in-game planning
        /// panel is built separately against the game's own UI framework.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // The game builds this panel from scratch each time and discards the last
            // one, so anything tracked from a previous visit is already dead.
            s_localised.Clear();

            // Logged in English on purpose, and it names the language in use. When a
            // player reports that the panel is in the wrong language, this line says
            // whether the setting was read correctly or the table is what is wrong.
            Log.Info("Settings UI requested. Language is '" + Loc.CurrentLanguage + "'.");

            // Deliberately not inside a group: a group would add a title that also needs
            // retranslating, and the dropdown's own label already says what it is.
            UIDropDown picker = helper.AddDropdown(Loc.Get(Strings.LabelLanguage),
                                                   Loc.LanguageNames,
                                                   Loc.CurrentIndex,
                                                   OnLanguageChanged) as UIDropDown;

            if (picker != null)
            {
                // UIDropDown.OnMouseWheel moves the selection and then calls Use() on
                // the event, so the page does not scroll. Left on, a player scrolling
                // this page changes language once per notch — each one writing the
                // settings file and rewriting every label — while the page sits still.
                picker.enableMouseWheel = false;
            }

            TrackDropdownLabel(picker, Strings.LabelLanguage);

#if DEVTOOLS
            // Keep diagnostic controls out of the Release build.
            UIHelperBase diagnostics = helper.AddGroup(Loc.Get(Strings.GroupDiagnostics));

            UIButton testLog = Track(diagnostics.AddButton(Loc.Get(Strings.ButtonTestLog),
                                     Guarded("diagnostics", OnDiagnosticsButton)) as UIButton,
                                     Strings.ButtonTestLog);
            TrackGroupTitle(testLog, Strings.GroupDiagnostics);

            Track(diagnostics.AddButton(Loc.Get(Strings.ButtonBusPrefabs),
                  Guarded("bus prefab dump", BusPrefabResolver.DumpBusPrefabs)) as UIButton,
                  Strings.ButtonBusPrefabs);

            // This older probe remains an optional read-only diagnostic. Planning
            // snapshots and previews use the sliced simulation-frame pipeline below.
            Track(diagnostics.AddButton(Loc.Get(Strings.ButtonMeasure),
                  Guarded("performance probe", PerformanceProbe.Run)) as UIButton,
                  Strings.ButtonMeasure);

            RouteEditingUi.Build(helper); // OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
#endif
            UIHelperBase planning = helper.AddGroup(Loc.Get(Strings.LabelPlanningStatus));
            UIButton preview = Track(planning.AddButton(Loc.Get(Strings.ButtonPlanningPreview),
                Guarded("planning preview", delegate { PlanningCapture.RequestPreview(); })) as UIButton,
                Strings.ButtonPlanningPreview);
            TrackGroupTitle(preview, Strings.LabelPlanningStatus);

            s_planningStatus = planning.AddTextfield(Loc.Get(Strings.LabelPlanningStatus),
                PlanningCapture.DisplayText, null) as UITextField;
            if (s_planningStatus != null)
            {
                s_planningStatus.multiline = true;
                s_planningStatus.readOnly = true;
                s_planningStatus.height = 280f;
            }

#if DEVTOOLS
            s_routeCreationArmed = false;
            s_routeProposalDropdown = null;
            s_routeCreateButton = null;
            s_displayedPreview = null;
            s_routeProposalIndexes = new int[0];
            s_selectedRouteProposalIndex = -1;

            UIHelperBase routeCreation = helper.AddGroup(Loc.Get(Strings.GroupRouteCreation));
            UICheckBox routeArm = Track(routeCreation.AddCheckbox(Loc.Get(Strings.CheckRouteArm),
                false, OnRouteCreationArmedChanged) as UICheckBox, Strings.CheckRouteArm);
            TrackGroupTitle(routeArm, Strings.GroupRouteCreation);

            s_routeProposalDropdown = Track(routeCreation.AddDropdown(
                Loc.Get(Strings.LabelRouteProposal), new string[] { Loc.Get(Strings.NoEligibleRouteProposal) },
                0, OnRouteProposalChanged) as UIDropDown, Strings.LabelRouteProposal);
            TrackDropdownLabel(s_routeProposalDropdown, Strings.LabelRouteProposal);

            s_routeCreateButton = Track(routeCreation.AddButton(Loc.Get(Strings.ButtonRouteCreate),
                Guarded("route proposal creation confirmation", ConfirmSelectedRouteProposal)) as UIButton,
                Strings.ButtonRouteCreate);
            if (s_routeCreateButton != null) { s_routeCreateButton.isEnabled = false; }
            RefreshRouteProposalOptions(PlanningCapture.LatestPreview, true);
#endif

#if DEVTOOLS
            // Throwaway controls for the save round-trip spike. Creating a test line
            // changes the loaded city and requires a fresh confirmation on every
            // click. The destructive remove operation is disabled because deleted
            // routes cannot be restored by this tool.
            //
            // The create button also remains disarmed until the checkbox is ticked.
            // Arming resets every time the page is opened and is not remembered.
            s_spikeArmed = false;
            s_spikeCreateButton = null;

            UIHelperBase spike = helper.AddGroup(Loc.Get(Strings.GroupSpike));

            UICheckBox arm = Track(spike.AddCheckbox(Loc.Get(Strings.CheckSpikeArm),
                                   false, OnSpikeArmedChanged) as UICheckBox,
                                   Strings.CheckSpikeArm);
            TrackGroupTitle(arm, Strings.GroupSpike);

            s_spikeCreateButton = Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeCreate),
                Guarded("create test line confirmation",
                    ConfirmedEveryClick(Strings.SpikeConfirmTitle, Strings.SpikeConfirmCreate,
                        Guarded("create test line", ArmedOnly(SaveRoundTripSpike.CreateTestLine))))) as UIButton,
                Strings.ButtonSpikeCreate);
            if (s_spikeCreateButton != null)
            {
                s_spikeCreateButton.isEnabled = false;
            }

            Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeReport),
                  Guarded("report test lines", SaveRoundTripSpike.ReportTestLines)) as UIButton,
                  Strings.ButtonSpikeReport);

            UIButton removeTestLines = Track(spike.AddButton(Loc.Get(Strings.ButtonSpikeRemove),
                Guarded("disabled test line removal", delegate
                {
                    Log.Warning("Test line removal is disabled because deleted routes cannot be restored by this tool.");
                })) as UIButton, Strings.ButtonSpikeRemove);
            if (removeTestLines != null)
            {
                removeTestLines.isEnabled = false;
            }

#endif
        }

#if DEVTOOLS
        private static void RefreshRouteProposalOptions(PlanningPreviewResult preview, bool force)
        {
            if (s_routeProposalDropdown == null || (!force && object.ReferenceEquals(preview, s_displayedPreview)))
            {
                return;
            }

            s_displayedPreview = preview;
            List<string> options = new List<string>();
            List<int> proposalIndexes = new List<int>();
            if (preview != null)
            {
                for (int i = 0; i < preview.Proposals.Count; i++)
                {
                    RouteProposal proposal = preview.Proposals[i];
                    if (!RouteCreationRules.HasValidStopSites(proposal.StopSites)) { continue; }
                    options.Add(string.Format(CultureInfo.InvariantCulture,
                        Loc.Get(Strings.RouteProposalFormat), i + 1, proposal.StopCount,
                        proposal.CoverageIndex.ToString("F1", CultureInfo.InvariantCulture)));
                    proposalIndexes.Add(i);
                }
            }

            if (options.Count == 0) { options.Add(Loc.Get(Strings.NoEligibleRouteProposal)); }
            s_routeProposalIndexes = proposalIndexes.ToArray();
            s_routeProposalDropdown.items = options.ToArray();
            s_routeProposalDropdown.selectedIndex = 0;
            s_selectedRouteProposalIndex = s_routeProposalIndexes.Length == 0
                ? -1 : s_routeProposalIndexes[0];
            UpdateRouteCreateButton();
        }

        private static void OnRouteProposalChanged(int selection)
        {
            if (selection < 0 || selection >= s_routeProposalIndexes.Length)
            {
                s_selectedRouteProposalIndex = -1;
            }
            else
            {
                s_selectedRouteProposalIndex = s_routeProposalIndexes[selection];
            }
            UpdateRouteCreateButton();
        }

        private static void OnRouteCreationArmedChanged(bool isChecked)
        {
            s_routeCreationArmed = isChecked;
            UpdateRouteCreateButton();
            Log.Info(isChecked
                ? "Route proposal creation armed. Every click still requires confirmation."
                : "Route proposal creation disarmed.");
        }

        private static void UpdateRouteCreateButton()
        {
            if (s_routeCreateButton != null)
            {
                s_routeCreateButton.isEnabled = s_routeCreationArmed &&
                    s_selectedRouteProposalIndex >= 0 &&
                    object.ReferenceEquals(PlanningCapture.LatestPreview, s_displayedPreview) &&
                    !RouteCreationController.IsBusy;
            }
        }

        private static void ConfirmSelectedRouteProposal()
        {
            if (!s_routeCreationArmed)
            {
                Log.Warning("Ignored route proposal creation because the control is disarmed.");
                return;
            }

            PlanningPreviewResult preview = PlanningCapture.LatestPreview;
            if (!object.ReferenceEquals(preview, s_displayedPreview))
            {
                RefreshRouteProposalOptions(preview, true);
                Log.Warning("Ignored route proposal creation because the displayed proposal list was stale; select again after it refreshes.");
                return;
            }

            int proposalIndex = s_selectedRouteProposalIndex;
            if (preview == null || proposalIndex < 0 || proposalIndex >= preview.Proposals.Count)
            {
                Log.Warning("Ignored route proposal creation because no current proposal is selected.");
                return;
            }

            ConfirmPanel.ShowModal(Loc.Get(Strings.RouteConfirmTitle), Loc.Get(Strings.RouteConfirmCreate),
                delegate(UIComponent component, int result)
                {
                    if (result == 1 && s_routeCreationArmed)
                    {
                        RouteCreationController.RequestCreate(preview, proposalIndex);
                    }
                });
        }
#endif

        // ------------------------------------------------------------------ language

        /// <summary>
        /// Guarded like the button handlers are. The game dispatches dropdown and
        /// checkbox events with a bare delegate call and no try/catch of its own, so an
        /// exception here reaches the UI input dispatcher.
        /// </summary>
        private static void OnLanguageChanged(int selection)
        {
            Log.Guard("language change", delegate
            {
                string[] order = Loc.Languages;
                if (selection < 0 || selection >= order.Length)
                {
                    return;
                }

                Loc.SetLanguage(order[selection]);
                Retranslate();
#if DEVTOOLS
                RefreshRouteProposalOptions(PlanningCapture.LatestPreview, true);
#endif
            });
        }

        /// <summary>
        /// Rewrites every tracked label in the language now selected.
        ///
        /// Components are compared against null through Unity's own operator, which
        /// reports a destroyed object as null, so a panel torn down between the change
        /// and this call is skipped rather than throwing.
        /// </summary>
        private static void Retranslate()
        {
            for (int i = 0; i < s_localised.Count; i++)
            {
                string text = Loc.Get(s_localised[i].Value);
                object component = s_localised[i].Key;

                // UIButton and UILabel both take their text from UITextComponent.
                UITextComponent textComponent = component as UITextComponent;
                if (textComponent != null)
                {
                    textComponent.text = text;
                    continue;
                }

                // UICheckBox descends straight from UIComponent and has its own.
                UICheckBox checkBox = component as UICheckBox;
                if (checkBox != null)
                {
                    checkBox.text = text;
                }
            }
        }

        private static T Track<T>(T component, string key) where T : UIComponent
        {
            if (component != null)
            {
                s_localised.Add(new KeyValuePair<object, string>(component, key));
                return component;
            }

            // Said out loud on purpose. The label lookups below walk a hierarchy built
            // by templates that live in an asset bundle, so the number of levels between
            // a control and its caption cannot be confirmed from any managed assembly —
            // it was read off UIHelper's source and is an assumption until it runs.
            //
            // Silently skipping would leave a label that never changes language and no
            // way to tell whether the hierarchy is wrong or Retranslate never ran. This
            // line answers that on the first run instead of costing a second one.
            Log.Warning("No UI component found to localise for key '" + key +
                        "'; that label will not follow the language.");
            return component;
        }

        /// <summary>
        /// A dropdown's caption is a sibling UILabel named "Label", inside the panel
        /// the template created. UIHelper.AddDropdown sets it the same way.
        /// </summary>
        private static void TrackDropdownLabel(UIDropDown dropdown, string key)
        {
            if (dropdown == null || dropdown.parent == null)
            {
                return;
            }

            Track(dropdown.parent.Find<UILabel>("Label"), key);
        }

        /// <summary>
        /// A group's title is a UILabel named "Label" on the group panel, but AddGroup
        /// hands back a helper over the panel's "Content" child, and the root behind
        /// that helper is private. Climbing from a control inside the group reaches the
        /// same label without touching anything private: control -> Content -> group.
        /// </summary>
        private static void TrackGroupTitle(UIComponent childOfGroup, string key)
        {
            if (childOfGroup == null || childOfGroup.parent == null || childOfGroup.parent.parent == null)
            {
                return;
            }

            Track(childOfGroup.parent.parent.Find<UILabel>("Label"), key);
        }

#if DEVTOOLS
        // ------------------------------------------------------------------ spike arming

        /// <summary>
        /// Whether the spike's writing buttons are currently allowed to act. Static
        /// because OnSettingsUI hands the game delegates that outlive this call.
        /// </summary>
        private static bool s_spikeArmed;
        private static UIButton s_spikeCreateButton;

        private static void OnSpikeArmedChanged(bool isChecked)
        {
            Log.Guard("spike arming", delegate
            {
                s_spikeArmed = isChecked;
                if (s_spikeCreateButton != null)
                {
                    s_spikeCreateButton.isEnabled = isChecked;
                }

                Log.Info(isChecked
                    ? "Spike create button armed. Every click still requires confirmation."
                    : "Spike buttons disarmed.");
            });
        }

        /// <summary>
        /// Blocks a handler until the player has explicitly armed the spike group.
        /// Button 2 is read-only and is not wrapped.
        /// </summary>
        private static OnButtonClicked ArmedOnly(OnButtonClicked handler)
        {
            return delegate
            {
                if (!s_spikeArmed)
                {
                    Log.Warning("Ignored: tick the checkbox above first. These buttons change your city.");
                    return;
                }

                handler();
            };
        }

        /// <summary>
        /// Shows a new confirmation dialog for every click. The action runs only
        /// from the affirmative callback; cancellation has no side effects.
        /// </summary>
        private static OnButtonClicked ConfirmedEveryClick(string titleKey, string messageKey,
                                                           OnButtonClicked onConfirm)
        {
            return delegate
            {
                ConfirmPanel.ShowModal(Loc.Get(titleKey), Loc.Get(messageKey),
                    delegate(UIComponent component, int result)
                    {
                        if (result == 1)
                        {
                            onConfirm();
                        }
                    });
            };
        }
#endif

        /// <summary>
        /// Wraps a button handler so nothing it throws reaches the game's UI event
        /// dispatcher.
        /// </summary>
        private static OnButtonClicked Guarded(string context, OnButtonClicked handler)
        {
            return delegate { Log.Guard(context, delegate { handler(); }); };
        }

#if DEVTOOLS
        private static void OnDiagnosticsButton()
        {
            Log.Info("Diagnostics button pressed. Mod assembly is loaded and responding.");
        }
#endif
    }
}
