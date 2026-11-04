using System;
using System.Threading;
using AutomatedPublicTransportPlanning.Planning;

// Signature: OpenAI-Codex_aptp_release_boundary.
public static class PlanningPreviewAdmissionChecks
{
    private static int s_assertions;

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new Exception(message); }
        s_assertions++;
    }

    public static int Run()
    {
        PlanningPreviewAdmission admission = new PlanningPreviewAdmission();
        RouteEditRequestGate owners = new RouteEditRequestGate();
        RouteEditRequestTicket first = null;
        ManualResetEvent preparing = new ManualResetEvent(false);
        ManualResetEvent finishPreparing = new ManualResetEvent(false);
        ManualResetEvent consuming = new ManualResetEvent(false);
        bool accepted = false;
        bool taken = false;
        Thread producer = new Thread(delegate()
        {
            accepted = admission.TryQueue(delegate
            {
                if (!owners.TryBegin(out first)) { return false; }
                preparing.Set();
                return finishPreparing.WaitOne(5000);
            });
        });
        Thread consumer = new Thread(delegate()
        {
            consuming.Set();
            taken = admission.TryTake();
        });
        producer.Start();
        Require(preparing.WaitOne(5000), "producer reached ticket preparation");
        consumer.Start();
        Require(consuming.WaitOne(5000), "consumer started during preparation");
        Require(!consumer.Join(100), "consumer cannot take a request before its ticket is ready");
        finishPreparing.Set();
        Require(producer.Join(5000) && consumer.Join(5000), "controlled interleaving finishes");
        Require(accepted && taken && owners.IsOwner(first), "consumer receives a fully prepared owner");
        Require(!admission.TryQueue(delegate { return true; }), "another request is refused while capture is active");

        admission.Cancel(delegate { owners.Reset(delegate { }); });
        Require(!admission.TryQueue(delegate { return true; }), "request refused until cancellation is consumed");
        Require(admission.TryResetCancelled(delegate { }), "simulation thread consumes cancellation");
        RouteEditRequestTicket second = null;
        Require(admission.TryQueue(delegate { return owners.TryBegin(out second); }), "new session starts");
        Require(admission.TryTake(), "new session request is available");
        string display = "new session";
        Require(!owners.TryPublish(first, delegate { display = "obsolete preview"; }), "old preview cannot publish a result");
        Require(!owners.TryPublish(first, delegate { display = "obsolete error"; }), "old worker cannot publish an error");
        Require(display == "new session", "stale callbacks leave the new session display unchanged");
        Require(!owners.TryPublish(first, delegate { admission.Complete(); }), "old worker cannot release new session");
        Require(!admission.TryQueue(delegate { return true; }), "new session remains busy");
        Require(owners.TryPublish(second, delegate { admission.Complete(); }), "current worker releases its request");
        Require(owners.Complete(second), "current ticket completes");
        Require(admission.TryQueue(delegate { return true; }), "another request can start after completion");

        PlanningPreviewAdmission failure = new PlanningPreviewAdmission();
        Require(!failure.TryQueue(delegate { return false; }), "failed preparation is refused");
        Require(!failure.TryTake(), "failed preparation never publishes a request");
        Require(failure.TryQueue(delegate { return true; }), "failed preparation permits retry");
        failure.Cancel(delegate { });
        Require(!failure.TryTake(), "cancelled pending request cannot be consumed");
        Require(failure.TryResetCancelled(delegate { }), "pending cancellation is reset");
        Require(failure.TryQueue(delegate { return true; }), "retry works after cancellation");
        return s_assertions;
    }
}
