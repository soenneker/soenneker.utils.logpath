[![](https://img.shields.io/nuget/v/soenneker.utils.logpath.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.logpath/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.logpath/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.logpath/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.logpath.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.logpath/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.logpath/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.logpath/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.LogPath
A utility library for determining the log path across all environments.

## Installation

```bash
dotnet add package Soenneker.Utils.LogPath
```

## Quick start

```csharp
using Soenneker.Utils.LogPath;
```

Call the static `LogPathUtil` methods directly; no dependency-injection registration is required.

## Usage

```csharp
string logPath = await LogPathUtil.Get("application-.log", cancellationToken);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

`Get` returns a full path for the supplied file name. It does not create or open the log file.
Pass a file name rather than a rooted path or a path containing directory traversal segments.

Candidates are evaluated in this order:

1. `LOG_PATH/<fileName>` when the `LOG_PATH` environment variable is set.
2. Azure App Service's persistent `LogFiles` directory.
3. `<GITHUB_WORKSPACE>/logs` in GitHub Actions.
4. `/var/log/app` in another detected container.
5. `<AppContext.BaseDirectory>/logs` as the fallback.

The explicit `LOG_PATH` override is returned as configured; the caller is responsible for making
sure that directory exists and is writable. For the environment-derived candidates, the utility
creates the directory and falls through when creation is denied or fails with an I/O error. The
final application-directory fallback can still throw if that location cannot be created.

Container detection is the asynchronous part of the operation and observes the cancellation
token. Resolving a path does not guarantee a later file open will succeed because permissions or
the filesystem can change afterward.
