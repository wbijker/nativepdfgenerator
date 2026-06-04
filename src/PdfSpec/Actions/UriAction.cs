using PdfSpec.Objects;

namespace PdfSpec.Actions;

/// <summary>
/// A URI action (ISO 32000-1 §12.6.4.7) — opens a URL in an external viewer.
/// </summary>
public sealed class UriAction : PdfAction
{
    public string Url { get; }

    public UriAction(string url) => Url = url;

    public override PdfDictionary Build()
    {
        var d = Base("URI");
        d.Add("URI", new PdfString(Url));
        return d;
    }
}
