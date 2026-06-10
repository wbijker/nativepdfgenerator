using System.Reflection;
using PdfSpec.Fonts;

namespace PdfSpec.Samples;

/// <summary>
/// Loaders for the TTF faces bundled with this assembly. The fonts live
/// in <c>Samples/Fonts/</c> and are embedded as resources by
/// <c>PdfSpec.csproj</c>; <see cref="LoadEmbedded"/> reads the bytes
/// from the manifest and hands them to <see cref="TrueTypeFont.FromBytes"/>.
/// </summary>
internal static class SampleFonts
{
    /// <summary>Painting with Chocolate — a thick, hand-drawn display face.</summary>
    public static TrueTypeFont PaintingWithChocolate() => LoadEmbedded("Paintingwithchocolate-K5mo.ttf");

    /// <summary>Quake3d — the angular display face from the game's title screen.</summary>
    public static TrueTypeFont Quake3d() => LoadEmbedded("Quake3d.ttf");

    private static TrueTypeFont LoadEmbedded(string fileName)
    {
        var asm = typeof(SampleFonts).Assembly;
        string resourceName = $"PdfSpec.Samples.Fonts.{fileName}";
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded font resource not found: {resourceName}", fileName);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return TrueTypeFont.FromBytes(ms.ToArray());
    }
}
