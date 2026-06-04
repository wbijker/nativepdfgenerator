using PdfSpec.Objects;

namespace PdfSpec.Layers;

/// <summary>
/// An Optional Content Group (ISO 32000-1 §8.11.2): a named layer that can
/// be toggled in the viewer. State is held directly in the dictionary.
/// </summary>
public sealed class OptionalContentGroup
{
    public PdfDictionary Dictionary { get; } = new();

    public string Name { get; }

    public OptionalContentGroup(string name, OptionalContentIntent? intent = null)
    {
        Name = name;
        Dictionary.SetName("Type", "OCG");
        Dictionary.SetString("Name", name);
        Dictionary.SetName("Intent", intent?.ToString());
    }

    public OptionalContentIntent? Intent { set => Dictionary.SetName("Intent", value?.ToString()); }
}

/// <summary>
/// Optional Content Group <c>/Intent</c> entry (ISO 32000-1 §8.11.2.1 Table
/// 96). <c>View</c> means the group affects screen rendering; <c>Design</c>
/// means it affects only the design-time / editing view of the document.
/// Enum names match the PDF name objects emitted to the file.
/// </summary>
public enum OptionalContentIntent
{
    View,
    Design,
}
