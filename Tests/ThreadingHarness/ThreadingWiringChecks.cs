// Signature: OpenAI-Codex_Automated_Public_Transport_Vehicle_Management
using System;
using AutomatedPublicTransportVehicleManagement.Lifecycle;
using AutomatedPublicTransportVehicleManagement.Game;
using AutomatedPublicTransportVehicleManagement.Ui;

internal static class ThreadingWiringChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static int Main()
    {
        try
        {
            var oldExtension = new ObservationThreadingExtension();
            CitySession.Load(true);
            CitySession.Request();
            oldExtension.OnAfterSimulationFrame();
            Check(LineReader.Calls == 1 && CitySession.Display.Contains("Capture complete"),
                "ActiveProductionExtension_DrivesRealCapture");
            oldExtension.OnReleased();
            Check(ObservationPanel.Releases == 1 && CitySession.Display == "No playable city loaded.",
                "FirstRelease_ClearsOldSession");

            CitySession.Load(true);
            CitySession.Request();
            string newRequest = CitySession.Display;
            int reads = LineReader.Calls;
            int updates = ObservationPanel.Updates;
            oldExtension.OnAfterSimulationFrame();
            Check(LineReader.Calls == reads && CitySession.Display == newRequest,
                "RetiredFrame_DoesNotAdvanceNewCapture");
            oldExtension.OnUpdate(1f, 1f);
            Check(ObservationPanel.Updates == updates, "RetiredUpdate_DoesNotTouchUi");
            oldExtension.OnReleased();
            Check(ObservationPanel.Releases == 1 && CitySession.Display == newRequest,
                "RepeatOldRelease_DoesNotClearNewSession");

            var currentExtension = new ObservationThreadingExtension();
            currentExtension.OnAfterSimulationFrame();
            Check(LineReader.Calls == reads + 1 && CitySession.Display.Contains("Capture complete"),
                "CurrentExtension_CompletesPreservedNewCapture");
            string complete = CitySession.Display;
            oldExtension.OnReleased();
            oldExtension.OnAfterSimulationFrame();
            Check(CitySession.Display == complete && ObservationPanel.Releases == 1,
                "RetiredCallbacks_PreservePublishedNewCapture");
            currentExtension.OnUpdate(1f, 1f);
            Check(ObservationPanel.Updates == updates + 1, "CurrentUpdate_ReachesUiBoundary");
            currentExtension.OnReleased();
            Check(ObservationPanel.Releases == 2 && CitySession.Display == "No playable city loaded.",
                "CurrentRelease_StillCleansUp");
            var previewExtension = new ObservationThreadingExtension();
            CitySession.Load(true);
            LineReader.HasObservation = false;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("Observed records: 0; unavailable/incomplete: 0."),
                "EmptyCensus_ReportsZeroRecords");
            LineReader.HasObservation = true;
            LineReader.Actual = 0;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("actual=0;") &&
                CitySession.Display.Contains("Observed records: 1; unavailable/incomplete: 0."),
                "ValidZeroVehicles_IsNotAnEmptyCensus");
            LineReader.Actual = null;
            LineReader.Quality = AutomatedPublicTransportVehicleManagement.Model.ObservationQuality.Unavailable;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("actual=Unknown;") &&
                CitySession.Display.Contains("Observed records: 1; unavailable/incomplete: 1."),
                "UnknownVehicles_AreCountedAsUnavailable");
            LineReader.Actual = 2;
            LineReader.Quality = AutomatedPublicTransportVehicleManagement.Model.ObservationQuality.Valid;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("Observed records: 1; unavailable/incomplete: 0."),
                "Refresh_ResetsCensusSummary");
            LineReader.Actual = -1;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("discarded records: 1") &&
                CitySession.Display.Contains("Observed records: 0; unavailable/incomplete: 0."),
                "RejectedObservation_IsReportedNotSilentlyOmitted");
            LineReader.Actual = 2;
            CitySession.Request();
            previewExtension.OnAfterSimulationFrame();
            Check(CitySession.Display.Contains("discarded records: 0"),
                "Refresh_ResetsDiscardedRecordCount");
            previewExtension.OnReleased();
            Console.WriteLine("All " + checks + " production threading wiring checks passed.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine("FAIL " + exception); return 1; }
    }
}
