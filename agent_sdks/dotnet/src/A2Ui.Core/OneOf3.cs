#pragma warning disable SA1649 // File name should match first type name — OneOf with 3 type params shares the name with the 2-arity overload in OneOf.cs

namespace A2Ui.Core;

/// <summary>
/// A discriminated union that holds exactly one of three possible types.
/// </summary>
/// <typeparam name="T1">The first possible type.</typeparam>
/// <typeparam name="T2">The second possible type.</typeparam>
/// <typeparam name="T3">The third possible type.</typeparam>
public abstract record OneOf<T1, T2, T3>
{
    private OneOf() { }

    /// <summary>Wraps a value of type <typeparamref name="T1"/>.</summary>
    public sealed record Case1(T1 Value) : OneOf<T1, T2, T3>;

    /// <summary>Wraps a value of type <typeparamref name="T2"/>.</summary>
    public sealed record Case2(T2 Value) : OneOf<T1, T2, T3>;

    /// <summary>Wraps a value of type <typeparamref name="T3"/>.</summary>
    public sealed record Case3(T3 Value) : OneOf<T1, T2, T3>;

    /// <summary>Returns <see langword="true"/> when this instance holds a <typeparamref name="T1"/> value.</summary>
    public bool IsT1 => this is Case1;

    /// <summary>Returns <see langword="true"/> when this instance holds a <typeparamref name="T2"/> value.</summary>
    public bool IsT2 => this is Case2;

    /// <summary>Returns <see langword="true"/> when this instance holds a <typeparamref name="T3"/> value.</summary>
    public bool IsT3 => this is Case3;

    /// <summary>
    /// Exhaustively matches the union, calling the appropriate function and returning its result.
    /// </summary>
    public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2, Func<T3, T> onT3) =>
        this switch
        {
            Case1 c1 => onT1(c1.Value),
            Case2 c2 => onT2(c2.Value),
            Case3 c3 => onT3(c3.Value),
            _ => throw new InvalidOperationException("Unreachable"),
        };

    /// <summary>
    /// Exhaustively matches the union, calling the appropriate action.
    /// </summary>
    public void Switch(Action<T1> onT1, Action<T2> onT2, Action<T3> onT3)
    {
        switch (this)
        {
            case Case1 c1:
                onT1(c1.Value);
                break;
            case Case2 c2:
                onT2(c2.Value);
                break;
            case Case3 c3:
                onT3(c3.Value);
                break;
            default:
                throw new InvalidOperationException("Unreachable");
        }
    }

    /// <summary>
    /// Attempts to extract the <typeparamref name="T1"/> value.
    /// </summary>
    /// <returns><see langword="true"/> when this instance is <see cref="Case1"/>.</returns>
    public bool TryGetAsT1(out T1? value)
    {
        if (this is Case1 c1)
        {
            value = c1.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to extract the <typeparamref name="T2"/> value.
    /// </summary>
    /// <returns><see langword="true"/> when this instance is <see cref="Case2"/>.</returns>
    public bool TryGetAsT2(out T2? value)
    {
        if (this is Case2 c2)
        {
            value = c2.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to extract the <typeparamref name="T3"/> value.
    /// </summary>
    /// <returns><see langword="true"/> when this instance is <see cref="Case3"/>.</returns>
    public bool TryGetAsT3(out T3? value)
    {
        if (this is Case3 c3)
        {
            value = c3.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>Implicitly wraps a <typeparamref name="T1"/> value.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T1 value) => new Case1(value);

    /// <summary>Implicitly wraps a <typeparamref name="T2"/> value.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T2 value) => new Case2(value);

    /// <summary>Implicitly wraps a <typeparamref name="T3"/> value.</summary>
    public static implicit operator OneOf<T1, T2, T3>(T3 value) => new Case3(value);
}
