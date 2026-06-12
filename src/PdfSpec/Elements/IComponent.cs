namespace PdfSpec.Elements;

/// <summary>
/// A reusable composition unit — a class that populates a slot via
/// <see cref="Compose"/>. Encapsulates a piece of the document (a
/// header, a card, a list of verses) so a page can hand off a slot to
/// the component instead of inlining the layout.
///
/// <para>
/// Invoke through <see cref="IContainer.Component"/>; the container
/// passes itself, the component does its work via the same fluent
/// surface — chrome setters, content terminals, nested rows/columns
/// — exactly as if the page were composing inline.
/// </para>
/// </summary>
public interface IComponent
{
    /// <summary>Populate <paramref name="container"/> — the component's host slot.</summary>
    void Compose(IContainer container);
}
