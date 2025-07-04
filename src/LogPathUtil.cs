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
    public static async ValueTask<string> Get(string logFileName, CancellationToken cancellationToken = default)
    {
        // 1️⃣  Caller-supplied override
        string? overrideDir = Environment.GetEnvironmentVariable("LOG_PATH");

        if (overrideDir.HasContent())
            return Path.Combine(overrideDir, logFileName);

        // 2️⃣  Azure App Service (Win & Linux) – %HOME% is always set
        if (RuntimeUtil.IsAzureAppService)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");

            if (home.HasContent())
            {
                string dir = Path.Combine(home, "LogFiles");

                if (TryEnsureWritable(dir))
                    return Path.Combine(dir, logFileName);
            }
        }

        // 3️⃣  GitHub Actions (runner has guaranteed write access)
        if (RuntimeUtil.IsGitHubAction)
        {
            string? ghWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

            if (ghWorkspace.HasContent())
            {
                string dir = Path.Combine(ghWorkspace, "logs");

                if (TryEnsureWritable(dir))
                    return Path.Combine(dir, logFileName);
            }
        }

        // Generic container (not WAFC)
        if (await RuntimeUtil.IsContainer(cancellationToken).NoSync())
        {
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