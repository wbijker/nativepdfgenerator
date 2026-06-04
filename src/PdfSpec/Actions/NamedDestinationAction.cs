using PdfSpec.Objects;

namespace PdfSpec.Actions;

/// <summary>
/// A GoTo action targeting a destination registered in the document's
/// <c>/Dests</c> name tree (ISO 32000-1 §12.3.2.3). Resolved by the viewer
/// at click time.
/// </summary>
public sealed class NamedDestinationAction : PdfAction
{
    public string DestinationName { get; }

    public NamedDestinationAction(string destinationName) => DestinationName = destinationName;

    public override PdfDictionary Build()
    {
        var d = Base("GoTo");
        d.SetString("D", DestinationName);
        return d;
    }
}
