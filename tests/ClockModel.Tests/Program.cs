// Signature: OpenAI-Codex_Responsible: Realistic_City_Time
using System;
using System.Reflection;

internal static class Program
{
    private static int count;
    private static readonly Type Model = typeof(Program).Assembly.GetType("RealisticCityTime.ClockModel");

    private static object New(int precision)
    {
        try { return Activator.CreateInstance(Model, precision); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static object Call(object instance, string name, params object[] args)
    {
        try { return Model.GetMethod(name).Invoke(instance, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Equal(object expected, object actual)
    {
        if (!expected.Equals(actual)) throw new Exception($"Expected {expected}, got {actual}");
    }
    private static void Test(string name, Action test)
    {
        test(); count++; Console.WriteLine("PASS " + name);
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static long Advance(object model, long ticks, decimal multiplier) =>
        (long)Call(model, "Advance", ticks, multiplier);

    private static int Main()
    {
        try
        {
            if (Model == null) throw new Exception("Missing ClockModel: decimal multiplier behavior is not implemented");
            Test("default multiplier is one", () => Equal(1m, Model.GetField("DefaultMultiplier").GetRawConstantValue()));
            foreach (var pair in new[] { ("1", 1m), ("10", 10m), ("1.25", 1.25m), ("9.999", 9.999m) })
                Test("parse " + pair.Item1, () => Equal(pair.Item2, Call(New(3), "ParseMultiplier", pair.Item1)));
            foreach (string input in new[] { "", "abc", "NaN", "Infinity", "-Infinity", "0.5", "0", "10.001", "1,25", "1e0", " 1.25 ", "+1.25" })
                Test("reject input " + input, () => Reject<ArgumentException>(() => Call(New(3), "ParseMultiplier", input)));
            Test("null rejected", () => Reject<ArgumentException>(() => Call(New(3), "ParseMultiplier", new object[] { null })));
            Test("precision is supplied, not defaulted", () => Reject<ArgumentOutOfRangeException>(() => New(29)));
            Test("excess precision rejected, not rounded", () => Reject<ArgumentException>(() => Call(New(2), "ParseMultiplier", "1.234")));
            Test("trailing zeros do not consume precision", () => Equal(1.25m, Call(New(2), "ParseMultiplier", "1.2500")));
            Test("zero precision policy rejects fractional input", () => Reject<ArgumentException>(() => Call(New(0), "ParseMultiplier", "1.1")));
            Test("one minute at one", () => Equal(600000000L, Advance(New(3), 600000000L, 1m)));
            Test("one minute at ten", () => Equal(6000000000L, Advance(New(3), 600000000L, 10m)));
            Test("decimal conversion", () => Equal(750000000L, Advance(New(3), 600000000L, 1.25m)));
            Test("fractional ticks carried across calls", () => { var m=New(3); Equal(1L,Advance(m,1,1.25m)); Equal(1L,Advance(m,1,1.25m)); Equal(1L,Advance(m,1,1.25m)); Equal(2L,Advance(m,1,1.25m)); });
            Test("partition invariance", () => { var m=New(3); long sum=0; for(int i=0;i<10000;i++) sum+=Advance(m,1,1.333m); Equal(Advance(New(3),10000,1.333m),sum); });
            Test("multiplier change preserves fractional elapsed time", () => {var m=New(3); Equal(1L,Advance(m,1,1.5m)); Equal(2L,Advance(m,1,1.5m)); Equal(1L,Advance(m,1,1.25m)); Equal(2L,Advance(m,1,1.75m)); });
            Test("direct numeric bounds and precision enforced", () => { var m=New(2); Reject<ArgumentException>(()=>Advance(m,1,0.9m)); Reject<ArgumentException>(()=>Advance(m,1,10.1m)); Reject<ArgumentException>(()=>Advance(m,1,1.001m)); });
            Test("negative elapsed rejected", () => Reject<ArgumentOutOfRangeException>(()=>Advance(New(3),-1,1m)));
            Test("overflow rejected atomically", () => {var m=New(3); Equal(1L,Advance(m,1,1.5m)); Reject<OverflowException>(()=>Advance(m,long.MaxValue,10m)); Equal(2L,Advance(m,1,1.5m)); });
            Test("invalid multiplier preserves remainder", () => {var m=New(3); Advance(m,1,1.5m); Reject<ArgumentException>(()=>Advance(m,1,11m)); Equal(2L,Advance(m,1,1.5m)); });
            Test("explicit reset clears fractional remainder", () => {var m=New(3); Advance(m,1,1.5m); Call(m,"Reset"); Equal(1L,Advance(m,1,1.5m)); });
            Test("zero elapsed does not advance", () => {var m=New(3); Advance(m,1,1.5m); Equal(0L,Advance(m,0,1m)); Equal(2L,Advance(m,1,1.5m)); });
            Console.WriteLine($"Passed {count} model checks; no game adapter or gameplay tested.");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
