using System.Net.Http;
using System.Reflection;
using System.Text;

namespace AIGaming.Companion;

internal static class Program
{
    private const int Limit = 8;
    private const int FormerMaximumResponseBytes = 1024 * 1024;

    private static async Task Main()
    {
        SessionDisposalClearsTokenExactlyOnce();
        ParallelSessionDisposalIsSafe();
        RejectsCancelledModuleRequestBeforeCompanionLaunch();
        ParsesZeroAsUnlimitedResponseSetting();
        ParsesConfiguredResponseLimitInMebibytes();
        ParsesMaximumSupportedResponseLimit();
        RejectsInvalidResponseLimitSettings();
        RejectsCaptureInvalidatedByChangedSettings();
        CaptureInvalidationPreservesActiveResponse();
        AcceptsSchemaV4WithoutOptionalGroups();
        await ReadsBodyBelowLimitAsync();
        await AcceptsBodyAtLimitAsync();
        await RejectsAdvertisedBodyAboveLimitAsync();
        await RejectsUnknownLengthBodyAfterOneExtraByteAsync();
        await ReadsAdvertisedBodyAboveFormerMaximumAsync();
        await ReadsUnknownLengthBodyAboveFormerMaximumAsync();
        AcceptsSchemaV4WithAllCityDataGroups();
        RejectsMissingResponseLimit();
        RejectsNegativeResponseLimit();
        RejectsPartialCityDataGroup();
        Console.WriteLine("PASS: bounded model-list, configurable local response limits, independent capture/response generations, and schema v4 checks.");
    }

    private static void SessionDisposalClearsTokenExactlyOnce()
    {
        var process = new CountingUnstartedProcess();
        byte[] token = Enumerable.Repeat((byte)0xA5, 32).ToArray();
        var session = new AIGaming.CompanionSession(process, token, 1);
        session.Dispose();
        session.Dispose();
        if (process.DisposeCount != 1 || session.SessionToken != null || token.Any(value => value != 0))
            throw new InvalidOperationException("Repeated session disposal must release the process once and clear the original token.");
    }

    private static void ParallelSessionDisposalIsSafe()
    {
        for (int iteration = 0; iteration < 100; iteration++)
        {
            var process = new CountingUnstartedProcess();
            byte[] token = Enumerable.Repeat((byte)0xA5, 32).ToArray();
            var session = new AIGaming.CompanionSession(process, token, 1);
            Parallel.For(0, 16, _ => session.Dispose());
            if (process.DisposeCount != 1 || session.SessionToken != null || token.Any(value => value != 0))
                throw new InvalidOperationException("Parallel session disposal must release the process once and clear the original token.");
        }
        Console.WriteLine("PASS: repeated and concurrent session disposal clears the token and releases the synthetic process once.");
    }

    private static void RejectsCancelledModuleRequestBeforeCompanionLaunch()
    {
        string? path = Environment.GetEnvironmentVariable("AIGAMING_MODULE_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("SKIP: compiled-module cancellation check requires AIGAMING_MODULE_PATH.");
            return;
        }
        string modulePath = Path.GetFullPath(path);
        if (Directory.Exists(Path.Combine(Path.GetDirectoryName(modulePath)!, "Companion")))
            throw new InvalidOperationException("Cancellation fixture must not contain a launchable Companion directory.");
        var assembly = Assembly.LoadFrom(modulePath);
        var clientType = assembly.GetType("AIGaming.CompanionClient", true)!;
        var requestType = assembly.GetType("AIGaming.LocalAiRequest", true)!;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object client = Activator.CreateInstance(clientType, true)!;
        object request = Activator.CreateInstance(requestType, true)!;
        requestType.GetProperty("ModelName", members)!.SetValue(request, "cancelled-request-check");
        clientType.GetMethod("Cancel", members)!.Invoke(client, null);
        object result = clientType.GetMethod("Send", members)!.Invoke(client, new[] { request })!;
        string? code = result.GetType().GetProperty("ErrorCode", members)!.GetValue(result) as string;
        if (code != "cancelled") throw new InvalidOperationException("Cancelled queued request entered Companion startup: " + code);
        Console.WriteLine("PASS: cancelled queued module request stops before Companion startup.");
    }

    private static void RejectsCaptureInvalidatedByChangedSettings()
    {
        var generation = new AIGaming.RequestCaptureGeneration();
        long pendingCapture = generation.Begin();
        generation.Invalidate();

        if (generation.IsCurrent(pendingCapture))
        {
            throw new InvalidOperationException("A pending city capture must be discarded after its settings change.");
        }
    }

    private static void CaptureInvalidationPreservesActiveResponse()
    {
        var generation = new AIGaming.RequestCaptureGeneration();
        long activeResponse = generation.BeginResponse();
        long pendingCapture = generation.Begin();
        generation.Invalidate();

        if (!generation.IsResponseCurrent(activeResponse) || generation.IsCurrent(pendingCapture))
        {
            throw new InvalidOperationException("Invalidating a city capture must not discard an active AI response.");
        }
    }

    private static void ParsesZeroAsUnlimitedResponseSetting()
    {
        if (!TryParseResponseLimit("0", out int mebibytes, out int bytes) || mebibytes != 0 || bytes != 0)
        {
            throw new InvalidOperationException("A zero response limit should mean unlimited.");
        }
    }

    private static void ParsesConfiguredResponseLimitInMebibytes()
    {
        if (!TryParseResponseLimit("8", out int mebibytes, out int bytes) ||
            mebibytes != 8 || bytes != 8388608)
        {
            throw new InvalidOperationException("An 8 MiB response limit should convert to 8388608 bytes.");
        }
    }

    private static void ParsesMaximumSupportedResponseLimit()
    {
        if (!TryParseResponseLimit("2047", out int mebibytes, out int bytes) ||
            mebibytes != 2047 || bytes != 2146435072)
        {
            throw new InvalidOperationException("The maximum supported 2047 MiB response limit should parse without overflow.");
        }
    }

    private static void RejectsInvalidResponseLimitSettings()
    {
        if (TryParseResponseLimit("-1", out _, out _) ||
            TryParseResponseLimit("1.5", out _, out _) ||
            TryParseResponseLimit("2048", out _, out _))
        {
            throw new InvalidOperationException("Negative, fractional, or unsupported MiB limits should be rejected.");
        }
    }

    private static bool TryParseResponseLimit(string text, out int mebibytes, out int bytes)
    {
        Type? type = typeof(Program).Assembly.GetType("AIGaming.LocalAiResponseLimit");
        MethodInfo? method = type?.GetMethod("TryParseMiB", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("The response limit settings parser is not available.");
        }

        object?[] arguments = { text, 0, 0 };
        bool result = (bool)method.Invoke(null, arguments)!;
        mebibytes = (int)arguments[1]!;
        bytes = (int)arguments[2]!;
        return result;
    }

    private static async Task ReadsBodyBelowLimitAsync()
    {
        byte[] expected = "abc"u8.ToArray();
        using var content = new ByteArrayContent(expected);
        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        AssertBytes(expected, actual, "body below limit");
    }

    private static async Task AcceptsBodyAtLimitAsync()
    {
        byte[] expected = Enumerable.Range(0, Limit).Select(value => (byte)value).ToArray();
        using var content = new ByteArrayContent(expected);
        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        AssertBytes(expected, actual, "body at limit");
    }

    private static async Task RejectsAdvertisedBodyAboveLimitAsync()
    {
        using var content = new ByteArrayContent(new byte[Limit + 1]);
        await AssertRejectedAsync(content, "advertised body above limit");
    }

    private static async Task RejectsUnknownLengthBodyAfterOneExtraByteAsync()
    {
        var stream = new NonSeekableReadStream(new byte[Limit + 20]);
        using var content = new StreamContent(stream);
        if (content.Headers.ContentLength != null)
        {
            throw new InvalidOperationException("The test stream must not advertise a content length.");
        }

        await AssertRejectedAsync(content, "unknown-length body above limit");
        if (stream.BytesRead != Limit + 1)
        {
            throw new InvalidOperationException($"Expected {Limit + 1} bytes read, got {stream.BytesRead}.");
        }
    }

    private static async Task ReadsAdvertisedBodyAboveFormerMaximumAsync()
    {
        byte[] expected = Enumerable.Repeat((byte)42, FormerMaximumResponseBytes + 1).ToArray();
        using var content = new ByteArrayContent(expected);
        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(content, 0, CancellationToken.None);
        AssertBytes(expected, actual, "advertised body above former maximum");
    }

    private static async Task ReadsUnknownLengthBodyAboveFormerMaximumAsync()
    {
        byte[] expected = Enumerable.Repeat((byte)42, FormerMaximumResponseBytes + 1).ToArray();
        var stream = new NonSeekableReadStream(expected);
        using var content = new StreamContent(stream);
        if (content.Headers.ContentLength != null)
        {
            throw new InvalidOperationException("The test stream must not advertise a content length.");
        }

        byte[] actual = await BoundedOllamaResponseReader.ReadAsync(
            content,
            0,
            CancellationToken.None);
        AssertBytes(expected, actual, "unknown-length body above former maximum");
        if (stream.BytesRead != expected.Length)
        {
            throw new InvalidOperationException($"Expected {expected.Length} bytes read, got {stream.BytesRead}.");
        }
    }

    private static void AcceptsSchemaV4WithoutOptionalGroups()
    {
        string payload = string.Join("\n", new[]
        {
            "schemaVersion=4",
            "model=" + Convert.ToBase64String("local-model"u8),
            "question=" + Convert.ToBase64String("What should I improve?"u8),
            "installedAvailableDlcs=",
            "maxResponseBytes=0"
        });

        if (!SessionProtocol.TryParseRequest(Encoding.UTF8.GetBytes(payload), out CompanionRequest request) ||
            GetMaximumResponseBytes(request) != 0)
        {
            throw new InvalidOperationException("Schema v4 with no optional groups and unlimited responses should be accepted.");
        }
    }

    private static void AcceptsSchemaV4WithAllCityDataGroups()
    {
        string[] values =
        {
            "electricityCapacity=100", "electricityConsumption=20",
            "waterCapacity=100", "waterConsumption=20",
            "sewageCapacity=100", "sewageAccumulation=20",
            "garbageAmount=10", "garbageCapacity=100",
            "garbageAccumulation=5", "incinerationCapacity=30",
            "residentialBuildingCount=1", "residentialBuildingArea=10",
            "commercialBuildingCount=2", "commercialBuildingArea=20",
            "industrialBuildingCount=3", "industrialBuildingArea=30",
            "officeBuildingCount=4", "officeBuildingArea=40",
            "trafficFlow=70", "averagePassengers=50",
            "policeCriminalAmount=2", "policeCriminalCapacity=10", "policeExtraCriminals=0",
            "fireCoverageMeanByte=128.5", "fireCoverageSegmentCount=10",
            "healthSickCount=3", "healthCapacity=12", "healthCareProductivity=8"
        };
        var lines = new List<string>
        {
            "schemaVersion=4",
            "model=" + Convert.ToBase64String("local-model"u8),
            "question=" + Convert.ToBase64String("What should I improve?"u8),
            "installedAvailableDlcs=",
            "maxResponseBytes=1048576"
        };
        lines.AddRange(values);

        if (!SessionProtocol.TryParseRequest(Encoding.UTF8.GetBytes(string.Join("\n", lines)), out CompanionRequest request))
        {
            throw new InvalidOperationException("Schema v4 with complete city data groups and a configured response limit should be accepted.");
        }
        if (GetMaximumResponseBytes(request) != 1048576 ||
            request.Utilities?.ElectricityCapacity != 100 ||
            request.Zoning?.OfficeBuildingArea != 40 ||
            request.TrafficTransit?.AveragePassengers != 50 ||
            request.ServiceStatus?.FireCoverageMeanByte != 128.5f)
        {
            throw new InvalidOperationException("Schema v4 response limit or city data group values were not parsed correctly.");
        }
    }

    private static void RejectsMissingResponseLimit()
    {
        string payload = string.Join("\n", new[]
        {
            "schemaVersion=4",
            "model=" + Convert.ToBase64String("local-model"u8),
            "question=" + Convert.ToBase64String("What should I improve?"u8),
            "installedAvailableDlcs="
        });

        if (SessionProtocol.TryParseRequest(Encoding.UTF8.GetBytes(payload), out _))
        {
            throw new InvalidOperationException("Schema v4 without maxResponseBytes should be rejected.");
        }
    }

    private static void RejectsNegativeResponseLimit()
    {
        string payload = string.Join("\n", new[]
        {
            "schemaVersion=4",
            "model=" + Convert.ToBase64String("local-model"u8),
            "question=" + Convert.ToBase64String("What should I improve?"u8),
            "installedAvailableDlcs=",
            "maxResponseBytes=-1"
        });

        if (SessionProtocol.TryParseRequest(Encoding.UTF8.GetBytes(payload), out _))
        {
            throw new InvalidOperationException("A negative maxResponseBytes value should be rejected.");
        }
    }

    private static void RejectsPartialCityDataGroup()
    {
        string payload = string.Join("\n", new[]
        {
            "schemaVersion=4",
            "model=" + Convert.ToBase64String("local-model"u8),
            "question=" + Convert.ToBase64String("What should I improve?"u8),
            "installedAvailableDlcs=",
            "maxResponseBytes=0",
            "electricityCapacity=100"
        });

        if (SessionProtocol.TryParseRequest(Encoding.UTF8.GetBytes(payload), out _))
        {
            throw new InvalidOperationException("A partial optional city data group should be rejected.");
        }
    }

    private static async Task AssertRejectedAsync(HttpContent content, string testName)
    {
        try
        {
            _ = await BoundedOllamaResponseReader.ReadAsync(content, Limit, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {testName} to be rejected.");
    }

    private static void AssertBytes(byte[] expected, byte[] actual, string testName)
    {
        if (!expected.AsSpan().SequenceEqual(actual))
        {
            throw new InvalidOperationException($"Unexpected bytes for {testName}.");
        }
    }

    private static int GetMaximumResponseBytes(CompanionRequest request)
    {
        PropertyInfo? property = typeof(CompanionRequest).GetProperty(
            "MaximumResponseBytes",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (property == null || property.GetValue(request, null) is not int maximumResponseBytes)
        {
            throw new InvalidOperationException("The parsed request did not expose its response limit.");
        }

        return maximumResponseBytes;
    }
}

internal sealed class NonSeekableReadStream : Stream
{
    private readonly byte[] _bytes;
    private int _position;

    internal NonSeekableReadStream(byte[] bytes) => _bytes = bytes;

    internal int BytesRead { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesToRead = Math.Min(count, _bytes.Length - _position);
        Array.Copy(_bytes, _position, buffer, offset, bytesToRead);
        _position += bytesToRead;
        BytesRead += bytesToRead;
        return bytesToRead;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int bytesToRead = Math.Min(buffer.Length, _bytes.Length - _position);
        _bytes.AsMemory(_position, bytesToRead).CopyTo(buffer);
        _position += bytesToRead;
        BytesRead += bytesToRead;
        return ValueTask.FromResult(bytesToRead);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

internal sealed class CountingUnstartedProcess : System.Diagnostics.Process
{
    private int _disposeCount;
    internal int DisposeCount => Volatile.Read(ref _disposeCount);

    protected override void Dispose(bool disposing)
    {
        if (disposing) Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}

internal static class CompanionBuildInfo
{
    internal const string Edition = "release";
    internal const string Version = "0.4.0";
    internal const string ProtocolVersion = "1";
}

// Implementation update signed by OpenAI-Codex.
// Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
// Cancellation lifecycle regression: OpenAI-Codex_Responsible: AI_Gaming.
// Synthetic session disposal checks: OpenAI-Codex_AI_Gaming.
