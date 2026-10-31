// Signature: OpenAI-Codex_Responsible: Realistic_Vehicle_Traffic_Control
using System.Reflection;

// Reflection permits the first cloud run to compile the test harness against an
// empty library and report missing behavior as failed checks, not compiler errors.
static class Program
{
    static readonly Assembly Model = Assembly.Load("RealisticVehicleTrafficControl.Model");
    static Type T(string name) => Model.GetType("RealisticVehicleTrafficControl.Model." + name)
        ?? throw new Exception("Missing production model: " + name);
    static dynamic New(string name, params object[] args) => Activator.CreateInstance(T(name), args);
    static object E(string type, string name) => Enum.Parse(T(type), name);
    static Array Lanes(params object[] values)
    {
        var array = Array.CreateInstance(T("LaneObservation"), values.Length);
        for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i);
        return array;
    }
    static dynamic Lane(uint id, int rank, bool reversed = false, string kind = "GeneralCar",
        string turn = "Forward", uint[] allowed = null, ushort segment = 10, uint build = 1)
        => New("LaneObservation", id, segment, build, rank, reversed,
            E("LaneKind", kind), E("TurnKind", turn), allowed ?? Array.Empty<uint>());
    static Array Inputs(int count) => Lanes(Enumerable.Range(0, count)
        .Select(i => (object)Lane((uint)(i + 1), i, allowed: Enumerable.Range(100, 24).Select(x => (uint)x).ToArray())).ToArray());
    static Array Exits(int count) => Lanes(Enumerable.Range(0, count).Select(i => (object)Lane(
        (uint)(100 + i), i, turn: count > 8 ? (i < 3 ? "Left" : i < 6 ? "Forward" : "Right") : "Forward",
        segment: (ushort)(20 + i / 3))).ToArray());
    static dynamic Snapshot(Array incoming = null, Array outgoing = null, bool left = false,
        string origin = "SyntheticFixture", bool geometry = true, bool rules = true,
        long epoch = 1, ushort node = 7, uint revision = 1, int segments = 4)
        => New("JunctionSnapshot", epoch, node, revision, left, segments, geometry, rules,
            E("DataOrigin", origin), incoming ?? Inputs(2), outgoing ?? Exits(3));
    static dynamic Build(dynamic snapshot) => New("LaneProposalPlanner").Build((object)snapshot);
    static void Check(bool value, string failure) { if (!value) throw new Exception(failure); }
    static void Status(dynamic proposal, string expected) => Check(proposal.Status.ToString() == expected,
        "Expected " + expected + ", got " + proposal.Status + ": " + proposal.Reason);
    static string Describe(dynamic result)
    {
        var rows = new List<string>();
        foreach (dynamic connection in result.Connections)
            rows.Add(connection.IncomingLaneId + ":" + string.Join(",", (uint[])connection.OutgoingLaneIds));
        return string.Join(";", rows);
    }
    static void Ready(int incoming, int outgoing, string expected)
    {
        var result = Build(Snapshot(Inputs(incoming), Exits(outgoing)));
        Status(result, "Ready");
        Check(Describe(result) == expected, "Wrong lane mapping: " + Describe(result));
        Check(result.Origin.ToString() == "SyntheticFixture", "Fixture presented as live data");
    }
    static int Main()
    {
        var tests = new (string, Action)[] {
            ("TwoInThreeOut", () => Ready(2,3,"1:100,101;2:102")),
            ("ThreeInEightOut", () => Ready(3,8,"1:100,101,102;2:103,104,105;3:106,107")),
            ("ThreeInNineOut", () => Ready(3,9,"1:100,101,102;2:103,104,105;3:106,107,108")),
            ("MoreIncomingThanOutgoing", () => Ready(3,2,"1:100;2:100;3:101")),
            ("NoLegalExitRefused", () => {
                var result = Build(Snapshot(Lanes(Lane(1,0,allowed:new uint[]{999})), Exits(1)));
                Status(result,"Unsupported"); Check(result.Connections.Length == 0,"Partial unsafe result");
            }),
            ("UnknownSourceNotInvented", () => Status(Build(Snapshot(origin:"Unknown")),"Unknown")),
            ("UnknownExternalRulesRefused", () => Status(Build(Snapshot(rules:false)),"Unsupported")),
            ("MixedRailRefused", () => Status(Build(Snapshot(Lanes(Lane(1,0,kind:"Rail")))),"Unsupported")),
            ("BusLaneRefused", () => Status(Build(Snapshot(Lanes(Lane(1,0,kind:"Bus")))),"Unsupported")),
            ("UnknownLaneRefused", () => Status(Build(Snapshot(Lanes(Lane(1,0,kind:"Unknown")))),"Unsupported")),
            ("OrdinarySidewalkAllowed", () => {
                var result=Build(Snapshot(Lanes(Lane(1,0,allowed:new uint[]{100}),Lane(9,1,kind:"Sidewalk")),Exits(1)));
                Status(result,"Ready"); Check(Describe(result)=="1:100","Sidewalk used as car lane");
            }),
            ("PedestrianOnlyRefused", () => Status(Build(Snapshot(Lanes(Lane(9,0,kind:"Sidewalk")))),"Unsupported")),
            ("UnsupportedGeometryRefused", () => Status(Build(Snapshot(geometry:false)),"Unsupported")),
            ("TooManyApproachLanesRefused", () => Status(Build(Snapshot(Inputs(9),Exits(9))),"Unsupported")),
            ("TooManyNodeSegmentsRefused", () => Status(Build(Snapshot(segments:5)),"Unsupported")),
            ("DuplicateLaneIdentityRefused", () => Status(Build(Snapshot(Lanes(Lane(1,0),Lane(1,1)))),"Unsupported")),
            ("LeftHandMirror", () => {
                var s=Snapshot(Lanes(Lane(1,0,allowed:new uint[]{100,101,102}),Lane(2,-1,allowed:new uint[]{100,101,102})),
                    Lanes(Lane(100,0,segment:20),Lane(101,-1,segment:20),Lane(102,-2,segment:20)),left:true);
                Check(Describe(Build(s))=="1:100,101;2:102","Left-hand mirror changed topology");
            }),
            ("ReversedOrientationEquivalent", () => {
                var s=Snapshot(Lanes(Lane(1,0,true,allowed:new uint[]{100,101,102}),Lane(2,-1,true,allowed:new uint[]{100,101,102})),
                    Lanes(Lane(100,0,true,segment:20),Lane(101,-1,true,segment:20),Lane(102,-2,true,segment:20)));
                Check(Describe(Build(s))=="1:100,101;2:102","Orientation normalization failed");
            }),
            ("SnapshotCopyIsolation", () => {
                uint[] targets={100,101,102}; Array input=Lanes(Lane(1,0,allowed:targets),Lane(2,1,allowed:targets));
                var s=Snapshot(input); targets[0]=999; input.SetValue(Lane(99,0),0);
                Array copy=s.Incoming; copy.SetValue(Lane(98,0),0);
                Check(Describe(Build(s))=="1:100,101;2:102","Input mutation escaped snapshot");
            }),
            ("ProposalCopyIsolation", () => {
                var p=Build(Snapshot()); var copy=p.Connections; uint[] ids=copy[0].OutgoingLaneIds; ids[0]=999; copy[0]=null;
                Check(Describe(p)=="1:100,101;2:102","Result mutation escaped immutable proposal");
            }),
            ("DeterministicUnderInputPermutation", () => {
                var s=Snapshot(Lanes(Lane(2,1,allowed:new uint[]{100,101,102}),Lane(1,0,allowed:new uint[]{100,101,102})),
                    Lanes(Lane(102,2,segment:20),Lane(100,0,segment:20),Lane(101,1,segment:20)));
                Check(Describe(Build(s))=="1:100,101;2:102","Enumeration order changed proposal");
            }),
            ("LateRequestIgnored", () => {
                var session=New("PreviewLifecycle",1L); var old=session.Begin((ushort)7,(uint)1);
                var current=session.Begin((ushort)7,(uint)1);
                Check(!session.Accept(old,Snapshot()),"Old callback accepted");
                Check(session.Accept(current,Snapshot()),"Current callback refused");
            }),
            ("CityChangeDropsPending", () => {
                var session=New("PreviewLifecycle",1L); var token=session.Begin((ushort)7,(uint)1); session.Invalidate(2L);
                Check(!session.Accept(token,Snapshot()),"Old city callback accepted");
            }),
            ("TopologyChangeInvalidates", () => {
                var session=New("PreviewLifecycle",1L); var token=session.Begin((ushort)7,(uint)1);
                Check(session.Accept(token,Snapshot()),"Initial capture refused");
                Check(!session.IsCurrent(Snapshot(revision:2)),"Reused topology considered current");
            }),
            ("LaneIdentityReuseInvalidates", () => {
                var session=New("PreviewLifecycle",1L); var token=session.Begin((ushort)7,(uint)1); session.Accept(token,Snapshot());
                var changed=Snapshot(outgoing:Lanes(Lane(100,0,segment:20,build:2),Lane(101,1,segment:20),Lane(102,2,segment:20)));
                Check(!session.IsCurrent(changed),"Changed lane generation considered current");
            }),
            ("DuplicateCallbackIgnored", () => {
                var session=New("PreviewLifecycle",1L); var token=session.Begin((ushort)7,(uint)1);
                Check(session.Accept(token,Snapshot()),"First capture refused");
                Check(!session.Accept(token,Snapshot()),"Capture accepted twice");
            })
        };
        int failures=0;
        foreach(var (name,run) in tests) {
            try {run(); Console.WriteLine("PASS "+name);}
            catch(Exception e) {failures++; Console.WriteLine("FAIL "+name+": "+(e.InnerException??e).Message);}
        }
        Console.WriteLine($"RESULT {tests.Length-failures}/{tests.Length} passed; {failures} failed. Synthetic fixtures only; no game capture.");
        return failures==0 ? 0 : 1;
    }
}
