// Signature: OpenAI-Codex_Automated_Public_Transport_Vehicle_Management
// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using System;
using System.Text;
using ICities;
using ColossalFramework;
using AutomatedPublicTransportVehicleManagement.Game;
using AutomatedPublicTransportVehicleManagement.Model;
using AutomatedPublicTransportVehicleManagement.State;
using AutomatedPublicTransportVehicleManagement.Ui;

namespace AutomatedPublicTransportVehicleManagement.Lifecycle
{
    internal static class CitySession
    {
        private static readonly object Gate = new object();
        private static int session;
        private static bool active;
        private static bool requested;
        private static int cursor;
        private static int observedRecords;
        private static int unavailableRecords;
        private static int discardedRecords;
        private static long sequence;
        private static int captureSession;
        private static StringBuilder capture;
        private static string display = "Load a city to observe bus routes.";
        internal static string Display { get { lock (Gate) { return display; } } }
        internal static void Load(bool enabled)
        {
            lock (Gate) { session++; active = enabled; requested = false; capture = null; display = enabled ? "Ready. Select Refresh to capture a read-only census." : "No playable city loaded."; }
        }
        internal static void Unload() { Load(false); }
        internal static void Request()
        {
            lock (Gate) { if (!active) { display = "No playable city loaded."; return; } requested = true; capture = null; display = "Capture requested; waiting for simulation frames."; }
        }
        internal static void Step()
        {
            lock (Gate)
            {
                if (!active) return;
                if (requested)
                {
                    requested = false; cursor = 1; observedRecords = 0; unavailableRecords = 0; discardedRecords = 0; sequence++; captureSession = session;
                    capture = new StringBuilder("Read-only city-bus census (collected across simulation frames).\nTarget counts and waiting passengers: Unknown.\n");
                }
                if (capture == null || captureSession != session) return;
                if (!Singleton<TransportManager>.exists) { capture = null; display = "Transport manager unavailable."; return; }
                try
                {
                    int size = Math.Min(Singleton<TransportManager>.instance.m_lines.m_buffer.Length, ushort.MaxValue + 1);
                    int end = Math.Min(cursor + 8, size);
                    for (; cursor < end; cursor++)
                    {
                        LineObservation observation;
                        if (LineReader.TryCapture((ushort)cursor, captureSession, sequence, out observation))
                        {
                            if (!ObservationGate.CanDisplay(observation, new LineKey(captureSession, (ushort)cursor, sequence)))
                            {
                                discardedRecords++;
                                continue;
                            }
                            observedRecords++;
                            if (observation.Quality != ObservationQuality.Valid || !observation.ActualVehicles.HasValue)
                                unavailableRecords++;
                            // Cap presentation size independently of the number of scanned slots.
                            if (capture.Length < 24000)
                                capture.Append("Line ").Append(cursor).Append(": actual=")
                                    .Append(observation.ActualVehicles.HasValue ? observation.ActualVehicles.Value.ToString() : "Unknown")
                                    .Append("; target=Unknown; quality=").Append(observation.Quality).Append('\n');
                        }
                    }
                    if (cursor >= size)
                    {
                        capture.Append("Observed records: ").Append(observedRecords)
                            .Append("; unavailable/incomplete: ").Append(unavailableRecords).Append(". discarded records: ").Append(discardedRecords).Append(".\n");
                        display = "City session " + captureSession + "; observation " + sequence + ".\n" + capture.ToString() + (capture.Length >= 24000 ? "\nDisplay truncated; refresh to recapture." : "\nCapture complete. Values are a historical census, not live dispatch state.");
                        capture = null;
                    }
                }
                catch (Exception) { capture = null; display = "Capture unavailable. No simulation settings were changed."; }
            }
        }
    }
    public sealed class CityLoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            CitySession.Load(mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario);
        }
        public override void OnLevelUnloading() { CitySession.Unload(); base.OnLevelUnloading(); }
        public override void OnReleased() { CitySession.Unload(); base.OnReleased(); }
    }
    public sealed class ObservationThreadingExtension : ThreadingExtensionBase
    {
        // Signature: OpenAI-Codex_Automated_Public_Transport_Vehicle_Management
        // Serialize release with this instance's callbacks; a retired instance cannot touch a later city.
        private readonly object callbackGate = new object();
        private bool released;
        public override void OnReleased()
        {
            lock (callbackGate)
            {
                if (released) return;
                released = true;
                CitySession.Unload();
                ObservationPanel.Release();
                base.OnReleased();
            }
        }
        public override void OnAfterSimulationFrame()
        {
            lock (callbackGate) { if (!released) CitySession.Step(); }
        }
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            lock (callbackGate) { if (!released) ObservationPanel.Update(); }
        }
    }
}
