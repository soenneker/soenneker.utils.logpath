using Soenneker.Tests.HostedUnit;

namespace Soenneker.Utils.LogPath.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class LogPathUtilTests : HostedUnitTest
{
    public LogPathUtilTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
