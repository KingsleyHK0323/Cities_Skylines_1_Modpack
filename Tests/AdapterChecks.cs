// Signature: OpenAI-Codex_Realistic_Pedestrian_Traffic_Control
using System;
using System.Threading;
using PedestrianTrafficControl;
internal static class AdapterChecks {
    private static int checks;
    private static void Check(bool condition, string name) {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++; Console.WriteLine("PASS: " + name);
    }
    private static CrossingObservation Sample() {
        return new CrossingObservation("city-a",10,20,true,3,4,CrossingRule.Allow,
            CrossingGeometry.Supported,CrossingSignal.NotSignalized,MarkingState.Unknown,
            ObservationSource.Synthetic,null);
    }
    private static CrossingPreview Read(CrossingReadAdapter adapter) {
        return adapter.Read("city-a",10,20,true,3,4);
    }
    internal static int Run() {
        checks = 0;
        CrossingReadAdapter missing = new CrossingReadAdapter(null);
        missing.BeginSession("city-a");
        Check(Read(missing).Status == PreviewStatus.Unavailable && Read(missing).Source == ObservationSource.Unknown,
            "adapter without verified reader cannot invent a game observation");
        int reads = 0;
        CrossingReadAdapter adapter = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) {
            reads++; return Sample();
        });
        Check(Read(adapter).Status == PreviewStatus.Unavailable && reads == 0,"unopened session never calls reader");
        adapter.BeginSession("city-a");
        CrossingPreview value = Read(adapter);
        Check(value.Status == PreviewStatus.Observed && value.Source == ObservationSource.Synthetic,
            "adapter preserves synthetic provenance");
        Check(value.WouldHideMarkingAfterConfirmedDeny == false,"adapter does not infer a ban from unknown marking");
        int previousReads = reads;
        Check(adapter.Read("city-a",0,20,true,3,4).Status == PreviewStatus.Unavailable && reads == previousReads,
            "invalid identity rejected before calling reader");
        adapter.BeginSession("city-b");
        Check(Read(adapter).Status == PreviewStatus.Stale && reads == previousReads,"old city rejected before calling reader");
        adapter.BeginSession("city-a");
        Check(adapter.Read("city-a",10,20,true,3,5).Status == PreviewStatus.Stale,"source revision mismatch stays stale");
        adapter.Invalidate();
        Check(Read(adapter).Status == PreviewStatus.Unavailable,"invalidated session never returns cached success");
        CrossingReadAdapter changing = null;
        changing = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) {
            changing.BeginSession("city-a"); return Sample();
        });
        changing.BeginSession("city-a");
        Check(Read(changing).Status == PreviewStatus.Stale,"same-city reopen during read invalidates old generation");
        CrossingReadAdapter unloading = null;
        unloading = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) {
            unloading.Invalidate(); return Sample();
        });
        unloading.BeginSession("city-a");
        Check(Read(unloading).Status == PreviewStatus.Stale,"unload during read refuses matching old data");
        Check(Read(unloading).Status == PreviewStatus.Unavailable,"unload requires explicit new session");
        CrossingReadAdapter throwing = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) {
            throw new InvalidOperationException("synthetic source failure");
        });
        throwing.BeginSession("city-a");
        Check(Read(throwing).Status == PreviewStatus.Unavailable && Read(throwing).Source == ObservationSource.Unknown,
            "reader failure stays unknown without leaking exception contents");
        CrossingReadAdapter empty = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) { return null; });
        empty.BeginSession("city-a");
        Check(Read(empty).Status == PreviewStatus.Unavailable,"null reader result stays unavailable");
        adapter.BeginSession(" ");
        Check(Read(adapter).Status == PreviewStatus.Unavailable,"invalid session does not keep previous city alive");
        using (ManualResetEvent entered = new ManualResetEvent(false))
        using (ManualResetEvent release = new ManualResetEvent(false)) {
            CrossingReadAdapter concurrent = new CrossingReadAdapter(delegate(string city, int segment, int node, bool start) {
                entered.Set();
                if (!release.WaitOne(5000, false)) throw new InvalidOperationException("test synchronization timeout");
                return Sample();
            });
            concurrent.BeginSession("city-a");
            CrossingPreview concurrentResult = null;
            Thread worker = new Thread(delegate() { concurrentResult = Read(concurrent); });
            worker.IsBackground = true;
            worker.Start();
            bool readerEntered = entered.WaitOne(5000, false);
            concurrent.Invalidate();
            release.Set();
            bool finished = worker.Join(5000);
            Check(readerEntered && finished && concurrentResult != null && concurrentResult.Status == PreviewStatus.Stale,
                "concurrent invalidation while reader is blocked refuses the result");
        }
        return checks;
    }
}
