using System.Collections.Generic;
using System.Text.Json;
using A2Ui.Core.Capabilities;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class ProtocolContractsTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ServerCapabilities_RoundTrip()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["https://a2ui.org/basic"],"acceptsInlineCatalogs":true}}""";

        var caps = JsonSerializer.Deserialize<ServerCapabilities>(json, s_opts)!;

        var single = Assert.Single(caps.V09.SupportedCatalogIds!);
        Assert.Equal("https://a2ui.org/basic", single);
        Assert.True(caps.V09.AcceptsInlineCatalogs);

        string roundTrip = JsonSerializer.Serialize(caps, s_opts);
        Assert.Contains("supportedCatalogIds", roundTrip);
    }

    [Fact]
    public void ServerCapabilities_AcceptsInlineCatalogs_DefaultsFalse()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["test"]}}""";

        var caps = JsonSerializer.Deserialize<ServerCapabilities>(json, s_opts)!;

        Assert.False(caps.V09.AcceptsInlineCatalogs);
    }

    [Fact]
    public void ClientCapabilities_RoundTrip()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["https://a2ui.org/basic","custom:my-catalog"]}}""";

        var caps = JsonSerializer.Deserialize<ClientCapabilities>(json, s_opts)!;

        Assert.Equal(2, caps.V09.SupportedCatalogIds.Length);
        Assert.Null(caps.V09.InlineCatalogs);
    }

    [Fact]
    public void ClientCapabilities_WithInlineCatalogs_PreservesRawJson()
    {
        var json =
            """{"v0.9":{"supportedCatalogIds":["test"],"inlineCatalogs":[{"catalogId":"inline-1","components":{}}]}}""";

        var caps = JsonSerializer.Deserialize<ClientCapabilities>(json, s_opts)!;

        Assert.Single(caps.V09.InlineCatalogs!);
        Assert.Equal("inline-1", caps.V09.InlineCatalogs![0].GetProperty("catalogId").GetString());
    }

    [Fact]
    public void ClientDataModel_RoundTrip()
    {
        var json = """{"version":"v0.9","surfaces":{"main":{"name":"Alice","count":42}}}""";

        var cdm = JsonSerializer.Deserialize<ClientDataModel>(json, s_opts)!;

        Assert.Equal("v0.9", cdm.Version);
        Assert.Contains("main", (IDictionary<string, JsonElement>)cdm.Surfaces);
        Assert.Equal("Alice", cdm.Surfaces["main"].GetProperty("name").GetString());
        Assert.Equal(42, cdm.Surfaces["main"].GetProperty("count").GetInt32());
    }

    [Fact]
    public void ClientDataModel_MultipleSurfaces()
    {
        var json = """{"version":"v0.9","surfaces":{"s1":{"a":1},"s2":{"b":2}}}""";

        var cdm = JsonSerializer.Deserialize<ClientDataModel>(json, s_opts)!;

        Assert.Equal(2, cdm.Surfaces.Count);
    }

    [Fact]
    public void ClientDataModel_DefaultVersion()
    {
        var cdm = new ClientDataModel { Surfaces = new Dictionary<string, JsonElement>() };

        Assert.Equal("v0.9", cdm.Version);
    }
}
