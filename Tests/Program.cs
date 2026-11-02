// Signature: OpenAI-Codex
// Observation batch: OpenAI-Codex_Realistic_Border_Control
using System;
using System.Collections.Generic;
using RealisticBorderControl;
class Program
{
    static int count;
    static void Check(bool ok) { if (!ok) throw new Exception("Check failed: " + (count + 1)); count++; }
    static void Reject(Action action) { try { action(); } catch (ArgumentException) { count++; return; } throw new Exception("Expected rejection"); }
    static void Main()
    {
        var w = new Dictionary<string, decimal> { { "b", 1 }, { "a", 1 }, { "c", 1 } };
        var q = PassengerQuotaPreview.Allocate(2, w);
        Check(q["a"] == 1 && q["b"] == 1 && q["c"] == 0);
        Check(w["a"] == 1);
        Check(PassengerQuotaPreview.Allocate(0, w)["a"] == 0);
        Check(PassengerQuotaPreview.Allocate(10, new Dictionary<string, decimal>()).Count == 0);
        Check(PassengerQuotaPreview.Allocate(10, new Dictionary<string, decimal> { { "a", 0 } }).Count == 0);
        Reject(() => PassengerQuotaPreview.Allocate(-1, w));
        Reject(() => PassengerQuotaPreview.Allocate(1, null));
        Reject(() => PassengerQuotaPreview.Allocate(1, new Dictionary<string, decimal> { { "a", -1 } }));
        Reject(() => PassengerQuotaPreview.Allocate(1, new Dictionary<string, decimal> { { "", 1 } }));
        var large = PassengerQuotaPreview.Allocate(int.MaxValue, new Dictionary<string, decimal> { { "a", decimal.MaxValue }, { "b", decimal.MaxValue } });
        Check((long)large["a"] + large["b"] == int.MaxValue);
        Check(large["a"] == large["b"] + 1);
        var small = PassengerQuotaPreview.Allocate(7, new Dictionary<string, decimal> { { "a", 0.0000000000000000000000000001m }, { "b", 0.0000000000000000000000000002m } });
        Check(small["a"] == 2 && small["b"] == 5);
        for (int budget = 0; budget < 100; budget++) {
            var result = PassengerQuotaPreview.Allocate(budget, new Dictionary<string, decimal> { { "a", 2 }, { "b", 3 }, { "c", 5 } });
            Check(result["a"] + result["b"] + result["c"] == budget);
        }
        Check(PassengerQuotaPreview.ObservedPassengers == null);
        Check(PassengerQuotaPreview.DataSource.Contains("Unknown"));
        ObservationChecks.Run(Check, Reject);
        Console.WriteLine(count + " model checks passed. Synthetic inputs only; no game integration.");
    }
}
