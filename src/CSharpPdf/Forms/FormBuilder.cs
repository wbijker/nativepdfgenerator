using CSharpPdf.Content;
using CSharpPdf.Geometry;
using CSharpPdf.Objects;
using CSharpPdf.Text;

namespace CSharpPdf.Forms;

/// <summary>
/// Builds AcroForm fields (Chapter 7). Each visible field is a merged field +
/// widget annotation: it carries field keys (FT, T, V, Ff, ...) and annotation
/// keys (Subtype /Widget, Rect, AP). Appearance streams are generated as form
/// XObjects so the fields render in any viewer. The builder shares one Helvetica
/// font via the form's default resources (DR).
/// </summary>
public sealed class FormBuilder
{
    // Field flag bit values (1-based bit n == 1 << (n-1)).
    private const int FlagPushButton = 1 << 16; // bit 17
    private const int FlagRadio = 1 << 15;      // bit 16
    private const int FlagNoToggleToOff = 1 << 14; // bit 15
    private const int FlagCombo = 1 << 17;      // bit 18
    private const int FlagEdit = 1 << 18;       // bit 19
    private const int FlagMultiline = 1 << 12;  // bit 13

    private const int AnnotationPrint = 4; // annotation F flag, bit 3

    private readonly PdfDocument _doc;
    private readonly PdfReference _fontRef;

    public FormBuilder(PdfDocument doc)
    {
        _doc = doc;
        _fontRef = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica, StandardFonts.WinAnsiEncoding));

        // Default resources (DR) and default appearance (DA) shared by all fields.
        var fonts = new PdfDictionary { ["Helv"] = _fontRef };
        _doc.AcroForm["DR"] = new PdfDictionary { ["Font"] = fonts };
        _doc.AcroForm["DA"] = new PdfString("/Helv 0 Tf 0 g");
        _doc.AcroForm["NeedAppearances"] = new PdfBoolean(false);
    }

    /// <summary>A single-line (or multi-line) text entry field.</summary>
    public void TextField(PdfPage page, string name, PdfRectangle rect, string value,
        double fontSize = 12, bool multiline = false)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        DrawBorder(appearance, w, h);

        // Draw the value, breaking the appearance into lines for multiline fields.
        string[] lines = multiline ? value.Split('\n') : new[] { value };
        var text = appearance.Content.SetRgbFill(0, 0, 0).BeginText()
            .SetFont("Helv", fontSize).SetLeading(fontSize + 2)
            .SetTextMatrix(1, 0, 0, 1, 4, h - fontSize - 3);
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) text.NextLine();
            text.ShowText(lines[i]);
        }
        text.EndText();

        var field = Widget("Tx", name, rect);
        field["V"] = new PdfString(value);
        field["DA"] = new PdfString($"/Helv {fontSize} Tf 0 g");
        if (multiline) field["Ff"] = new PdfNumber(FlagMultiline);
        field["AP"] = new PdfDictionary { ["N"] = Build(appearance) };
        Register(page, field);
    }

    /// <summary>A checkbox toggling between the Yes and Off states.</summary>
    public void CheckBox(PdfPage page, string name, PdfRectangle rect, bool isChecked)
    {
        var on = NewAppearance(rect, out double w, out double h);
        DrawBorder(on, w, h);
        on.Content.SetRgbStroke(0, 0, 0).SetLineWidth(2)
            .MoveTo(w * 0.2, h * 0.5).LineTo(w * 0.42, h * 0.25).LineTo(w * 0.8, h * 0.8).Stroke();

        var off = NewAppearance(rect, out _, out _);
        DrawBorder(off, w, h);

        var field = Widget("Btn", name, rect);
        field["V"] = new PdfName(isChecked ? "Yes" : "Off");
        field["AS"] = new PdfName(isChecked ? "Yes" : "Off");
        field["AP"] = new PdfDictionary
        {
            ["N"] = new PdfDictionary { ["Yes"] = Build(on), ["Off"] = Build(off) },
        };
        Register(page, field);
    }

    /// <summary>A push button that triggers an action when clicked.</summary>
    public void PushButton(PdfPage page, string name, PdfRectangle rect, string caption, PdfDictionary action)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        appearance.Content.SetRgbFill(0.86, 0.86, 0.92).Rectangle(0, 0, w, h).Fill();
        DrawBorder(appearance, w, h);
        double fontSize = 12;
        double textWidth = caption.Length * fontSize * 0.5;
        appearance.Content.SetRgbFill(0, 0, 0).BeginText().SetFont("Helv", fontSize)
            .SetTextMatrix(1, 0, 0, 1, Math.Max(4, (w - textWidth) / 2), (h - fontSize) / 2 + 1)
            .ShowText(caption).EndText();

        var field = Widget("Btn", name, rect);
        field["Ff"] = new PdfNumber(FlagPushButton);
        field["AP"] = new PdfDictionary { ["N"] = Build(appearance) };
        field["A"] = action;
        Register(page, field);
    }

    /// <summary>A drop-down choice field (combo box), optionally user-editable.</summary>
    public void ComboBox(PdfPage page, string name, PdfRectangle rect, string[] options, string value, bool editable = false)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        DrawBorder(appearance, w, h);
        appearance.Content.SetRgbFill(0, 0, 0).BeginText().SetFont("Helv", 12)
            .SetTextMatrix(1, 0, 0, 1, 4, (h - 12) / 2 + 1).ShowText(value).EndText();
        // Down-pointing triangle to hint at the drop-down.
        appearance.Content.SetRgbFill(0.3, 0.3, 0.3)
            .MoveTo(w - 16, h - 9).LineTo(w - 6, h - 9).LineTo(w - 11, h - 17).Fill();

        var field = Widget("Ch", name, rect);
        field["Ff"] = new PdfNumber(FlagCombo | (editable ? FlagEdit : 0));
        field["Opt"] = Options(options);
        field["V"] = new PdfString(value);
        field["DA"] = new PdfString("/Helv 12 Tf 0 g");
        field["AP"] = new PdfDictionary { ["N"] = Build(appearance) };
        Register(page, field);
    }

    /// <summary>A scrollable list choice field, with one item selected by index.</summary>
    public void ListBox(PdfPage page, string name, PdfRectangle rect, string[] options, int selectedIndex)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        DrawBorder(appearance, w, h);
        const double fontSize = 12, lineHeight = 16;
        double y = h - lineHeight;
        for (int i = 0; i < options.Length && y > -lineHeight; i++, y -= lineHeight)
        {
            if (i == selectedIndex)
            {
                appearance.Content.SetRgbFill(0.6, 0.75, 1.0).Rectangle(1, y - 3, w - 2, lineHeight).Fill();
            }
            appearance.Content.SetRgbFill(0, 0, 0).BeginText().SetFont("Helv", fontSize)
                .SetTextMatrix(1, 0, 0, 1, 4, y).ShowText(options[i]).EndText();
        }

        var field = Widget("Ch", name, rect);
        field["Opt"] = Options(options);
        if (selectedIndex >= 0 && selectedIndex < options.Length)
        {
            field["V"] = new PdfString(options[selectedIndex]);
            field["I"] = new PdfArray(new PdfNumber(selectedIndex));
        }
        field["DA"] = new PdfString("/Helv 12 Tf 0 g");
        field["AP"] = new PdfDictionary { ["N"] = Build(appearance) };
        Register(page, field);
    }

    /// <summary>
    /// A radio button group: a non-widget parent field whose Kids are the widget
    /// buttons. Each button's "on" state name is its export value; only the
    /// selected one is on.
    /// </summary>
    public void RadioGroup(PdfPage page, string name, (string Export, PdfRectangle Rect)[] buttons, string selected)
    {
        var group = new PdfDictionary
        {
            ["FT"] = new PdfName("Btn"),
            ["T"] = new PdfString(name),
            ["Ff"] = new PdfNumber(FlagRadio | FlagNoToggleToOff),
            ["V"] = new PdfName(selected),
        };
        var groupRef = _doc.AddObject(group);

        var kids = new PdfArray();
        foreach (var (export, rect) in buttons)
        {
            var on = NewAppearance(rect, out double w, out double h);
            DrawRadio(on, w, h, filled: true);
            var off = NewAppearance(rect, out _, out _);
            DrawRadio(off, w, h, filled: false);

            var widget = new PdfDictionary
            {
                ["Type"] = new PdfName("Annot"),
                ["Subtype"] = new PdfName("Widget"),
                ["Rect"] = rect.ToArray(),
                ["Parent"] = groupRef,
                ["F"] = new PdfNumber(AnnotationPrint),
                ["AS"] = new PdfName(export == selected ? export : "Off"),
                ["AP"] = new PdfDictionary
                {
                    ["N"] = new PdfDictionary { [export] = Build(on), ["Off"] = Build(off) },
                },
            };
            kids.Add(page.AddAnnotation(widget));
        }
        group["Kids"] = kids;
        _doc.RegisterFormField(groupRef);
    }

    private static void DrawRadio(FormXObject form, double w, double h, bool filled)
    {
        double cx = w / 2, cy = h / 2, r = Math.Min(w, h) / 2 - 1;
        form.Content.SetRgbStroke(0, 0, 0).SetLineWidth(1).Circle(cx, cy, r).Stroke();
        if (filled)
        {
            form.Content.SetRgbFill(0, 0, 0).Circle(cx, cy, r * 0.5).Fill();
        }
    }

    private static PdfArray Options(string[] options)
    {
        var array = new PdfArray();
        foreach (string option in options)
        {
            array.Add(new PdfString(option));
        }
        return array;
    }

    // ----- shared helpers -----

    private FormXObject NewAppearance(PdfRectangle rect, out double width, out double height)
    {
        width = rect.Width;
        height = rect.Height;
        var form = new FormXObject(PdfRectangle.FromSize(width, height));
        form.AddResource("Font", "Helv", _fontRef);
        return form;
    }

    private static void DrawBorder(FormXObject form, double w, double h) =>
        form.Content.SetRgbStroke(0.4, 0.4, 0.4).SetLineWidth(1).Rectangle(0.5, 0.5, w - 1, h - 1).Stroke();

    private PdfReference Build(FormXObject form) => _doc.AddObject(form.Build());

    private static PdfDictionary Widget(string fieldType, string name, PdfRectangle rect) => new()
    {
        ["Type"] = new PdfName("Annot"),
        ["Subtype"] = new PdfName("Widget"),
        ["FT"] = new PdfName(fieldType),
        ["T"] = new PdfString(name),
        ["Rect"] = rect.ToArray(),
        ["F"] = new PdfNumber(AnnotationPrint),
    };

    private void Register(PdfPage page, PdfDictionary field) =>
        _doc.RegisterFormField(page.AddAnnotation(field));
}
