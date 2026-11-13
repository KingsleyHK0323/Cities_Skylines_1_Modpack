// OpenAI-Codex_Full_Map_Unlock_And_Expansion
namespace FullMapUnlockAndExpansion {
 // Closed enumerations: no free text, paths, identifiers, coordinates, grid or balances.
 public enum DiagnosticSignal {
  LoadingCreated, LoadNewGame, LoadOther, Unloading, ProviderBound, ProviderReleased,
  SchedulerQueued, SchedulerEntered, ObservationUnavailable, ObservationComplete, Paused, OwnerAmbiguous
 }
 public enum DiagnosticOutcome { Seen, Pass, Fail, Unknown }
 public sealed class DiagnosticRecord {
  public readonly int Sequence;
  public readonly DiagnosticSignal Signal;
  public readonly DiagnosticOutcome Outcome;
  internal DiagnosticRecord(int sequence, DiagnosticSignal signal, DiagnosticOutcome outcome) {
   Sequence = sequence; Signal = signal; Outcome = outcome;
  }
 }
 // In-memory prototype only. Read returns a copy; there is no file or network export.
 // It is not connected to game callbacks and its event sequence is not a privacy guarantee.
 public sealed class RedactedDiagnosticBuffer {
  private const int Capacity = 64;
  private readonly object gate = new object();
  private readonly bool enabled;
  private readonly DiagnosticRecord[] records = new DiagnosticRecord[Capacity];
  private int count;
  private bool overflowed;
  public RedactedDiagnosticBuffer() { enabled = false; }
  // Used only by synthetic tests compiled into the same test assembly.
  internal RedactedDiagnosticBuffer(bool enabledForSyntheticTests) { enabled = enabledForSyntheticTests; }
  public bool Enabled { get { return enabled; } }
  public bool Overflowed { get { lock (gate) { return overflowed; } } }
  public bool Record(DiagnosticSignal signal, DiagnosticOutcome outcome) {
   if (!enabled || !System.Enum.IsDefined(typeof(DiagnosticSignal), signal) ||
       !System.Enum.IsDefined(typeof(DiagnosticOutcome), outcome)) return false;
   lock (gate) {
    if (count == Capacity) { overflowed = true; return false; }
    records[count] = new DiagnosticRecord(count + 1, signal, outcome); count++;
    return true;
   }
  }
  public DiagnosticRecord[] Read() {
   lock (gate) {
    DiagnosticRecord[] copy = new DiagnosticRecord[count];
    System.Array.Copy(records, copy, count);
    return copy;
   }
  }
 }
}
