using System.Collections.Generic;
using System.Text.Json;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace A2Ui.Core.Tests;

public sealed class ProtocolContractsTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ServerCapabilities_RoundTrip()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["https://a2ui.org/basic"],"acceptsInlineCatalogs":true}}""";

        var caps = JsonSerializer.Deserialize<ServerCapabilities>(json, s_opts)!;

        caps.V09.SupportedCatalogIds.Should().ContainSingle().Which.Should().Be("https://a2ui.org/basic");
        caps.V09.AcceptsInlineCatalogs.Should().BeTrue();

        string roundTrip = JsonSerializer.Serialize(caps, s_opts);
        roundTrip.Should().Contain("supportedCatalogIds");
    }

    [Fact]
    public void ServerCapabilities_AcceptsInlineCatalogs_DefaultsFalse()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["test"]}}""";

        var caps = JsonSerializer.Deserialize<ServerCapabilities>(json, s_opts)!;

        caps.V09.AcceptsInlineCatalogs.Should().BeFalse();
    }

    [Fact]
    public void ClientCapabilities_RoundTrip()
    {
        var json = """{"v0.9":{"supportedCatalogIds":["https://a2ui.org/basic","custom:my-catalog"]}}""";

        var caps = JsonSerializer.Deserialize<ClientCapabilities>(json, s_opts)!;

        caps.V09.SupportedCatalogIds.Should().HaveCount(2);
        caps.V09.InlineCatalogs.Should().BeNull();
    }

    [Fact]
    public void ClientCapabilities_WithInlineCatalogs_PreservesRawJson()
    {
        var json =
            """{"v0.9":{"supportedCatalogIds":["test"],"inlineCatalogs":[{"catalogId":"inline-1","components":{}}]}}""";

        var caps = JsonSerializer.Deserialize<ClientCapabilities>(json, s_opts)!;

        caps.V09.InlineCatalogs.Should().HaveCount(1);
        caps.V09.InlineCatalogs![0].GetProperty("catalogId").GetString().Should().Be("inline-1");
    }

    [Fact]
    public void ClientDataModel_RoundTrip()
    {
        var json = """{"version":"v0.9","surfaces":{"main":{"name":"Alice","count":42}}}""";

        var cdm = JsonSerializer.Deserialize<ClientDataModel>(json, s_opts)!;

        cdm.Version.Should().Be("v0.9");
        cdm.Surfaces.Should().ContainKey("main");
        cdm.Surfaces["main"].GetProperty("name").GetString().Should().Be("Alice");
        cdm.Surfaces["main"].GetProperty("count").GetInt32().Should().Be(42);
    }

    [Fact]
    public void ClientDataModel_MultipleSurfaces()
    {
        var json = """{"version":"v0.9","surfaces":{"s1":{"a":1},"s2":{"b":2}}}""";

        var cdm = JsonSerializer.Deserialize<ClientDataModel>(json, s_opts)!;

        cdm.Surfaces.Should().HaveCount(2);
    }

    [Fact]
    public void ClientDataModel_DefaultVersion()
    {
        var cdm = new ClientDataModel { Surfaces = new Dictionary<string, JsonElement>() };

        cdm.Version.Should().Be("v0.9");
    }
}
