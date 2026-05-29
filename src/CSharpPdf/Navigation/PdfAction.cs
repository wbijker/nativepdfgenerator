using CSharpPdf.Objects;

namespace CSharpPdf.Navigation;

/// <summary>
/// Builds action dictionaries (Chapter 5, "Actions"). Every action has an S key
/// declaring its type; the remaining keys depend on the type. Actions can be
/// chained with <see cref="Then"/> (the Next key).
/// </summary>
public static class PdfAction
{
    /// <summary>GoTo — jump to an explicit destination within this document.</summary>
    public static PdfDictionary GoTo(PdfArray destination) =>
        Base("GoTo", ("D", destination));

    /// <summary>GoTo — jump to a named destination (resolved via the Dests name tree).</summary>
    public static PdfDictionary GoToNamed(string name) =>
        Base("GoTo", ("D", new PdfString(name)));

    /// <summary>URI — open a uniform resource identifier (e.g. a web link).</summary>
    public static PdfDictionary Uri(string uri) =>
        Base("URI", ("URI", new PdfString(uri)));

    /// <summary>
    /// GoToR — "remote go-to": open another PDF and jump to a zero-based page
    /// index (note: GoToR destinations use page numbers, not references).
    /// </summary>
    public static PdfDictionary GoToRemote(string path, int pageIndex, string zoom = "Fit") =>
        Base("GoToR",
            ("F", Filespec(path)),
            ("D", new PdfArray(new PdfNumber(pageIndex), new PdfName(zoom))));

    /// <summary>Launch — open a non-PDF document with the OS-associated application.</summary>
    public static PdfDictionary Launch(string path) =>
        Base("Launch", ("F", Filespec(path)));

    /// <summary>
    /// GoToE — "embedded go-to": jump into an embedded PDF named in the
    /// EmbeddedFiles name tree (Chapter 8), at a zero-based page index.
    /// </summary>
    public static PdfDictionary GoToEmbedded(string targetName, int pageIndex = 0, string zoom = "Fit")
    {
        var a = Base("GoToE", ("D", new PdfArray(new PdfNumber(pageIndex), new PdfName(zoom))));
        a["T"] = new PdfDictionary { ["R"] = new PdfName("C"), ["N"] = new PdfString(targetName) };
        return a;
    }

    /// <summary>SubmitForm — send field values to a URL (Chapter 7, "Form Actions").</summary>
    public static PdfDictionary SubmitForm(string url) =>
        Base("SubmitForm", ("F", Filespec(url)));

    /// <summary>ResetForm — reset fields to their default values.</summary>
    public static PdfDictionary ResetForm() =>
        Base("ResetForm");

    /// <summary>ImportData — import field data from an FDF file.</summary>
    public static PdfDictionary ImportData(string path) =>
        Base("ImportData", ("F", Filespec(path)));

    /// <summary>Sound — play a sound stream (Chapter 9, legacy).</summary>
    public static PdfDictionary PlaySound(PdfReference sound) =>
        Base("Sound", ("Sound", sound));

    /// <summary>Rendition — control multimedia playback through a screen annotation.</summary>
    public static PdfDictionary Rendition(PdfReference screenAnnotation, PdfReference rendition, int operation = 0) =>
        Base("Rendition", ("AN", screenAnnotation), ("R", rendition), ("OP", new PdfNumber(operation)));

    /// <summary>Chain a follow-on action (or array of actions) via the Next key.</summary>
    public static PdfDictionary Then(this PdfDictionary action, PdfObject next)
    {
        action["Next"] = next;
        return action;
    }

    /// <summary>A file specification dictionary, writing both F and UF for compatibility.</summary>
    public static PdfDictionary Filespec(string path) => new()
    {
        ["Type"] = new PdfName("Filespec"),
        ["F"] = new PdfString(path),
        ["UF"] = new PdfString(path),
    };

    private static PdfDictionary Base(string type, params (string Key, PdfObject Value)[] entries)
    {
        var action = new PdfDictionary
        {
            ["Type"] = new PdfName("Action"),
            ["S"] = new PdfName(type),
        };
        foreach (var (key, value) in entries)
        {
            action[key] = value;
        }
        return action;
    }
}
