using PdfSpec.Objects;

namespace PdfSpec.Actions;

/// <summary>
/// A GoTo action (ISO 32000-1 §12.6.4.2) — jumps to an explicit
/// <see cref="Destination"/> within the same document.
/// </summary>
public sealed class GoToAction : PdfAction
{
    public Destination Destination { get; }

    public GoToAction(Destination destination) => Destination = destination;

    public override PdfDictionary Build()
    {
        var d = Base("GoTo");
        d.Add("D", Destination.Build());
        return d;
    }
}
