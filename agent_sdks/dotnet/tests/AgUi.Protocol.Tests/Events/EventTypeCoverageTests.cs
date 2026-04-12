using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using AgUi.Protocol.Events;

namespace AgUi.Protocol.Tests.Events;

/// <summary>
/// Guards against drift between <see cref="EventType"/> enum values and
/// <see cref="JsonDerivedTypeAttribute"/> discriminators on <see cref="BaseEvent"/>.
/// </summary>
public sealed class EventTypeCoverageTests
{
    private static HashSet<string> GetJsonDerivedTypeDiscriminators()
    {
        return typeof(BaseEvent)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(a => (string)a.TypeDiscriminator!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string SnakeToPascal(string snake)
    {
        var sb = new StringBuilder();
        foreach (string part in snake.Split('_'))
        {
            if (part.Length == 0)
            {
                continue;
            }

            sb.Append(char.ToUpperInvariant(part[0]));
            sb.Append(part[1..].ToLowerInvariant());
        }

        return sb.ToString();
    }

    private static string PascalToSnake(string pascal)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('_');
            }

            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    [Fact]
    public void AllJsonDerivedTypes_HaveMatchingEnumValue()
    {
        HashSet<string> discriminators = GetJsonDerivedTypeDiscriminators();

        Assert.NotEmpty(discriminators);

        var missing = new List<string>();
        foreach (string discriminator in discriminators)
        {
            string pascal = SnakeToPascal(discriminator);
            if (!Enum.TryParse<EventType>(pascal, out _))
            {
                missing.Add($"{discriminator} -> {pascal}");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"JsonDerivedType discriminators without matching EventType values: {string.Join(", ", missing)}"
        );
    }

    [Fact]
    public void AllEnumValues_HaveMatchingJsonDerivedType()
    {
        HashSet<string> discriminators = GetJsonDerivedTypeDiscriminators();
        EventType[] enumValues = Enum.GetValues<EventType>();

        Assert.NotEmpty(enumValues);

        var missing = new List<string>();
        foreach (EventType value in enumValues)
        {
            string snake = PascalToSnake(value.ToString());
            if (!discriminators.Contains(snake))
            {
                missing.Add($"{value} -> {snake}");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"EventType values without matching JsonDerivedType discriminators: {string.Join(", ", missing)}"
        );
    }
}
