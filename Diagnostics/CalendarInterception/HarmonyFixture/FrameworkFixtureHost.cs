// A fresh x86 .NET Framework 4 fixture host; it never chooses a target method.
// The caller must supply the one explicitly authorized Harmony DLL path.
// This host verifies bytes and invokes only the fixture in its own executable.
// It does not establish game, Unity, Mono, save, or native clock safety.
// Execution evidence markers: OpenAI-Codex
// Signature: OpenAI-Codex_harmony_fixture_impl
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

[assembly: AssemblyDescription("Synthetic fixture by OpenAI-Codex and OpenAI-Codex_harmony_fixture_impl.")]

public static class FrameworkFixtureHost
{
    private const int ExpectedLength = 2575872;
    private const string ExpectedSha256 = "C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503";
    private const string ExpectedName = "0Harmony";
    private static readonly Version ExpectedVersion = new Version(2, 4, 3, 0);

    private static void RequireX86Executable()
    {
        PortableExecutableKinds kind;
        ImageFileMachine machine;
        Assembly.GetExecutingAssembly().ManifestModule.GetPEKind(out kind, out machine);
        if (IntPtr.Size != 4 || machine != ImageFileMachine.I386 || (kind & PortableExecutableKinds.Required32Bit) == 0)
            throw new InvalidOperationException("The fixture host must be an x86 executable in a 32-bit process.");
    }

    private static bool IsGameAssembly(string name)
    {
        return name.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "ColossalManaged", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "ICities", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHarmonyAssembly(string name)
    {
        return String.Equals(name, "0Harmony", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "Harmony", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "HarmonyLib", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "HarmonyX", StringComparison.OrdinalIgnoreCase);
    }

    private static void RequireNoGameAssemblies()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (IsGameAssembly(assembly.GetName().Name))
                throw new InvalidOperationException("A named game or Unity assembly is loaded.");
    }

    private static void RequireNoPreloadedHarmony()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (IsHarmonyAssembly(assembly.GetName().Name))
                throw new InvalidOperationException("A Harmony assembly was already loaded.");
    }

    private static void RequireOnlyVerifiedHarmony(Assembly verified)
    {
        int matches = 0;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!IsHarmonyAssembly(assembly.GetName().Name)) continue;
            if (!Object.ReferenceEquals(assembly, verified))
                throw new InvalidOperationException("Another Harmony assembly is loaded.");
            matches++;
        }
        if (matches != 1) throw new InvalidOperationException("The verified Harmony assembly is not uniquely loaded.");
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", String.Empty);
    }

    private static string Hash(FileStream stream)
    {
        stream.Position = 0;
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", String.Empty);
    }

    private static byte[] ReadLockedBytes(FileStream stream)
    {
        if (stream.Length != ExpectedLength) throw new InvalidDataException("Harmony DLL length does not match the approved length.");
        byte[] bytes = new byte[ExpectedLength];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int read = stream.Read(bytes, offset, bytes.Length - offset);
            if (read <= 0) throw new EndOfStreamException("Harmony DLL was not read completely.");
            offset += read;
        }
        if (stream.ReadByte() != -1) throw new InvalidDataException("Harmony DLL contains unexpected extra bytes.");
        if (!String.Equals(Hash(bytes), ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Harmony DLL SHA-256 does not match the approved hash.");
        return bytes;
    }

    private static FieldInfo DisableHarmonyDebug(Assembly harmony)
    {
        Type type = harmony.GetType("HarmonyLib.Harmony", true, false);
        FieldInfo field = type.GetField("DEBUG", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (field == null || field.FieldType != typeof(bool) || field.IsInitOnly || field.IsLiteral)
            throw new InvalidOperationException("Harmony DEBUG cannot be explicitly disabled.");
        field.SetValue(null, false);
        if ((bool)field.GetValue(null)) throw new InvalidOperationException("Harmony DEBUG is still enabled.");
        return field;
    }

    private static void InvokeOwnedFixture(Assembly verifiedHarmony)
    {
        Assembly executing = Assembly.GetExecutingAssembly();
        Type fixture = executing.GetType("HarmonyFixtureChecks", false, false);
        if (fixture == null || fixture.Assembly != executing)
            throw new InvalidOperationException("The fixture entry point is not in this executable.");
        MethodInfo run = fixture.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null, new Type[] { typeof(Assembly) }, null);
        if (run == null || run.DeclaringType != fixture || run.ReturnType != typeof(void))
            throw new InvalidOperationException("The fixture entry point signature is not supported.");
        run.Invoke(null, new object[] { verifiedHarmony });
    }

    private static void Report(string phase, Exception error)
    {
        Console.Error.WriteLine(phase + ": " + error.GetType().FullName + ": " + error.Message);
        Exception inner = error.InnerException;
        while (inner != null)
        {
            Console.Error.WriteLine("Inner exception: " + inner.GetType().FullName + ": " + inner.Message);
            inner = inner.InnerException;
        }
    }

    public static int Main(string[] args)
    {
        FileStream stream = null;
        ResolveEventHandler resolver = null;
        Assembly verified = null;
        int result = 1;
        try
        {
            RequireX86Executable();
            Console.WriteLine("PASS: x86 fixture process");
            RequireNoGameAssemblies();
            RequireNoPreloadedHarmony();
            if (args == null || args.Length != 1 || String.IsNullOrEmpty(args[0]) || !Path.IsPathRooted(args[0]))
                throw new ArgumentException("Supply exactly one absolute authorized Harmony DLL path.");

            Environment.SetEnvironmentVariable("HARMONY_DEBUG", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("HARMONY_NO_LOG", "1", EnvironmentVariableTarget.Process);
            if (Environment.GetEnvironmentVariable("HARMONY_DEBUG", EnvironmentVariableTarget.Process) != null ||
                Environment.GetEnvironmentVariable("HARMONY_NO_LOG", EnvironmentVariableTarget.Process) != "1")
                throw new InvalidOperationException("Process-local Harmony logging settings could not be established.");

            stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] bytes = ReadLockedBytes(stream);
            Console.WriteLine("PASS: approved DLL length and SHA-256 from locked bytes");

            resolver = delegate(object sender, ResolveEventArgs request)
            {
                AssemblyName requested = new AssemblyName(request.Name);
                if (verified == null || !String.Equals(requested.Name, ExpectedName, StringComparison.OrdinalIgnoreCase) ||
                    requested.Version == null || !requested.Version.Equals(ExpectedVersion) ||
                    !String.Equals(requested.FullName, verified.GetName().FullName, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Unapproved assembly resolution request: " + requested.Name);
                return verified;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            verified = Assembly.Load(bytes);
            AssemblyName identity = verified.GetName();
            if (!String.Equals(identity.Name, ExpectedName, StringComparison.OrdinalIgnoreCase) || !ExpectedVersion.Equals(identity.Version))
                throw new InvalidDataException("The verified DLL is not 0Harmony 2.4.3.0.");
            RequireOnlyVerifiedHarmony(verified);
            RequireNoGameAssemblies();
            FieldInfo debug = DisableHarmonyDebug(verified);
            Console.WriteLine("PASS: approved Harmony identity and disabled debug");

            InvokeOwnedFixture(verified);
            RequireOnlyVerifiedHarmony(verified);
            RequireNoGameAssemblies();
            if ((bool)debug.GetValue(null) || Environment.GetEnvironmentVariable("HARMONY_NO_LOG", EnvironmentVariableTarget.Process) != "1")
                throw new InvalidOperationException("Harmony debug or process-local no-log setting changed.");
            result = 0;
        }
        catch (Exception error)
        {
            Report("Fixture host failed", error);
            result = 1;
        }
        finally
        {
            if (stream != null)
            {
                try
                {
                    if (stream.Length != ExpectedLength || !String.Equals(Hash(stream), ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The locked DLL handle no longer has the approved length and SHA-256.");
                    Console.WriteLine("PASS: final SHA-256 from the same locked handle");
                }
                catch (Exception error) { Report("Final DLL verification failed", error); result = 1; }
            }
            try { RequireNoGameAssemblies(); Console.WriteLine("PASS: final no named game assemblies"); }
            catch (Exception error) { Report("Final loaded-assembly check failed", error); result = 1; }
            if (resolver != null)
            {
                try { AppDomain.CurrentDomain.AssemblyResolve -= resolver; }
                catch (Exception error) { Report("Resolver cleanup failed", error); result = 1; }
            }
            if (stream != null)
            {
                try { stream.Dispose(); }
                catch (Exception error) { Report("DLL handle cleanup failed", error); result = 1; }
            }
        }
        return result;
    }
}
