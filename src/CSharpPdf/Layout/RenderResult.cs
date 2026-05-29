namespace CSharpPdf.Layout;

/// <summary>The outcome of rendering a component into the space it was given.</summary>
public enum RenderStatus
{
    /// <summary>Nothing fit; the caller should make more room (e.g. a new page) and retry.</summary>
    Empty,

    /// <summary>The component rendered completely.</summary>
    Full,

    /// <summary>Part of the component rendered; <see cref="RenderResult.Remainder"/> holds the rest.</summary>
    Partial,
}

/// <summary>
/// What a <see cref="Component"/> produced: how much space it consumed, and — when
/// it could not finish — the remaining work as a new component for the next page.
/// </summary>
public sealed record RenderResult(RenderStatus Status, Size Used, Component? Remainder = null)
{
    public static readonly RenderResult Empty = new(RenderStatus.Empty, Size.Zero);

    public static RenderResult Full(Size used) => new(RenderStatus.Full, used);

    public static RenderResult Partial(Size used, Component remainder) =>
        new(RenderStatus.Partial, used, remainder);
}
