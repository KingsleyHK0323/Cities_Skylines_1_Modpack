// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic in-memory checks only; no game callbacks or file export.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ICities;

class LifecycleTraceCandidateChecks {
 static int checks;
 static Type traceType, componentType, recordType;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
 static object Signal(string name) { return Enum.Parse(componentType, name); }
 static object Call(object trace, string method, params object[] values) { return traceType.GetMethod(method).Invoke(trace, values); }
 static object EnabledTrace() { return Activator.CreateInstance(traceType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { true }, null); }
 static Array Read(object trace) { return (Array)Call(trace, "Read"); }
 static int Number(object record, string name) { return (int)recordType.GetField(name).GetValue(record); }
 static string Kind(object record) { return recordType.GetField("Kind").GetValue(record).ToString(); }
 static string Mode(object record) { return recordType.GetField("Mode").GetValue(record).ToString(); }
 static void DefaultDisabled() {
  object trace = Activator.CreateInstance(traceType);
  Check(!(bool)traceType.GetProperty("Enabled").GetValue(trace, null), "trace defaults to disabled");
  Check(traceType.GetConstructor(new Type[] { typeof(bool) }) == null, "synthetic activation constructor is not public");
  Check(Call(trace, "Register", Signal("Loading")) == null, "disabled trace cannot register an instance");
  Check(Read(trace).Length == 0 && ((byte[])Call(trace, "ExportUtf8")).Length == 0, "disabled trace has no in-process output");
 }
 static void OrderingAndIsolation() {
  object trace = EnabledTrace();
  object loading = Call(trace, "Register", Signal("Loading"));
  object areas = Call(trace, "Register", Signal("Areas"));
  object thread = Call(trace, "Register", Signal("Threading"));
  object generation = Call(trace, "Loaded", loading, LoadMode.NewGame);
  Check(generation != null && (bool)Call(trace, "BindProvider", areas, generation), "synthetic provider bound to observed generation");
  object queue = Call(trace, "Queue", thread, generation);
  Check(queue != null, "synthetic queue receives a local token");
  Check((bool)Call(trace, "Enter", queue) && (bool)Call(trace, "Enter", queue), "nested entry is recorded as observation only");
  Check((bool)Call(trace, "Exit", queue) && (bool)Call(trace, "Exit", queue), "nested callbacks can exit");
  Check((bool)Call(trace, "Unload", loading), "unload is recorded");
  Check((bool)Call(trace, "Enter", queue) && (bool)Call(trace, "Exit", queue), "late callback is recorded without authorization");
  Check((bool)Call(trace, "Release", loading), "release is recorded");
  Check(Call(trace, "Loaded", loading, LoadMode.NewGame) == null, "released loading cannot begin a generation");
  Check((bool)Call(trace, "Release", areas) && (bool)Call(trace, "Release", thread), "provider and thread releases recorded");
  Array records = Read(trace);
  string[] expected = new string[] { "Created", "Created", "Created", "Loaded", "ProviderBound", "QueueRequested",
      "CallbackEntered", "OverlapOrReentry", "CallbackExited", "CallbackExited", "Unloading", "CallbackAfterUnload",
      "CallbackExited", "Released", "LateCallback", "Released", "Released" };
  Check(records.Length == expected.Length, "expected number of bounded events");
  for (int i = 0; i < expected.Length; i++)
   Check(Number(records.GetValue(i), "Sequence") == i + 1 && Kind(records.GetValue(i)) == expected[i], "event ordering and type");
  Check(Number(records.GetValue(0), "Instance") == 1 && Number(records.GetValue(1), "Instance") == 2 &&
      Number(records.GetValue(2), "Instance") == 3, "instance numbers are generated locally");
  Check(Number(records.GetValue(3), "Generation") == 1 && Mode(records.GetValue(3)) == "NewGame" &&
      Number(records.GetValue(5), "Queue") == 1, "generation, mode and queue identifiers are local");
  records.SetValue(null, 0); Check(Read(trace).GetValue(0) != null, "read returns detached array");

  byte[] exported = (byte[])Call(trace, "ExportUtf8"); string text = Encoding.UTF8.GetString(exported);
  string[] lines = text.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
  Check(lines.Length == expected.Length, "in-memory JSONL contains only recorded events");
  Regex shape = new Regex("^\\{\\\"seq\\\":[0-9]+,\\\"event\\\":[0-9]+,\\\"instance\\\":[0-9]+,\\\"generation\\\":[0-9]+,\\\"queue\\\":[0-9]+,\\\"mode\\\":[0-9]+\\}$");
  foreach (string line in lines) Check(shape.IsMatch(line), "JSONL has only fixed keys and numeric codes");
  Check(!text.Contains("SECRET_USER_CITY_X") && !text.Contains("sample/OpenAI-Codex_build_review.txt") && !text.Contains("Steam"), "no raw sample identifiers are exported");
  Check(exported.Length > 0 && exported[0] == (byte)'{' && !text.Contains("\r") && text == Encoding.UTF8.GetString((byte[])Call(trace, "ExportUtf8")),
      "in-process export is deterministic UTF-8 without BOM and uses LF");
  Call(trace, "Stop"); int stoppedCount = Read(trace).Length;
  Check(!(bool)traceType.GetProperty("Enabled").GetValue(trace, null) && Call(trace, "Register", Signal("Loading")) == null && Read(trace).Length == stoppedCount,
      "stopping prevents subsequent records");
  Check(Encoding.UTF8.GetString((byte[])Call(trace, "ExportUtf8")) == text && exported.Length > 0,
      "stopping collection still permits a new in-process export of previously recorded events");

  object first = EnabledTrace(), other = EnabledTrace();
  object firstLoading = Call(first, "Register", Signal("Loading"));
  object firstAreas = Call(first, "Register", Signal("Areas"));
  object firstThread = Call(first, "Register", Signal("Threading"));
  object firstGeneration = Call(first, "Loaded", firstLoading, LoadMode.NewGame);
  object otherLoading = Call(other, "Register", Signal("Loading"));
  object otherAreas = Call(other, "Register", Signal("Areas"));
  object otherThread = Call(other, "Register", Signal("Threading"));
  object otherGeneration = Call(other, "Loaded", otherLoading, LoadMode.NewGame);
  object otherQueue = Call(other, "Queue", otherThread, otherGeneration);
  int firstCount = Read(first).Length;
  Check(Call(first, "Loaded", otherLoading, LoadMode.NewGame) == null && Read(first).Length == firstCount,
      "enabled trace rejects another trace's loading token without recording");
  Check(!(bool)Call(first, "BindProvider", firstAreas, otherGeneration) &&
      !(bool)Call(first, "BindProvider", otherAreas, firstGeneration) && Read(first).Length == firstCount,
      "enabled trace rejects foreign generation and provider tokens without recording");
  Check(Call(first, "Queue", firstThread, otherGeneration) == null &&
      Call(first, "Queue", otherThread, firstGeneration) == null && Read(first).Length == firstCount,
      "enabled trace rejects foreign generation and threading tokens without recording");
  Check(!(bool)Call(first, "Enter", otherQueue) && !(bool)Call(first, "Exit", otherQueue) && Read(first).Length == firstCount,
      "enabled trace rejects another trace's queue token without recording");
  Check(Call(first, "Register", Enum.ToObject(componentType, 999)) == null && Read(first).Length == firstCount,
      "enabled trace rejects an unknown component without recording");
  object modeTrace = EnabledTrace(); object a = Call(modeTrace, "Register", Signal("Loading"));
  Call(modeTrace, "Loaded", a, LoadMode.LoadGame); Call(modeTrace, "Unload", a);
  Call(modeTrace, "Loaded", a, LoadMode.NewGameFromScenario); Call(modeTrace, "Unload", a);
  Call(modeTrace, "Loaded", a, (LoadMode)999);
  Array modeRecords = Read(modeTrace);
  Check(Mode(modeRecords.GetValue(1)) == "LoadGame" && Mode(modeRecords.GetValue(3)) == "Scenario" && Mode(modeRecords.GetValue(5)) == "Other",
      "load modes are reduced to a fixed allowlist");
  object queueTrace = EnabledTrace(); object queueLoading = Call(queueTrace, "Register", Signal("Loading"));
  object queueThread = Call(queueTrace, "Register", Signal("Threading"));
  object queueGeneration = Call(queueTrace, "Loaded", queueLoading, LoadMode.NewGame);
  object firstQueue = Call(queueTrace, "Queue", queueThread, queueGeneration), secondQueue = Call(queueTrace, "Queue", queueThread, queueGeneration);
  Check((bool)Call(queueTrace, "Enter", firstQueue) && !(bool)Call(queueTrace, "Exit", secondQueue),
      "an unentered queue cannot consume a different queue's callback exit");
  Check((bool)Call(queueTrace, "Exit", firstQueue), "the entered queue retains its matching exit");
  int repeatedAt = Read(queueTrace).Length;
  Check((bool)Call(queueTrace, "Enter", firstQueue) && (bool)Call(queueTrace, "Exit", firstQueue),
      "a fully exited queue may be entered again for observation");
  Array repeatedRecords = Read(queueTrace);
  Check(Kind(repeatedRecords.GetValue(repeatedAt)) == "CallbackEntered" &&
      Number(repeatedRecords.GetValue(repeatedAt), "Queue") == Number(repeatedRecords.GetValue(repeatedAt - 1), "Queue"),
      "a repeat entry has an ordinary event code and must be identified by its queue label");
  Check((bool)Call(queueTrace, "Enter", firstQueue) && (bool)Call(queueTrace, "Enter", secondQueue) &&
      (bool)Call(queueTrace, "Exit", secondQueue) && !(bool)Call(queueTrace, "Exit", secondQueue) &&
      (bool)Call(queueTrace, "Exit", firstQueue),
      "callback pairing relies on callers using the corresponding queue token");
  FieldInfo[] fields = recordType.GetFields(BindingFlags.Public | BindingFlags.Instance);
  Check(fields.Length == 6, "record contains only fixed numeric fields and enums");
  foreach (FieldInfo field in fields) Check(field.IsInitOnly && (field.FieldType == typeof(int) || field.FieldType.IsEnum),
      "record fields are read-only numeric or enum values, with no dedicated free-text field");
  foreach (MethodInfo method in traceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
   foreach (ParameterInfo parameter in method.GetParameters()) Check(parameter.ParameterType != typeof(string) && parameter.ParameterType != typeof(byte[]) && parameter.ParameterType != typeof(object),
       "trace API does not accept arbitrary text, byte payloads or object identities");
 }
 static void BoundedConcurrentEvents() {
  object trace = EnabledTrace(); object loading = Call(trace, "Register", Signal("Loading"));
  object thread = Call(trace, "Register", Signal("Threading")); object generation = Call(trace, "Loaded", loading, LoadMode.NewGame);
  var start = new ManualResetEvent(false); var threads = new Thread[4]; var errors = new Exception[4];
  for (int i = 0; i < threads.Length; i++) {
   int slot = i;
   threads[i] = new Thread(delegate() { try { start.WaitOne(); for (int j = 0; j < 24; j++) Call(trace, "Queue", thread, generation); } catch (Exception ex) { errors[slot] = ex; } });
   threads[i].Start();
  }
  start.Set(); foreach (Thread threadItem in threads) threadItem.Join(); start.Close();
  foreach (Exception error in errors) Check(error == null, "concurrent synthetic event does not throw");
  Array records = Read(trace);
  Check(records.Length == 64 && (bool)traceType.GetProperty("Overflowed").GetValue(trace, null), "trace is bounded per instance and reports truncation");
  for (int i = 0; i < records.Length; i++) Check(Number(records.GetValue(i), "Sequence") == i + 1, "concurrent sequence has no gaps");
  string exported = Encoding.UTF8.GetString((byte[])Call(trace, "ExportUtf8"));
  string[] lines = exported.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
  Check(lines.Length == 65 && lines[64].Contains("\"seq\":65") && lines[64].Contains("\"event\":12"),
      "truncation is explicit in the in-process JSONL copy");
  Call(trace, "Stop"); int afterStop = Read(trace).Length;
  Check(Call(trace, "Queue", thread, generation) == null && Read(trace).Length == afterStop, "stopped trace cannot accept queued events");
 }
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  // OpenAI-Codex_review_model
  // The scheduler below is a controlled fake. It never calls a game area API.
  private sealed class SchedulerAssertionFailure : Exception {
   internal SchedulerAssertionFailure(string message) : base(message) { }
  }
  private sealed class FakeSchedulerFailure : Exception { }
  private sealed class FakeScheduler : IThreading {
   internal readonly FakeSchedulerFailure Failure = new FakeSchedulerFailure();
   internal bool Inline, ThrowBefore, ThrowAfter;
   internal int Requests, Deliveries;
   internal Action Saved;
   public bool simulationPaused { get { return false; } }
   public void QueueSimulationThread(Action action) {
    if (action == null) throw new InvalidOperationException("missing synthetic callback");
    Requests++;
    if (ThrowBefore) throw Failure;
    Saved = action;
    if (Inline) Deliver();
    if (ThrowAfter) throw Failure;
   }
   internal void Deliver() {
    if (Saved == null) throw new InvalidOperationException("missing saved synthetic callback");
    Deliveries++;
    Saved();
   }
  }
  private sealed class SchedulerCase {
   internal readonly object Trace, Loading, Threading, Generation, Queue;
   internal readonly FakeScheduler Scheduler = new FakeScheduler();
   internal readonly CallbackProbe Callback;
   internal SchedulerCase() {
    Trace = EnabledTrace();
    Loading = Call(Trace, "Register", Signal("Loading"));
    Threading = Call(Trace, "Register", Signal("Threading"));
    Generation = Call(Trace, "Loaded", Loading, LoadMode.NewGame);
    Queue = Call(Trace, "Queue", Threading, Generation);
    SchedulerCheck(Loading != null && Threading != null && Generation != null && Queue != null,
      "synthetic trace setup failed");
    Callback = new CallbackProbe(Trace, Queue);
   }
  }
  private sealed class CallbackProbe {
   private readonly object trace, queue, gate = new object();
   internal int Calls, Gaps, Exits;
   internal readonly List<object> Invocations = new List<object>();
   internal CallbackProbe(object value, object item) { trace = value; queue = item; }
   internal void Run() {
    Interlocked.Increment(ref Calls);
    object invocation = SchedulerCall(trace, "EnterInvocation", queue);
    if (invocation == null) { Interlocked.Increment(ref Gaps); return; }
    lock (gate) { Invocations.Add(invocation); }
    if ((bool)SchedulerCall(trace, "ExitInvocation", invocation)) Interlocked.Increment(ref Exits);
   }
  }
  static int schedulerChecks;
  static void SchedulerCheck(bool value, string message) {
   Interlocked.Increment(ref schedulerChecks);
   if (!value) throw new SchedulerAssertionFailure(message);
  }
  static object SchedulerCall(object trace, string method, params object[] values) {
   MethodInfo found = traceType.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
   if (found == null) {
    if (method == "QueueThrew" || method == "QueueReturned")
     throw new SchedulerAssertionFailure("missing queue result observation");
    throw new SchedulerAssertionFailure("missing invocation or thread observation");
   }
   return found.Invoke(trace, values);
  }
  static Array SchedulerDetails(object trace) {
   return (Array)SchedulerCall(trace, "ReadObservationDetails");
  }
  static int DetailNumber(object detail, string name) {
   FieldInfo field = detail.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
   SchedulerCheck(field != null && field.FieldType == typeof(int) && field.IsInitOnly,
     "detail must expose the expected immutable numeric field");
   return (int)field.GetValue(detail);
  }
  static void SchedulerRows(object trace, string[] expectedKinds, int[] expectedQueues,
      int[] expectedInvocations, int[] expectedThreads) {
   Array records = Read(trace);
   Array details = SchedulerDetails(trace);
   SchedulerCheck(records.Length == expectedKinds.Length && details.Length == expectedKinds.Length &&
     expectedQueues.Length == expectedKinds.Length && expectedInvocations.Length == expectedKinds.Length &&
     expectedThreads.Length == expectedKinds.Length, "trace and detail event counts differ");
   for (int i = 0; i < records.Length; i++) {
    object record = records.GetValue(i), detail = details.GetValue(i);
    SchedulerCheck(Kind(record) == expectedKinds[i] && Number(record, "Sequence") == i + 1 &&
      Number(record, "Queue") == expectedQueues[i], "unexpected trace event or queue order");
    SchedulerCheck(DetailNumber(detail, "Sequence") == i + 1 &&
      DetailNumber(detail, "Invocation") == expectedInvocations[i] &&
      DetailNumber(detail, "ThreadTag") == expectedThreads[i], "unexpected local invocation or thread label");
   }
  }
  static void SchedulerFailureAndReplay() {
   var item = new SchedulerCase();
   item.Scheduler.ThrowAfter = true;
   Action callback = item.Callback.Run;
   Exception failure = null;
   try { item.Scheduler.QueueSimulationThread(callback); }
   catch (FakeSchedulerFailure exception) { failure = exception; }
   SchedulerCheck(object.ReferenceEquals(failure, item.Scheduler.Failure) && item.Scheduler.Requests == 1 &&
     object.ReferenceEquals(item.Scheduler.Saved, callback) && item.Scheduler.Deliveries == 0,
     "fake scheduler must save the callback and then throw");
   SchedulerCheck((bool)SchedulerCall(item.Trace, "QueueThrew", item.Queue), "queue exception must be recorded");
   int afterFailure = Read(item.Trace).Length;
   SchedulerCheck(!(bool)SchedulerCall(item.Trace, "QueueReturned", item.Queue) &&
     !(bool)SchedulerCall(item.Trace, "QueueThrew", item.Queue) && Read(item.Trace).Length == afterFailure,
     "a queue result is exclusive and recorded only once");
   item.Scheduler.Deliver();
   item.Scheduler.Deliver();
   SchedulerCheck(object.ReferenceEquals(item.Scheduler.Saved, callback) && item.Scheduler.Deliveries == 2 &&
     item.Callback.Calls == 2 && item.Callback.Exits == 2 && item.Callback.Invocations.Count == 2 &&
     !object.ReferenceEquals(item.Callback.Invocations[0], item.Callback.Invocations[1]),
     "the same saved callback must create two completed invocations");
   SchedulerRows(item.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "QueueThrew", "CallbackEntered", "CallbackExited", "CallbackEntered", "CallbackExited" },
     new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 1 },
     new int[] { 0, 0, 0, 0, 0, 1, 1, 2, 2 },
     new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 });
  }
  static void SchedulerBeforeStoreAndInline() {
   var before = new SchedulerCase();
   before.Scheduler.ThrowBefore = true;
   Exception failure = null;
   try { before.Scheduler.QueueSimulationThread(before.Callback.Run); }
   catch (FakeSchedulerFailure exception) { failure = exception; }
   SchedulerCheck(object.ReferenceEquals(failure, before.Scheduler.Failure) && before.Scheduler.Requests == 1 &&
     before.Scheduler.Saved == null && before.Scheduler.Deliveries == 0, "failure before storage must not invent a callback");
   SchedulerCheck((bool)SchedulerCall(before.Trace, "QueueThrew", before.Queue), "pre-storage exception must be recorded");
   SchedulerRows(before.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "QueueThrew" },
     new int[] { 0, 0, 0, 1, 1 }, new int[] { 0, 0, 0, 0, 0 }, new int[] { 1, 1, 1, 1, 1 });

   var inline = new SchedulerCase();
   inline.Scheduler.Inline = true;
   inline.Scheduler.QueueSimulationThread(inline.Callback.Run);
   SchedulerCheck(inline.Scheduler.Deliveries == 1 && inline.Callback.Calls == 1 && inline.Callback.Exits == 1,
     "inline fake callback must execute before scheduler return");
   SchedulerCheck((bool)SchedulerCall(inline.Trace, "QueueReturned", inline.Queue), "successful queue result must be recorded");
   int afterReturn = Read(inline.Trace).Length;
   SchedulerCheck(!(bool)SchedulerCall(inline.Trace, "QueueReturned", inline.Queue) &&
     !(bool)SchedulerCall(inline.Trace, "QueueThrew", inline.Queue) && Read(inline.Trace).Length == afterReturn,
     "a successful queue result cannot be duplicated or reversed");
   SchedulerRows(inline.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "CallbackEntered", "CallbackExited", "QueueReturned" },
     new int[] { 0, 0, 0, 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0, 1, 1, 0 }, new int[] { 1, 1, 1, 1, 1, 1, 1 });

   var inlineFailure = new SchedulerCase();
   inlineFailure.Scheduler.Inline = true;
   inlineFailure.Scheduler.ThrowAfter = true;
   failure = null;
   try { inlineFailure.Scheduler.QueueSimulationThread(inlineFailure.Callback.Run); }
   catch (FakeSchedulerFailure exception) { failure = exception; }
   SchedulerCheck(object.ReferenceEquals(failure, inlineFailure.Scheduler.Failure) &&
     inlineFailure.Scheduler.Deliveries == 1 && inlineFailure.Callback.Calls == 1,
     "inline callback must execute before the fake scheduler throws");
   SchedulerCheck((bool)SchedulerCall(inlineFailure.Trace, "QueueThrew", inlineFailure.Queue), "inline queue exception must be recorded");
   SchedulerRows(inlineFailure.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "CallbackEntered", "CallbackExited", "QueueThrew" },
     new int[] { 0, 0, 0, 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0, 1, 1, 0 }, new int[] { 1, 1, 1, 1, 1, 1, 1 });
  }
  static void SchedulerDelayedThreadAndUnload() {
   var delayed = new SchedulerCase();
   delayed.Scheduler.QueueSimulationThread(delayed.Callback.Run);
   SchedulerCheck((bool)SchedulerCall(delayed.Trace, "QueueReturned", delayed.Queue), "delayed queue return must be recorded");
   Exception workerError = null;
   Thread worker = new Thread(delegate() { try { delayed.Scheduler.Deliver(); } catch (Exception exception) { workerError = exception; } });
   worker.IsBackground = true; worker.Start();
   bool joined = worker.Join(5000);
   SchedulerCheck(joined && workerError == null && delayed.Scheduler.Deliveries == 1 && delayed.Callback.Calls == 1,
     "saved callback must run and finish on a second managed thread");
   SchedulerRows(delayed.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "QueueReturned", "CallbackEntered", "CallbackExited" },
     new int[] { 0, 0, 0, 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0, 0, 1, 1 }, new int[] { 1, 1, 1, 1, 1, 2, 2 });

   var unloaded = new SchedulerCase();
   unloaded.Scheduler.QueueSimulationThread(unloaded.Callback.Run);
   SchedulerCheck((bool)SchedulerCall(unloaded.Trace, "QueueReturned", unloaded.Queue), "unload scenario queue must return");
   SchedulerCheck((bool)Call(unloaded.Trace, "Unload", unloaded.Loading), "synthetic unload must be recorded");
   unloaded.Scheduler.Deliver();
   SchedulerCheck(unloaded.Callback.Calls == 1 && unloaded.Callback.Exits == 1, "callback must actually run after fake unload");
   SchedulerRows(unloaded.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "QueueReturned", "Unloading", "CallbackAfterUnload", "CallbackExited" },
     new int[] { 0, 0, 0, 1, 1, 0, 1, 1 }, new int[] { 0, 0, 0, 0, 0, 0, 1, 1 }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1 });
  }
  static void SchedulerIsolationAndPairing() {
   var first = new SchedulerCase();
   var other = new SchedulerCase();
   int before = Read(first.Trace).Length;
   SchedulerCheck(!(bool)SchedulerCall(first.Trace, "QueueReturned", other.Queue), "foreign queue cannot report return");
   SchedulerCheck(!(bool)SchedulerCall(first.Trace, "QueueThrew", other.Queue), "foreign queue cannot report an exception");
   SchedulerCheck(SchedulerCall(first.Trace, "EnterInvocation", other.Queue) == null && Read(first.Trace).Length == before,
     "foreign queue cannot enter or add records");
   object secondQueue = Call(first.Trace, "Queue", first.Threading, first.Generation);
   object firstInvocation = SchedulerCall(first.Trace, "EnterInvocation", first.Queue);
   object secondInvocation = SchedulerCall(first.Trace, "EnterInvocation", secondQueue);
   SchedulerCheck(firstInvocation != null && secondInvocation != null && !object.ReferenceEquals(firstInvocation, secondInvocation),
     "overlapping queue entries must have distinct invocation tokens");
   SchedulerCheck(!(bool)Call(first.Trace, "Exit", first.Queue), "legacy exit must not consume a new invocation");
   SchedulerCheck(!(bool)SchedulerCall(other.Trace, "ExitInvocation", firstInvocation), "foreign recorder cannot consume an invocation");
   SchedulerCheck((bool)SchedulerCall(first.Trace, "ExitInvocation", firstInvocation), "first invocation exits once");
   SchedulerCheck(!(bool)SchedulerCall(first.Trace, "ExitInvocation", firstInvocation), "an invocation cannot exit twice");
   SchedulerCheck((bool)SchedulerCall(first.Trace, "ExitInvocation", secondInvocation), "second queue retains its own invocation");
   Array firstDetails = SchedulerDetails(first.Trace), otherDetails = SchedulerDetails(other.Trace);
   SchedulerCheck(DetailNumber(firstDetails.GetValue(0), "ThreadTag") == 1 && DetailNumber(otherDetails.GetValue(0), "ThreadTag") == 1,
     "thread labels restart locally for each recorder");
  }
  static void SchedulerStopAndCapacity() {
   var stopped = new SchedulerCase();
   stopped.Scheduler.QueueSimulationThread(stopped.Callback.Run);
   SchedulerCheck((bool)SchedulerCall(stopped.Trace, "QueueReturned", stopped.Queue), "stop scenario queue must return");
   int before = Read(stopped.Trace).Length;
   Call(stopped.Trace, "Stop");
   stopped.Scheduler.Deliver();
   SchedulerCheck(stopped.Scheduler.Deliveries == 1 && stopped.Callback.Calls == 1 && stopped.Callback.Gaps == 1 &&
     Read(stopped.Trace).Length == before && SchedulerDetails(stopped.Trace).Length == before,
     "a callback can run after stop without a recorded entry");

   var bounded = new SchedulerCase();
   bounded.Scheduler.QueueSimulationThread(bounded.Callback.Run);
   for (int i = 0; i < 60; i++)
    SchedulerCheck(Call(bounded.Trace, "Queue", bounded.Threading, bounded.Generation) != null, "each in-capacity event must be recorded");
   SchedulerCheck(Read(bounded.Trace).Length == 64 && SchedulerDetails(bounded.Trace).Length == 64,
     "record and detail arrays reach the same capacity");
   SchedulerCheck(!(bool)SchedulerCall(bounded.Trace, "QueueReturned", bounded.Queue) &&
     (bool)traceType.GetProperty("Overflowed").GetValue(bounded.Trace, null) &&
     !(bool)traceType.GetProperty("Enabled").GetValue(bounded.Trace, null), "capacity must stop recording without a partial queue result");
   bounded.Scheduler.Deliver();
   SchedulerCheck(bounded.Callback.Calls == 1 && bounded.Callback.Gaps == 1 && Read(bounded.Trace).Length == 64 &&
     SchedulerDetails(bounded.Trace).Length == 64, "callback after capacity exhaustion is an observation gap");
   string exported = Encoding.UTF8.GetString((byte[])Call(bounded.Trace, "ExportUtf8"));
   string[] lines = exported.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
   SchedulerCheck(lines.Length == 65 && lines[64].Contains("\"seq\":65") && lines[64].Contains("\"event\":12"),
     "capacity exhaustion remains explicit in the old JSONL schema");
  }
  static void SchedulerThreadCapacityAndFields() {
   object trace = EnabledTrace();
   object loading = Call(trace, "Register", Signal("Loading"));
   object threading = Call(trace, "Register", Signal("Threading"));
   object generation = Call(trace, "Loaded", loading, LoadMode.NewGame);
   for (int i = 0; i < 32; i++) {
    object result = null; Exception workerError = null;
    Thread worker = new Thread(delegate() { try { result = Call(trace, "Queue", threading, generation); } catch (Exception exception) { workerError = exception; } });
    worker.IsBackground = true; worker.Start();
    bool joined = worker.Join(5000);
    SchedulerCheck(joined && workerError == null, "synthetic label worker must complete");
    if (i < 31) SchedulerCheck(result != null, "thread label below capacity must be recorded");
    else SchedulerCheck(result == null && (bool)traceType.GetProperty("Overflowed").GetValue(trace, null),
      "thread label capacity must stop without partial recording");
   }
   Array records = Read(trace), details = SchedulerDetails(trace);
   SchedulerCheck(records.Length == 34 && details.Length == 34, "thread capacity preserves matching bounded snapshots");
   for (int i = 3; i < details.Length; i++)
    SchedulerCheck(DetailNumber(details.GetValue(i), "Sequence") == i + 1 &&
      DetailNumber(details.GetValue(i), "ThreadTag") == i - 1, "managed threads receive recorder-local labels");
   Type detailType = traceType.Assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.TraceObservationDetail");
   SchedulerCheck(detailType != null, "numeric observation detail type must exist");
   FieldInfo[] fields = detailType.GetFields(BindingFlags.Public | BindingFlags.Instance);
   SchedulerCheck(fields.Length == 3, "observation details must have only three fixed fields");
   foreach (FieldInfo field in fields)
    SchedulerCheck(field.IsInitOnly && field.FieldType == typeof(int) &&
      (field.Name == "Sequence" || field.Name == "ThreadTag" || field.Name == "Invocation"),
      "observation detail fields must be immutable numeric labels");
  }
  // OpenAI-Codex_review_model
  static void SchedulerOldGenerationAfterReload() {
   var item = new SchedulerCase();
   Action oldAction = item.Callback.Run;
   item.Scheduler.QueueSimulationThread(oldAction);
   SchedulerCheck((bool)SchedulerCall(item.Trace, "QueueReturned", item.Queue), "old queue return must be recorded");
   SchedulerCheck((bool)Call(item.Trace, "Unload", item.Loading), "first fake generation must unload");
   object newerGeneration = Call(item.Trace, "Loaded", item.Loading, LoadMode.NewGame);
   SchedulerCheck(newerGeneration != null && !object.ReferenceEquals(item.Generation, newerGeneration),
     "second fake generation must have its own local token");
   item.Scheduler.Deliver();
   SchedulerCheck(object.ReferenceEquals(oldAction, item.Scheduler.Saved) && item.Scheduler.Deliveries == 1 &&
     item.Callback.Calls == 1 && item.Callback.Exits == 1, "the old saved fake callback must actually run");
   SchedulerRows(item.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "QueueReturned", "Unloading", "Loaded", "CallbackAfterUnload", "CallbackExited" },
     new int[] { 0, 0, 0, 1, 1, 0, 0, 1, 1 },
     new int[] { 0, 0, 0, 0, 0, 0, 0, 1, 1 },
     new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 });
   Array records = Read(item.Trace);
   SchedulerCheck(Number(records.GetValue(2), "Generation") == 1 &&
     Number(records.GetValue(6), "Generation") == 2 &&
     Number(records.GetValue(7), "Generation") == 1 &&
     Number(records.GetValue(8), "Generation") == 1,
     "late fake callback must retain its original generation after a new load");
  }
  // This records a controlled token transfer; it does not authenticate a real call stack.
  static void SchedulerCrossThreadTokenTransfer() {
   var item = new SchedulerCase();
   var entered = new ManualResetEvent(false);
   var release = new ManualResetEvent(false);
   object invocation = null;
   Exception enterError = null, exitError = null;
   bool enterWaitExpired = false, exitResult = false;
   Thread enterThread = new Thread(delegate() {
    try { invocation = SchedulerCall(item.Trace, "EnterInvocation", item.Queue); }
    catch (Exception exception) { enterError = exception; }
    finally { entered.Set(); if (!release.WaitOne(5000)) enterWaitExpired = true; }
   });
   Thread exitThread = new Thread(delegate() {
    try { exitResult = (bool)SchedulerCall(item.Trace, "ExitInvocation", invocation); }
    catch (Exception exception) { exitError = exception; }
   });
   enterThread.IsBackground = true; exitThread.IsBackground = true;
   bool enterReady = false, exitStarted = false, exitJoined = false, enterJoined = false;
   try {
    enterThread.Start();
    enterReady = entered.WaitOne(5000);
    if (enterReady && enterError == null && invocation != null) {
     exitThread.Start(); exitStarted = true; exitJoined = exitThread.Join(5000);
    }
   } finally {
    release.Set();
    enterJoined = enterThread.Join(5000);
   }
   SchedulerCheck(enterReady && exitStarted && exitJoined && enterJoined && !enterWaitExpired &&
     enterError == null && exitError == null && invocation != null && exitResult,
     "controlled fake invocation must enter and exit on two distinct managed threads");
   entered.Close(); release.Close();
   SchedulerRows(item.Trace,
     new string[] { "Created", "Created", "Loaded", "QueueRequested", "CallbackEntered", "CallbackExited" },
     new int[] { 0, 0, 0, 1, 1, 1 }, new int[] { 0, 0, 0, 0, 1, 1 }, new int[] { 1, 1, 1, 1, 2, 3 });
   Array details = SchedulerDetails(item.Trace);
   SchedulerCheck(DetailNumber(details.GetValue(4), "Invocation") == DetailNumber(details.GetValue(5), "Invocation") &&
     DetailNumber(details.GetValue(4), "ThreadTag") != DetailNumber(details.GetValue(5), "ThreadTag"),
     "transferred fake invocation must record different local thread labels");
   Console.WriteLine("FAKE_CROSS_THREAD_OBSERVATION_ONLY");
  }
  static bool SchedulerExperiment() {
   schedulerChecks = 0;
   Console.WriteLine("FAKE_SCHEDULER_EXPERIMENT_ACTIVE");
   try {
    SchedulerFailureAndReplay();
    SchedulerBeforeStoreAndInline();
    SchedulerDelayedThreadAndUnload();
    SchedulerIsolationAndPairing();
    SchedulerOldGenerationAfterReload();
    SchedulerCrossThreadTokenTransfer();
    SchedulerStopAndCapacity();
    SchedulerThreadCapacityAndFields();
    Console.WriteLine("FAKE_SCHEDULER_EXPERIMENT_PASS");
    return true;
   } catch (SchedulerAssertionFailure exception) {
    Console.WriteLine("FAKE_SCHEDULER_FAIL " + exception.Message);
    Environment.ExitCode = 1;
    return false;
   } catch (Exception) {
    Console.WriteLine("FAKE_SCHEDULER_UNEXPECTED_FAILURE");
    Environment.ExitCode = 1;
    return false;
   } finally {
    Console.WriteLine("FAKE_SCHEDULER_CHECKS_EXECUTED=" + schedulerChecks);
   }
  }
#endif
 static void Main() {
  Assembly assembly = typeof(LifecycleTraceCandidateChecks).Assembly;
  traceType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.LifecycleTraceCandidate");
  componentType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.TraceComponent");
  recordType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.TraceRecord");
  if (traceType == null || componentType == null || recordType == null) throw new Exception("isolated lifecycle trace candidate is missing");
  DefaultDisabled(); OrderingAndIsolation(); BoundedConcurrentEvents();
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
   if (!SchedulerExperiment()) return;
#else
   Console.WriteLine("FAKE_SCHEDULER_EXPERIMENT_NOT_COMPILED");
#endif
  Console.WriteLine("Passed " + checks + " isolated lifecycle trace checks; no game connection or persistent export tested.");
 }
}
