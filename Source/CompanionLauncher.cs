#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AIGaming
{
    internal sealed class CompanionSession : IDisposable
    {
        private readonly object _sequenceLock = new object();
        private int _disposeStarted;

        internal Process Process { get; private set; }
        internal byte[] SessionToken { get; private set; }
        internal int Port { get; private set; }
        internal ulong Sequence { get; private set; }

        internal CompanionSession(Process process, byte[] token, int port)
        {
            Process = process;
            SessionToken = token;
            Port = port;
        }

        internal ulong NextSequence()
        {
            lock (_sequenceLock)
            {
                if (Sequence == ulong.MaxValue)
                {
                    throw new InvalidOperationException("The companion request sequence is exhausted.");
                }
                Sequence++;
                return Sequence;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
            try { Process.StandardInput.Close(); }
            catch { }
            if (SessionToken != null)
            {
                Array.Clear(SessionToken, 0, SessionToken.Length);
                SessionToken = null;
            }
            try { Process.Dispose(); }
            catch { }
        }
    }

    internal static class CompanionLauncher
    {
        private const string ExecutableName = "AIGaming.Companion.exe";
        private const int HandshakeTimeoutMilliseconds = 15000;
        private const int ShutdownWaitMilliseconds = 2500;

        internal static bool TryStart(out CompanionSession session, out string errorCode)
        {
            session = null;
            errorCode = "companion_start_failed";
            try
            {
                return TryStartCore(out session, out errorCode);
            }
            catch
            {
                if (session != null)
                {
                    try { session.Dispose(); }
                    catch { }
                    session = null;
                }
                errorCode = "companion_start_failed";
                return false;
            }
        }

        private static bool TryStartCore(out CompanionSession session, out string errorCode)
        {
            session = null;
            errorCode = "companion_unavailable";
            string expectedHash = CompanionBuildInfo.Sha256;
            if (!IsSha256(expectedHash) || string.Equals(expectedHash, "UNCONFIGURED", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = "companion_build_not_pinned";
                return false;
            }

            if (!string.Equals(CompanionBuildInfo.Edition, "release", StringComparison.Ordinal) &&
                !string.Equals(CompanionBuildInfo.Edition, "development", StringComparison.Ordinal))
            {
                errorCode = "companion_edition_invalid";
                return false;
            }

            string modDirectory;
            string sourceExecutable;
            try
            {
                modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(modDirectory))
                {
                    errorCode = "companion_missing";
                    return false;
                }
                sourceExecutable = Path.GetFullPath(Path.Combine(Path.Combine(modDirectory, "Companion"), ExecutableName));
            }
            catch
            {
                errorCode = "companion_missing";
                return false;
            }

            if (!File.Exists(sourceExecutable) || IsReparsePoint(sourceExecutable))
            {
                errorCode = "companion_missing";
                return false;
            }

            string sourceHash;
            try { sourceHash = ComputeFileSha256(sourceExecutable); }
            catch { errorCode = "companion_unavailable"; return false; }
            if (!string.Equals(sourceHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                errorCode = "companion_hash_mismatch";
                return false;
            }

            string cachedExecutable;
            try
            {
                cachedExecutable = StageImmutableCache(sourceExecutable, expectedHash);
            }
            catch
            {
                errorCode = "companion_cache_unavailable";
                return false;
            }

            Process process = null;
            try
            {
                var start = new ProcessStartInfo(cachedExecutable)
                {
                    Arguments = string.Empty,
                    WorkingDirectory = Path.GetDirectoryName(cachedExecutable),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                process = Process.Start(start);
                if (process == null)
                {
                    errorCode = "companion_start_failed";
                    return false;
                }
                process.ErrorDataReceived += delegate { };
                process.BeginErrorReadLine();

                string parentImage = Process.GetCurrentProcess().MainModule.FileName;
                string bootstrap = SerializeBootstrap(parentImage, Process.GetCurrentProcess().Id, expectedHash);
                process.StandardInput.WriteLine(bootstrap);
                process.StandardInput.Flush();

                string ready;
                if (!TryReadLineWithTimeout(process, out ready))
                {
                    errorCode = "companion_handshake_timeout";
                    CloseBootstrapProcess(process);
                    process = null;
                    return false;
                }

                int port;
                byte[] token;
                if (!TryParseReady(ready, process.Id, Process.GetCurrentProcess().Id, expectedHash, out port, out token))
                {
                    errorCode = "companion_identity_mismatch";
                    CloseBootstrapProcess(process);
                    process = null;
                    return false;
                }

                if (process.HasExited)
                {
                    Array.Clear(token, 0, token.Length);
                    errorCode = "companion_exited";
                    CloseBootstrapProcess(process);
                    process = null;
                    return false;
                }

                session = new CompanionSession(process, token, port);
                process = null;
                errorCode = string.Empty;
                return true;
            }
            catch
            {
                errorCode = "companion_start_failed";
                if (process != null)
                {
                    CloseBootstrapProcess(process);
                    process = null;
                }
                return false;
            }
        }

        private static string StageImmutableCache(string sourceExecutable, string expectedHash)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData)) throw new InvalidOperationException();

            string cacheRoot = Path.GetFullPath(Path.Combine(
                Path.Combine(Path.Combine(localAppData, "SpaceSquare640"), "AI Gaming"),
                "Companion"));
            string editionDirectory = Path.Combine(cacheRoot, CompanionBuildInfo.Edition);
            string versionDirectory = Path.Combine(
                editionDirectory,
                CompanionBuildInfo.Version + "-" + expectedHash.ToLowerInvariant());
            string finalPath = Path.Combine(versionDirectory, ExecutableName);

            EnsureSafeDirectoryTree(localAppData, cacheRoot);
            EnsureSafeDirectoryTree(localAppData, editionDirectory);
            EnsureSafeDirectoryTree(localAppData, versionDirectory);
            EnsureInsideRoot(cacheRoot, finalPath);

            if (File.Exists(finalPath))
            {
                if (IsReparsePoint(finalPath) ||
                    !string.Equals(ComputeFileSha256(finalPath), expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException();
                }
                return finalPath;
            }

            string temporaryPath = Path.Combine(versionDirectory, ".candidate-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (FileStream input = new FileStream(sourceExecutable, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[65536];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                }
                output.Flush();
            }

            if (!string.Equals(ComputeFileSha256(temporaryPath), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException();
            }

            try
            {
                File.Move(temporaryPath, finalPath);
            }
            catch (IOException)
            {
                if (!File.Exists(finalPath) || IsReparsePoint(finalPath) ||
                    !string.Equals(ComputeFileSha256(finalPath), expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }

            return finalPath;
        }

        private static void EnsureSafeDirectoryTree(string root, string target)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullTarget = Path.GetFullPath(target);
            EnsureInsideRoot(fullRoot, fullTarget);

            string relative = fullTarget.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string current = fullRoot;
            if (!Directory.Exists(current) || IsReparsePoint(current)) throw new InvalidOperationException();

            string[] segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                current = Path.Combine(current, segments[i]);
                if (!Directory.Exists(current)) Directory.CreateDirectory(current);
                if (IsReparsePoint(current)) throw new InvalidOperationException();
            }
        }

        private static void EnsureInsideRoot(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        }

        private static bool IsReparsePoint(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }

        private static string ComputeFileSha256(string path)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = sha256.ComputeHash(stream);
                var text = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!((character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f') ||
                    (character >= 'A' && character <= 'F'))) return false;
            }
            return true;
        }

        private static string SerializeBootstrap(string parentImage, int parentProcessId, string expectedHash)
        {
            string payload = string.Join("\n", new[]
            {
                "protocol=" + CompanionBuildInfo.ProtocolVersion,
                "edition=" + CompanionBuildInfo.Edition,
                "modVersion=" + CompanionBuildInfo.Version,
                "companionVersion=" + CompanionBuildInfo.Version,
                "expectedSha256=" + expectedHash.ToLowerInvariant(),
                "parentProcessId=" + parentProcessId.ToString(CultureInfo.InvariantCulture),
                "parentImage=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(parentImage))
            });
            return "BOOT:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        }

        private static bool TryReadLineWithTimeout(Process process, out string line)
        {
            line = null;
            string result = null;
            Exception failure = null;
            var completed = new ManualResetEvent(false);
            var reader = new Thread(delegate()
            {
                try { result = process.StandardOutput.ReadLine(); }
                catch (Exception exception) { failure = exception; }
                finally { completed.Set(); }
            });
            reader.IsBackground = true;
            reader.Start();

            if (!completed.WaitOne(HandshakeTimeoutMilliseconds, false))
            {
                try { process.StandardInput.Close(); }
                catch { }
                try { process.WaitForExit(ShutdownWaitMilliseconds); }
                catch { }
                if (completed.WaitOne(ShutdownWaitMilliseconds, false)) completed.Close();
                return false;
            }

            completed.Close();
            if (failure != null || string.IsNullOrEmpty(result)) return false;
            line = result;
            return true;
        }

        private static bool TryParseReady(
            string text,
            int expectedProcessId,
            int expectedParentProcessId,
            string expectedHash,
            out int port,
            out byte[] token)
        {
            port = 0;
            token = null;
            if (string.IsNullOrEmpty(text) || !text.StartsWith("READY:", StringComparison.Ordinal)) return false;

            string payload;
            try { payload = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(text.Substring(6))); }
            catch { return false; }

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = payload.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int separator = lines[i].IndexOf('=');
                if (separator <= 0) return false;
                string key = lines[i].Substring(0, separator);
                if (values.ContainsKey(key)) return false;
                values.Add(key, lines[i].Substring(separator + 1));
            }

            string protocol;
            string edition;
            string version;
            string hash;
            string portText;
            string processText;
            string parentText;
            string secretText;
            int processId;
            int parentProcessId;
            if (values.Count != 8 ||
                !values.TryGetValue("protocol", out protocol) || protocol != CompanionBuildInfo.ProtocolVersion ||
                !values.TryGetValue("edition", out edition) || edition != CompanionBuildInfo.Edition ||
                !values.TryGetValue("companionVersion", out version) || version != CompanionBuildInfo.Version ||
                !values.TryGetValue("sha256", out hash) || !string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase) ||
                !values.TryGetValue("port", out portText) || !int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535 ||
                !values.TryGetValue("processId", out processText) || !int.TryParse(processText, NumberStyles.None, CultureInfo.InvariantCulture, out processId) || processId != expectedProcessId ||
                !values.TryGetValue("parentProcessId", out parentText) || !int.TryParse(parentText, NumberStyles.None, CultureInfo.InvariantCulture, out parentProcessId) || parentProcessId != expectedParentProcessId ||
                !values.TryGetValue("sessionToken", out secretText))
            {
                return false;
            }

            try { token = Convert.FromBase64String(secretText); }
            catch { return false; }
            return token.Length == 32;
        }

        private static void CloseBootstrapProcess(Process process)
        {
            try { process.StandardInput.Close(); }
            catch { }
            try { process.WaitForExit(ShutdownWaitMilliseconds); }
            catch { }
            try { process.Dispose(); }
            catch { }
        }
    }
}
// Implementation update signed by OpenAI-Codex.
// Session-disposal lifecycle fix: OpenAI-Codex_AI_Gaming.
