using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.Runtime;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.LogPath;

/// <summary>
/// A utility library for determining the log path across all environments
/// </summary>
public static class LogPathUtil
{
    /// <summary>
    /// Gets the log file path based on the environment.
    /// </summary>
    /// <remarks>Returns a task because this can be used in GetAwaiter() scenarios.</remarks>
    public static async Task<string> Get(string logFileName, CancellationToken cancellationToken = default)
    {
        // 1️⃣  Caller-supplied override
        string? overrideDir = Environment.GetEnvironmentVariable("LOG_PATH");

        if (overrideDir.HasContent())
            return Path.Combine(overrideDir, logFileName);

        // 2️⃣  Azure App Service (Win & Linux: code + custom containers)
        //     Logs are under HOME/LogFiles on Windows (D:\home\LogFiles)
        //     and under /home/LogFiles on Linux (even if HOME=/root in the container).
        if (RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            string baseDir;

            if (RuntimeUtil.IsWindows())
            {
                // Classic Windows App Service: HOME is usually "D:\home"
                // but fall back to that if HOME is missing for some reason.
                if (home.HasContent())
                    baseDir = home;
                else
                    baseDir = @"D:\home";
            }
            else
            {
                // Linux App Service / Web App for Containers:
                // Azure mounts the persistent disk at /home.
                // HOME inside the container can be /root, so prefer /home.
                if (home.HasContent() && home.StartsWith("/home", StringComparison.OrdinalIgnoreCase))
                    baseDir = home;
                else
                    baseDir = "/home";
            }

            string dir = Path.Combine(baseDir, "LogFiles");

            if (TryEnsureWritable(dir))
                return Path.Combine(dir, logFileName);

            // If for some reason it's not writable, we fall through to other strategies.
        }

        // 3️⃣  GitHub Actions (runner has guaranteed write access)
        if (RuntimeUtil.IsGitHubAction)
        {
            string? ghWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

            if (ghWorkspace.HasContent())
            {
                string dir = Path.Combine(ghWorkspace!, "logs");

                if (TryEnsureWritable(dir))
                    return Path.Combine(dir, logFileName);
            }
        }

        // 4️⃣  Generic container (non-App Service, or if all above failed)
        if (await RuntimeUtil.IsContainer(cancellationToken)
                             .NoSync())
        {
            // Standard Linux container log location (you can tweak if you want a different convention)
            const string localDir = "/var/log/app";

            if (TryEnsureWritable(localDir))
                return Path.Combine(localDir, logFileName);
        }

        // 5️⃣  Local dev fallback – <project root>/logs
        string local = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(local); // always possible locally

        return Path.Combine(local, logFileName);
    }

    private static bool TryEnsureWritable(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}