using PdfSpec.Objects;

namespace PdfSpec.Layers;

/// <summary>
/// An Optional Content Group (ISO 32000-1 §8.11.2): a named layer that can
/// be toggled in the viewer. State is held directly in the dictionary.
/// </summary>
public sealed class OptionalContentGroup
{
    internal PdfDictionary Dictionary { get; } = new();

    public string Name { get; }

    public OptionalContentGroup(string name, string? intent = null)
    {
        Name = name;
        Dictionary.Add("Type", new PdfName("OCG"));
        Dictionary.Add("Name", new PdfString(name));
        if (intent is not null) Dictionary.Add("Intent", new PdfName(intent));
    }

    public string? Intent
    {
        set => Dictionary.Set("Intent", value is null ? null : new PdfName(value));
    }
}
