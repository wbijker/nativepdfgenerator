using ImperativeElement = PdfSpec.Layout.Element;

namespace PdfSpec.Fluent;

/// <summary>
/// Base of the fluent builder layer. Each fluent type wraps an
/// imperative <see cref="ImperativeElement"/> instance and exposes
/// chainable setters that mutate the wrapped state in place. The fluent
/// hierarchy stays separate from the imperative
/// <see cref="ImperativeElement"/> tree — no inheritance, no partial
/// classes, no name collisions between the two layers — so a file picks
/// one API and never mixes.
///
/// <para>
/// The factory entry points (<c>Element.Paragraph(...)</c>,
/// <c>Element.VStack(...)</c>, <c>Element.Container(...)</c>, …) live on
/// the imperative <see cref="ImperativeElement"/> so callers reach them
/// via a single <c>Element.X(...)</c> call regardless of which layer the
/// surrounding code is using. They return the fluent wrapper types
/// defined here.
/// </para>
///
/// <para>
/// <see cref="Build"/> is internal — the assembly translates a fluent
/// tree to the underlying imperative tree at the document boundary
/// (<see cref="PdfDoc"/> / <see cref="PdfPage"/>), so callers never need
/// to see it.
/// </para>
/// </summary>
public abstract class Element
{
    /// <summary>Hand back the underlying imperative <see cref="ImperativeElement"/> this fluent builder describes.</summary>
    internal abstract ImperativeElement Build();
}
