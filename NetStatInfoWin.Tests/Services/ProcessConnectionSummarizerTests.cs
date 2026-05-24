using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;
using NetStatInfoWin.Services;

namespace NetStatInfoWin.Tests.Services;

[TestClass]
public sealed class ProcessConnectionSummarizerTests
{
    [TestMethod]
    public void Summarize_MultipleConnectionsForOnePid_CollapsesIntoSingleRow()
    {
        var summarizer = new ProcessConnectionSummarizer(new FakeMetadataService(), new FakeResourceService());

        IReadOnlyList<ProcessConnectionSummary> results = summarizer.Summarize(
        [
            new OwnedConnectionRecord(101, ConnectionProtocol.Tcp, "127.0.0.1:5000", "8.8.8.8:443"),
            new OwnedConnectionRecord(101, ConnectionProtocol.Udp, "0.0.0.0:5353", null),
        ]);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Browser", results[0].DisplayName);
        Assert.AreEqual("TCP 1 · UDP 1", results[0].ProtocolMix);
        Assert.AreEqual(2, results[0].ActiveConnectionCount);
    }

    [TestMethod]
    public void Summarize_MetadataFailure_UsesFallbackProcessName()
    {
        var summarizer = new ProcessConnectionSummarizer(new ThrowingMetadataService(), new FakeResourceService());

        IReadOnlyList<ProcessConnectionSummary> results = summarizer.Summarize(
        [
            new OwnedConnectionRecord(222, ConnectionProtocol.Tcp, "127.0.0.1:5000", "1.1.1.1:53"),
        ]);

        Assert.AreEqual("Process 222", results[0].DisplayName);
        Assert.AreEqual("P2", results[0].Initials);
    }

    [TestMethod]
    public void Summarize_SortsByConnectionCountThenName()
    {
        var summarizer = new ProcessConnectionSummarizer(new FakeMetadataService(), new FakeResourceService());

        IReadOnlyList<ProcessConnectionSummary> results = summarizer.Summarize(
        [
            new OwnedConnectionRecord(101, ConnectionProtocol.Tcp, "127.0.0.1:5000", "8.8.8.8:443"),
            new OwnedConnectionRecord(101, ConnectionProtocol.Tcp, "127.0.0.1:5001", "8.8.4.4:443"),
            new OwnedConnectionRecord(303, ConnectionProtocol.Tcp, "127.0.0.1:6000", "4.4.4.4:443"),
            new OwnedConnectionRecord(404, ConnectionProtocol.Tcp, "127.0.0.1:6001", "9.9.9.9:443"),
        ]);

        CollectionAssert.AreEqual(
            new[] { "Browser", "Alpha", "Reader" },
            results.Select(item => item.DisplayName).ToArray());
    }

    private sealed class FakeMetadataService : IProcessMetadataService
    {
        public ProcessMetadata GetProcessMetadata(int processId)
        {
            return processId switch
            {
                101 => new ProcessMetadata("Browser", "BR"),
                303 => new ProcessMetadata("Reader", "RE"),
                404 => new ProcessMetadata("Alpha", "AL"),
                _ => new ProcessMetadata($"Process {processId}", "P"),
            };
        }
    }

    private sealed class ThrowingMetadataService : IProcessMetadataService
    {
        public ProcessMetadata GetProcessMetadata(int processId)
        {
            throw new InvalidOperationException("unavailable");
        }
    }

    private sealed class FakeResourceService : IResourceService
    {
        public string Format(string key, params object[] arguments)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, GetString(key), arguments);
        }

        public string GetString(string key)
        {
            return key switch
            {
                "ProcessConnectionCountFormat" => "{0} connections",
                "ProcessIdentifierFormat" => "PID {0}",
                "ProcessNoEndpointSummary" => "Waiting for endpoint details",
                "ProcessListeningSingleFormat" => "Listening on {0}",
                "ProcessListeningMultipleFormat" => "Listening on {0} local endpoints",
                "ProcessSingleRemoteFormat" => "Connected to {0}",
                "ProcessMultipleRemoteFormat" => "{0} remote endpoints · first {1}",
                "ProcessRemoteAndListenerFormat" => "{0} remote endpoints · {1} listeners",
                "ProcessFallbackNameFormat" => "Process {0}",
                "ProtocolTcp" => "TCP",
                "ProtocolUdp" => "UDP",
                _ => key,
            };
        }
    }
}
