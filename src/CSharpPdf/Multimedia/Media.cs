using CSharpPdf.Geometry;
using CSharpPdf.Navigation;
using CSharpPdf.Objects;

namespace CSharpPdf.Multimedia;

/// <summary>
/// Factories for multimedia and 3D constructs (Chapter 9): the legacy sound and
/// movie annotations, the modern screen annotation with its rendition action,
/// and 3D annotations with views. Media data itself is opaque to this library;
/// these build the dictionaries that reference it.
/// </summary>
public static class Media
{
    // ----- Simple media: sound -----

    /// <summary>A sound stream: sample data plus rate (R), channels (C), bits (B), encoding (E).</summary>
    public static PdfStream SoundStream(byte[] samples, int sampleRate, int channels = 1, int bits = 8, string encoding = "Raw")
    {
        var stream = new PdfStream(samples);
        var d = stream.Dictionary;
        d["Type"] = new PdfName("Sound");
        d["R"] = new PdfNumber(sampleRate);
        d["C"] = new PdfNumber(channels);
        d["B"] = new PdfNumber(bits);
        d["E"] = new PdfName(encoding); // Raw, Signed, muLaw, ALaw
        return stream;
    }

    /// <summary>A sound annotation that plays <paramref name="sound"/> when activated.</summary>
    public static PdfDictionary SoundAnnotation(PdfRectangle rect, PdfReference sound, string contents, string icon = "Speaker")
    {
        var a = Base("Sound", rect);
        a["Sound"] = sound;
        a["Contents"] = new PdfString(contents);
        a["Name"] = new PdfName(icon); // Speaker or Mic
        return a;
    }

    // ----- Simple media: movie -----

    /// <summary>A (legacy) movie annotation referencing a movie file by specification.</summary>
    public static PdfDictionary MovieAnnotation(PdfRectangle rect, string fileName,
        double[]? aspect = null, bool showControls = true, string title = "")
    {
        var movie = new PdfDictionary { ["F"] = PdfAction.Filespec(fileName) };
        if (aspect is not null)
        {
            movie["Aspect"] = new PdfArray(new PdfNumber(aspect[0]), new PdfNumber(aspect[1]));
        }

        var a = Base("Movie", rect);
        a["Movie"] = movie;
        a["A"] = new PdfDictionary { ["ShowControls"] = new PdfBoolean(showControls) };
        a["Border"] = new PdfArray(new PdfNumber(0), new PdfNumber(0), new PdfNumber(1));
        if (title.Length > 0)
        {
            a["T"] = new PdfString(title);
        }
        return a;
    }

    internal static PdfDictionary Base(string subtype, PdfRectangle rect) => new()
    {
        ["Type"] = new PdfName("Annot"),
        ["Subtype"] = new PdfName(subtype),
        ["Rect"] = rect.ToArray(),
    };
}
