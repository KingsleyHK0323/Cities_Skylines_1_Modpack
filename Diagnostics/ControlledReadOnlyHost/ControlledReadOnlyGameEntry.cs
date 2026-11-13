// Signature: OpenAI-Codex_lifecycle_scope_review
// Separate candidate entry point. No game execution or Unity lookup behavior has been verified.
using System;
using ICities;

namespace RealisticCityTime.Diagnostics
{
    internal sealed class UnityExistingManagerSource : IExistingManagerSource
    {
        // This native lookup is invoked only by an explicit Start attempt on the caller's UI thread.
        // Its native side effects, thread requirements, and the lifetime of returned objects are unknown.
        public bool TryGetExisting(out SimulationManager simulation, out LoadingManager loading)
        {
            simulation = null;
            loading = null;
            try
            {
                UnityEngine.Object[] simulations = UnityEngine.Object.FindObjectsOfType(typeof(SimulationManager));
                UnityEngine.Object[] loadings = UnityEngine.Object.FindObjectsOfType(typeof(LoadingManager));
                if (simulations == null || simulations.Length != 1 || loadings == null || loadings.Length != 1)
                    return false;
                SimulationManager foundSimulation = simulations[0] as SimulationManager;
                LoadingManager foundLoading = loadings[0] as LoadingManager;
                if (foundSimulation == null || foundLoading == null) return false;
                simulation = foundSimulation;
                loading = foundLoading;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    internal static class ControlledDiagnosticGameHost
    {
        internal static readonly ControlledReadOnlyHost Instance =
            new ControlledReadOnlyHost(new UnityExistingManagerSource());
    }

    public sealed class ControlledReadOnlyDiagnosticMod : IUserMod
    {
        private ControlledDiagnosticUi view;
        public string Name { get { return "Controlled Read-Only Clock Diagnostic"; } }
        public string Description
        {
            get { return "One manually started, relative clock observation; game behavior is unverified."; }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            view = new ControlledDiagnosticUi(ControlledDiagnosticGameHost.Instance);
            view.Show(helper);
        }
    }

    public sealed class ControlledReadOnlyDiagnosticLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            bool gameplay = mode == LoadMode.NewGame || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario || mode == LoadMode.LoadScenario;
            ControlledDiagnosticGameHost.Instance.OnLevelLoaded(gameplay);
        }

        public override void OnLevelUnloading()
        {
            ControlledDiagnosticGameHost.Instance.OnLevelUnloading();
        }

        public override void OnReleased()
        {
            ControlledDiagnosticGameHost.Instance.OnReleased();
        }
    }

    public sealed class ControlledReadOnlyDiagnosticThreading : ThreadingExtensionBase
    {
        public override void OnBeforeSimulationFrame()
        {
            ControlledDiagnosticGameHost.Instance.BeforeFrame();
        }

        public override void OnAfterSimulationFrame()
        {
            ControlledDiagnosticGameHost.Instance.AfterFrame();
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            ControlledReadOnlyHost host = ControlledDiagnosticGameHost.Instance;
            if (host.State == ControlledHostState.StopPending) host.RetryStop();
            if (host.State == ControlledHostState.RevokePending) host.RetryRevocation();
            if (host.State == ControlledHostState.Recording) host.DisplayUpdate();
        }

        public override void OnReleased()
        {
            ControlledDiagnosticGameHost.Instance.OnReleased();
        }
    }
}
