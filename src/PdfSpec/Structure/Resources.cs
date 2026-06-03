using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A page or form-XObject <c>/Resources</c> dictionary (ISO 32000-1 §7.8.3): the
/// named resources visible to the content stream — fonts, XObjects (images,
/// forms), ExtGStates, shadings, patterns, and properties (e.g. OCGs).
/// Insertion order is preserved within each category.
/// </summary>
public sealed class Resources
{
    internal PdfDictionary Dictionary { get; } = new();

    /// <summary>True when nothing has been registered yet.</summary>
    public bool IsEmpty => Dictionary.Entries.Count == 0;

    /// <summary>Register a font under <paramref name="name"/> (selectable via Tf).</summary>
    public void AddFont(string name, PdfReference font) => Add("Font", name, font);

    /// <summary>Register an XObject (image or form) under <paramref name="name"/> (selectable via Do).</summary>
    public void AddXObject(string name, PdfReference xobject) => Add("XObject", name, xobject);

    /// <summary>Register a shading (selectable via sh).</summary>
    public void AddShading(string name, PdfReference shading) => Add("Shading", name, shading);

    /// <summary>Register a pattern (selectable via scn).</summary>
    public void AddPattern(string name, PdfReference pattern) => Add("Pattern", name, pattern);

    /// <summary>Register an ExtGState (invokable via gs).</summary>
    public void AddExtGState(string name, PdfReference extGState) => Add("ExtGState", name, extGState);

    /// <summary>Register a property list (e.g. an OCG for optional content marked-content sequences).</summary>
    public void AddProperty(string name, PdfReference property) => Add("Properties", name, property);

    /// <summary>Generic resource registration (escape hatch).</summary>
    public void Add(string category, string name, PdfObject value)
    {
        if (Dictionary.Get(category) is not PdfDictionary group)
        {
            group = new PdfDictionary();
            Dictionary[category] = group;
        }
        group[name] = value;
    }
}
