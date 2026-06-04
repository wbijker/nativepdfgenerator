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
        Dictionary.SetName("Type", "OCG");
        Dictionary.SetString("Name", name);
        Dictionary.SetName("Intent", intent);
    }

    public string? Intent { set => Dictionary.SetName("Intent", value); }
}
