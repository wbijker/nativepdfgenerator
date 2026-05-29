namespace CSharpPdf.Layout;

/// <summary>
/// A layout component. It advertises its intrinsic sizes (a floor and a natural
/// "grow-to" size, both hints the engine uses to allocate space) and renders into
/// the space the engine gives it, reporting how much it used and — if it could not
/// finish — the remaining work.
/// </summary>
public abstract class Component
{
    /// <summary>
    /// The smallest space the component can render in (its floor): e.g. for text,
    /// the longest unbreakable word wide, and one line tall. Used as a lower bound
    /// and for break decisions (don't start if less than this remains).
    /// </summary>
    public abstract Size MinimalSpaceRequired { get; }

    /// <summary>
    /// The natural size the component would take given unlimited room — what an
    /// auto-sized container should let it "grow to".
    /// </summary>
    public abstract Size PreferredSize { get; }

    /// <summary>
    /// Draw into <paramref name="available"/> at the context's region and report
    /// the outcome (full / partial-with-remainder / didn't-fit).
    /// </summary>
    public abstract RenderResult Render(RenderContext context, Size available);
}
