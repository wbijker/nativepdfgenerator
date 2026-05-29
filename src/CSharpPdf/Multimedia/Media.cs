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

    // ----- Modern multimedia: screen annotation + rendition -----

    /// <summary>
    /// A screen annotation: the page region where media plays. Bind a rendition
    /// action via its A key (see <see cref="PdfAction.Rendition"/>).
    /// </summary>
    public static PdfDictionary ScreenAnnotation(PdfRectangle rect, string title, double[]? borderColor = null)
    {
        var a = Base("Screen", rect);
        a["T"] = new PdfString(title);
        a["F"] = new PdfNumber(4);
        if (borderColor is not null)
        {
            a["MK"] = new PdfDictionary { ["BC"] = ToArray(borderColor) };
            a["BS"] = new PdfDictionary { ["Type"] = new PdfName("Border"), ["W"] = new PdfNumber(1), ["S"] = new PdfName("S") };
        }
        return a;
    }

    /// <summary>
    /// A media rendition (S=MR): what to play (media clip, by MIME type + URL),
    /// how (controls / repeat), and where (on the page).
    /// </summary>
    public static PdfDictionary MediaRendition(string mimeType, string url, bool controls = true, int repeatCount = 1)
    {
        var clip = new PdfDictionary
        {
            ["Type"] = new PdfName("MediaClip"),
            ["S"] = new PdfName("MCD"),
            ["CT"] = new PdfString(mimeType),
            ["D"] = new PdfDictionary
            {
                ["Type"] = new PdfName("Filespec"),
                ["FS"] = new PdfName("URL"),
                ["F"] = new PdfString(url),
            },
        };
        return new PdfDictionary
        {
            ["S"] = new PdfName("MR"),
            ["C"] = clip,
            ["P"] = new PdfDictionary { ["BE"] = new PdfDictionary { ["C"] = new PdfBoolean(controls), ["RC"] = new PdfNumber(repeatCount) } },
            ["SP"] = new PdfDictionary { ["BE"] = new PdfDictionary { ["W"] = new PdfNumber(0) } }, // 0 = play on the page
        };
    }

    // ----- 3D -----

    /// <summary>A 3D view dictionary positioning the camera via a 12-element C2W matrix.</summary>
    public static PdfDictionary ThreeDView(string name, double[] cameraToWorld)
    {
        var view = new PdfDictionary
        {
            ["Type"] = new PdfName("3DView"),
            ["XN"] = new PdfString(name),
            ["MS"] = new PdfName("M"),
            ["C2W"] = ToArray(cameraToWorld),
        };
        return view;
    }

    /// <summary>A 3D stream (U3D or PRC data) with optional views and a default view index.</summary>
    public static PdfStream ThreeDStream(byte[] data, string format = "U3D", PdfArray? views = null, int defaultView = 0)
    {
        var stream = new PdfStream(data);
        stream.Dictionary["Type"] = new PdfName("3D");
        stream.Dictionary["Subtype"] = new PdfName(format);
        if (views is not null)
        {
            stream.Dictionary["VA"] = views;
            stream.Dictionary["DV"] = new PdfNumber(defaultView);
        }
        return stream;
    }

    /// <summary>
    /// A 3D annotation: references the 3D data (3DD) and provides a fallback/poster
    /// appearance (AP/N) for viewers that don't support 3D.
    /// </summary>
    public static PdfDictionary ThreeDAnnotation(PdfRectangle rect, PdfReference threeDData, PdfReference appearance, string contents)
    {
        var a = Base("3D", rect);
        a["3DD"] = threeDData;
        a["AP"] = new PdfDictionary { ["N"] = appearance };
        a["Contents"] = new PdfString(contents);
        return a;
    }

    private static PdfArray ToArray(double[] values)
    {
        var array = new PdfArray();
        foreach (double v in values)
        {
            array.Add(new PdfNumber(v));
        }
        return array;
    }

    internal static PdfDictionary Base(string subtype, PdfRectangle rect) => new()
    {
        ["Type"] = new PdfName("Annot"),
        ["Subtype"] = new PdfName(subtype),
        ["Rect"] = rect.ToArray(),
    };
}
