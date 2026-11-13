// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic in-memory checks only; no game callbacks or file export.
using System;
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
 static void Main() {
  Assembly assembly = typeof(LifecycleTraceCandidateChecks).Assembly;
  traceType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.LifecycleTraceCandidate");
  componentType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.TraceComponent");
  recordType = assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.TraceRecord");
  if (traceType == null || componentType == null || recordType == null) throw new Exception("isolated lifecycle trace candidate is missing");
  DefaultDisabled(); OrderingAndIsolation(); BoundedConcurrentEvents();
  Console.WriteLine("Passed " + checks + " isolated lifecycle trace checks; no game connection or persistent export tested.");
 }
}
