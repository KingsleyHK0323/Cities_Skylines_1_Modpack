// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic contracts only. No live game, native submission or diagnostic export.
using System;
using System.Reflection;
using System.Threading;
using ICities;
using FullMapUnlockAndExpansion;

public static class NativeSubmissionChecks {
 static int checks;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
 static object Request(Type type, long? generation, long? revision, long? attempt,
     int? x, int? z, bool? eligibility, bool? previousKnown) {
  return Activator.CreateInstance(type, new object[] { generation, revision, attempt, x, z, eligibility, previousKnown });
 }
 static string Submit(MethodInfo method, object submitter, StartupSnapshot snapshot, object request) {
  return method.Invoke(submitter, new object[] { snapshot, request }).ToString();
 }
 static void CheckDisabledSubmission(Assembly assembly) {
  Type contract = assembly.GetType("FullMapUnlockAndExpansion.INativeStartupSubmission");
  Type implementation = assembly.GetType("FullMapUnlockAndExpansion.DisabledNativeStartupSubmission");
  Type requestType = assembly.GetType("FullMapUnlockAndExpansion.NativeSubmissionRequest");
  if (contract == null || implementation == null || requestType == null) throw new Exception("disabled native submission interface is missing");
  Check(contract.IsAssignableFrom(implementation), "disabled implementation satisfies the candidate interface");
  object submitter = Activator.CreateInstance(implementation);
  MethodInfo submit = implementation.GetMethod("Submit");
  if (submit == null) throw new Exception("disabled submission entry is missing");
  ParameterInfo[] parameters = submit.GetParameters();
  Check(parameters.Length == 2 && parameters[0].ParameterType == typeof(StartupSnapshot) && parameters[1].ParameterType == requestType,
      "disabled candidate accepts data only, not game provider or scheduler ports");
  Check(requestType.IsSealed && typeof(StartupSnapshot).IsSealed, "input data types cannot inject a callback through subclassing");
  Check(implementation.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0,
      "disabled implementation retains no state, delegate, provider or scheduler");
  bool[] grid = new bool[25]; grid[12] = true;
  var snapshot = new StartupSnapshot(6, 4, 1, LoadMode.NewGame, grid);
  object[] requests = new object[] {
   null,
   Request(requestType, 6, 4, 1, 2, 1, true, true),
   Request(requestType, 6, 4, 1, 2, 1, true, true),
   Request(requestType, null, 4, 1, 2, 1, true, true),
   Request(requestType, 7, 4, 1, 2, 1, true, true),
   Request(requestType, 6, null, 1, 2, 1, true, true),
   Request(requestType, 6, 3, 1, 2, 1, true, true),
   Request(requestType, 6, 4, null, 2, 1, true, true),
   Request(requestType, 6, 4, 0, 2, 1, true, true),
   Request(requestType, 6, 4, 1, 5, 0, true, true),
   Request(requestType, 6, 4, 1, -1, 1, true, true),
   Request(requestType, 6, 4, 1, 2, 1, null, true),
   Request(requestType, 6, 4, 1, 2, 1, false, true),
   Request(requestType, 6, 4, 1, 2, 1, true, null),
   Request(requestType, 6, 4, 1, 2, 1, true, false),
   Request(requestType, 6, 4, 1, 0, 5, true, true),
   Request(requestType, 6, 4, 1, null, 1, true, true),
   Request(requestType, 0, 4, 1, 2, 1, true, true)
  };
  for (int i = 0; i < requests.Length; i++) {
   object request = requests[i]; StartupSnapshot supplied = i == 1 ? null : snapshot;
   Check(Submit(submit, submitter, supplied, request) == "Unavailable", "untrusted or unknown request is unavailable");
   if (request != null) Check(!(bool)requestType.GetField("RequireMoney").GetValue(request), "request can only describe no-debit intent");
  }
  foreach (FieldInfo field in requestType.GetFields(BindingFlags.Public | BindingFlags.Instance))
   Check(field.FieldType != typeof(StartupSnapshot) && field.FieldType != typeof(bool[]), "request envelope does not retain snapshot or grid");
  Check(Submit(submit, submitter, null, null) == "Unavailable", "unknown provider and scheduler cannot be supplied or used");
  var loadedGame = new StartupSnapshot(6, 4, 1, LoadMode.LoadGame, grid);
  var scenario = new StartupSnapshot(6, 4, 1, LoadMode.NewGameFromScenario, grid);
  Check(Submit(submit, submitter, loadedGame, requests[2]) == "Unavailable" &&
      Submit(submit, submitter, scenario, requests[2]) == "Unavailable", "non-new-game modes cannot submit");
  Initial25Tile[] tiles = new Initial25Tile[25];
  for (int i = 0; i < 25; i++) tiles[i] = new Initial25Tile(i % 5, i / 5, i, grid[i]);
  var firstRun = new Initial25NoDebitRun(); var secondRun = new Initial25NoDebitRun();
  Initial25NoDebitDecision first = firstRun.Propose(tiles, 5, 5, 1, 6, 6, 4, 4, true);
  Initial25NoDebitDecision second = secondRun.Propose(tiles, 5, 5, 1, 6, 6, 4, 4, true);
  Check(first.Kind == Initial25DecisionKind.Candidate && second.Kind == Initial25DecisionKind.Candidate && first.AttemptId == second.AttemptId,
      "separate synthetic runs may reuse an attempt number");
  object firstRequest = Request(requestType, 6, 4, first.AttemptId, first.NativeIndex.Value % 5, first.NativeIndex.Value / 5, true, true);
  object secondRequest = Request(requestType, 6, 4, second.AttemptId, second.NativeIndex.Value % 5, second.NativeIndex.Value / 5, true, true);
  Check(Submit(submit, submitter, snapshot, firstRequest) == "Unavailable" && Submit(submit, submitter, snapshot, secondRequest) == "Unavailable",
      "duplicate attempt numbers across runs never enable the native entry");

  string[] results = new string[16]; Exception[] errors = new Exception[16]; var threads = new Thread[16]; var start = new ManualResetEvent(false);
  for (int i = 0; i < threads.Length; i++) {
   int slot = i;
   threads[i] = new Thread(delegate() { try { start.WaitOne(); results[slot] = Submit(submit, submitter, snapshot, requests[2]); } catch (Exception ex) { errors[slot] = ex; } });
   threads[i].Start();
  }
  start.Set(); foreach (Thread thread in threads) thread.Join(); start.Close();
  for (int i = 0; i < results.Length; i++) Check(errors[i] == null && results[i] == "Unavailable", "concurrent or wrong-thread submission is unavailable");
  Check(submit.GetParameters().Length == 2, "concurrent rejected requests have no game ports to invoke");
 }
 static void CheckDiagnosticPrototype(Assembly assembly) {
  Type bufferType = assembly.GetType("FullMapUnlockAndExpansion.RedactedDiagnosticBuffer");
  Type signalType = assembly.GetType("FullMapUnlockAndExpansion.DiagnosticSignal");
  Type outcomeType = assembly.GetType("FullMapUnlockAndExpansion.DiagnosticOutcome");
  Type recordType = assembly.GetType("FullMapUnlockAndExpansion.DiagnosticRecord");
  if (bufferType == null || signalType == null || outcomeType == null || recordType == null) throw new Exception("disabled redacted diagnostic prototype is missing");
  object disabled = Activator.CreateInstance(bufferType);
  MethodInfo record = bufferType.GetMethod("Record"), read = bufferType.GetMethod("Read");
  object signal = Enum.Parse(signalType, "LoadingCreated"), outcome = Enum.Parse(outcomeType, "Seen");
  Check(!(bool)bufferType.GetProperty("Enabled").GetValue(disabled, null) && !bufferType.GetProperty("Enabled").CanWrite,
      "diagnostics default to disabled without a public enable setter");
  Check(!(bool)record.Invoke(disabled, new object[] { signal, outcome }) && ((Array)read.Invoke(disabled, null)).Length == 0,
      "disabled diagnostics record nothing");
  Check(bufferType.GetConstructor(new Type[] { typeof(bool) }) == null, "synthetic enable constructor is not public");

  object enabled = Activator.CreateInstance(bufferType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { true }, null);
  Check((bool)bufferType.GetProperty("Enabled").GetValue(enabled, null), "test-only in-memory buffer can be exercised");
  Check(!(bool)record.Invoke(enabled, new object[] { Enum.ToObject(signalType, 999), outcome }) &&
      !(bool)record.Invoke(enabled, new object[] { signal, Enum.ToObject(outcomeType, 999) }), "undefined signal or outcome is rejected");
  Check((bool)record.Invoke(enabled, new object[] { signal, outcome }), "allowlisted synthetic event is accepted");
  Array first = (Array)read.Invoke(enabled, null); first.SetValue(null, 0);
  Check(((Array)read.Invoke(enabled, null)).GetValue(0) != null, "diagnostic read returns a detached array");
  FieldInfo[] fields = recordType.GetFields(BindingFlags.Public | BindingFlags.Instance);
  Check(fields.Length == 3, "diagnostic event contains only sequence and allowlisted enums");
  foreach (FieldInfo field in fields) Check(field.IsInitOnly && (field.FieldType == typeof(int) || field.FieldType == signalType || field.FieldType == outcomeType),
      "diagnostic event exposes no free-text, coordinates, grid, identifiers or balance fields");

  var threads = new Thread[4]; var start = new ManualResetEvent(false); Exception[] errors = new Exception[4];
  for (int i = 0; i < threads.Length; i++) {
   int slot = i;
   threads[i] = new Thread(delegate() { try { start.WaitOne(); for (int j = 0; j < 20; j++) record.Invoke(enabled, new object[] { signal, outcome }); } catch (Exception ex) { errors[slot] = ex; } });
   threads[i].Start();
  }
  start.Set(); foreach (Thread thread in threads) thread.Join(); start.Close();
  foreach (Exception error in errors) Check(error == null, "concurrent synthetic diagnostic event does not throw");
  Array bounded = (Array)read.Invoke(enabled, null);
  Check(bounded.Length == 64 && (bool)bufferType.GetProperty("Overflowed").GetValue(enabled, null), "diagnostics are bounded and report truncation");
  FieldInfo sequence = recordType.GetField("Sequence");
  for (int i = 0; i < bounded.Length; i++) Check((int)sequence.GetValue(bounded.GetValue(i)) == i + 1, "diagnostic sequence is monotonic");
 }
 public static void Run() {
  Assembly assembly = typeof(Observation).Assembly;
  CheckDisabledSubmission(assembly); CheckDiagnosticPrototype(assembly);
  Console.WriteLine("Passed " + checks + " disabled submission and redacted diagnostic checks; no game calls or output files tested.");
 }
}
