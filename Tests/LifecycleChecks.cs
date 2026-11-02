// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using RealisticVehicleTrafficControl.Model;

internal static class LifecycleChecks
{
    private static void Expect(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    public static int RunAll()
    {
        var checks = new (string, Action)[] {
            ("LoadStartsSessionWithoutCapture", () => {
                var lifecycle=new ModLifecycle(); int calls=0;
                Expect(lifecycle.Load(()=>calls++),"Load refused");
                Expect(lifecycle.IsLoaded && lifecycle.SessionEpoch>0 && calls==1,"Session not initialized once");
            }),
            ("DuplicateLoadDoesNotReenter", () => {
                var lifecycle=new ModLifecycle(); int calls=0;
                lifecycle.Load(()=>calls++); long epoch=lifecycle.SessionEpoch; lifecycle.Load(()=>calls++);
                Expect(calls==1 && lifecycle.SessionEpoch==epoch,"Duplicate load initialized twice");
            }),
            ("FailedLoadIsIsolatedUntilUnload", () => {
                var lifecycle=new ModLifecycle(); int retries=0;
                Expect(!lifecycle.Load(()=>throw new InvalidOperationException("private fixture detail")),"Failure escaped as success");
                Expect(!lifecycle.IsLoaded && lifecycle.LastFailure=="Load:InvalidOperationException","Unsafe or missing failure state");
                Expect(!lifecycle.Load(()=>retries++) && retries==0,"Failed session retried without cleanup");
                int cleanup=0; Expect(lifecycle.Unload(()=>cleanup++) && cleanup==1,"Failed load was not cleaned up");
                Expect(lifecycle.Load(null),"Clean session could not load again");
            }),
            ("UnloadClearsStateBeforeCallback", () => {
                var lifecycle=new ModLifecycle(); lifecycle.Load(null); long previous=lifecycle.SessionEpoch;
                lifecycle.Unload(()=>Expect(!lifecycle.IsLoaded && lifecycle.SessionEpoch>previous,"State not invalidated before cleanup"));
                Expect(lifecycle.LastFailure==null,"Cleanup assertion was isolated as an unnoticed error");
            }),
            ("UnloadFailureIsIsolated", () => {
                var lifecycle=new ModLifecycle(); lifecycle.Load(null);
                Expect(!lifecycle.Unload(()=>throw new InvalidOperationException("private fixture detail")),"Failure reported success");
                Expect(!lifecycle.IsLoaded && lifecycle.LastFailure=="Unload:InvalidOperationException","Failed cleanup left active state or private text");
            }),
            ("DuplicateUnloadDoesNotReenter", () => {
                var lifecycle=new ModLifecycle(); int calls=0; lifecycle.Load(null);
                lifecycle.Unload(()=>calls++); lifecycle.Unload(()=>calls++);
                Expect(calls==1,"Cleanup called twice");
            }),
            ("ReloadHasNewEpoch", () => {
                var lifecycle=new ModLifecycle(); lifecycle.Load(null); long first=lifecycle.SessionEpoch;
                lifecycle.Unload(null); lifecycle.Load(null);
                Expect(lifecycle.IsLoaded && lifecycle.SessionEpoch>first,"Old city epoch reused");
            }),
            ("UnloadInvalidatesPendingPreview", () => {
                var lifecycle=new ModLifecycle(); lifecycle.Load(null);
                var token=lifecycle.BeginPreview(7,1); long oldEpoch=lifecycle.SessionEpoch;
                lifecycle.Unload(null); lifecycle.Load(null);
                var stale=new JunctionSnapshot(oldEpoch,7,1,false,3,true,true,DataOrigin.SyntheticFixture,
                    Array.Empty<LaneObservation>(),Array.Empty<LaneObservation>());
                Expect(!lifecycle.AcceptPreview(token,stale),"Old session callback accepted");
            }),
            ("UnloadedSessionCannotBeginPreview", () => {
                var lifecycle=new ModLifecycle(); Expect(lifecycle.BeginPreview(7,1)==null,"Unloaded session issued request");
            })
        };
        int failed=0;
        foreach(var (name,run) in checks) {
            try { run(); Console.WriteLine("PASS "+name); }
            catch(Exception error) { failed++; Console.WriteLine("FAIL "+name+": "+error.Message); }
        }
        Console.WriteLine($"LIFECYCLE RESULT {checks.Length-failed}/{checks.Length} passed; {failed} failed. Memory-only checks; no game loaded.");
        return failed;
    }
}
