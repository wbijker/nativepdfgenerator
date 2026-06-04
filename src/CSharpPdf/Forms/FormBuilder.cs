using PdfSpec;
using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace CSharpPdf.Forms;

/// <summary>
/// Builds AcroForm fields (ISO 32000-1 Chapter 7). Each visible field is a
/// merged field + widget annotation. Appearance streams are generated as
/// form XObjects so the fields render in any viewer. The builder shares one
/// Helvetica font via the form's default resources (DR).
/// </summary>
public sealed class FormBuilder
{
    private const int FlagPushButton = 1 << 16;
    private const int FlagRadio = 1 << 15;
    private const int FlagNoToggleToOff = 1 << 14;
    private const int FlagCombo = 1 << 17;
    private const int FlagEdit = 1 << 18;
    private const int FlagMultiline = 1 << 12;

    private const int AnnotationPrint = 4;

    private readonly PdfDoc _doc;
    private readonly PdfReference _fontRef;

    public FormBuilder(PdfDoc doc)
    {
        _doc = doc;
        _fontRef = doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica, StandardFonts.WinAnsiEncoding));

        var fonts = new PdfDictionary();
        fonts.Add("Helv", _fontRef);
        var dr = new PdfDictionary();
        dr.Add("Font", fonts);
        _doc.AcroForm.Add("DR", dr);
        _doc.AcroForm.SetString("DA", "/Helv 0 Tf 0 g");
        _doc.AcroForm.SetBoolean("NeedAppearances", false);
    }

    public void TextField(PdfPage page, string name, PdfRectangle rect, string value,
        double fontSize = 12, bool multiline = false)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        DrawBorder(appearance, w, h);

        string[] lines = multiline ? value.Split('\n') : new[] { value };
        var t = appearance.Content.SetRgbFill(PdfColor.Rgb(0, 0, 0)).AddText()
            .SetFont("Helv", fontSize).SetLeading(fontSize + 2)
            .SetTextMatrix(1, 0, 0, 1, 4, h - fontSize - 3);
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) t.NextLine();
            t.ShowText(lines[i]);
        }
        t.Build();

        var field = Widget("Tx", name, rect);
        field.SetString("V", value);
        field.SetString("DA", $"/Helv {fontSize} Tf 0 g");
        if (multiline) field.SetInteger("Ff", FlagMultiline);
        var ap = new PdfDictionary();
        ap.Add("N", Build(appearance));
        field.Add("AP", ap);
        Register(page, field);
    }

    public void CheckBox(PdfPage page, string name, PdfRectangle rect, bool isChecked)
    {
        var on = NewAppearance(rect, out double w, out double h);
        DrawBorder(on, w, h);
        on.Content.SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetLineWidth(2)
            .MoveTo(w * 0.2, h * 0.5).LineTo(w * 0.42, h * 0.25).LineTo(w * 0.8, h * 0.8).Stroke();

        var off = NewAppearance(rect, out _, out _);
        DrawBorder(off, w, h);

        var field = Widget("Btn", name, rect);
        field.SetName("V", isChecked ? "Yes" : "Off");
        field.SetName("AS", isChecked ? "Yes" : "Off");
        var inner = new PdfDictionary();
        inner.Add("Yes", Build(on));
        inner.Add("Off", Build(off));
        var ap = new PdfDictionary();
        ap.Add("N", inner);
        field.Add("AP", ap);
        Register(page, field);
    }

    public void PushButton(PdfPage page, string name, PdfRectangle rect, string caption, PdfDictionary action)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        appearance.Content.SetRgbFill(PdfColor.Rgb(0.86, 0.86, 0.92)).Rectangle(0, 0, w, h).Fill();
        DrawBorder(appearance, w, h);
        double fontSize = 12;
        double textWidth = caption.Length * fontSize * 0.5;
        appearance.Content.SetRgbFill(PdfColor.Rgb(0, 0, 0)).AddText().SetFont("Helv", fontSize)
            .SetTextMatrix(1, 0, 0, 1, Math.Max(4, (w - textWidth) / 2), (h - fontSize) / 2 + 1)
            .ShowText(caption).Build();

        var field = Widget("Btn", name, rect);
        field.SetInteger("Ff", FlagPushButton);
        var ap = new PdfDictionary();
        ap.Add("N", Build(appearance));
        field.Add("AP", ap);
        field.Add("A", action);
        Register(page, field);
    }

    public void ComboBox(PdfPage page, string name, PdfRectangle rect, string[] options, string value, bool editable = false)
    {
        var appearance = NewAppearance(rect, out double w, out double h);
        DrawBorder(appearance, w, h);
        appearance.Content.SetRgbFill(PdfColor.Rgb(0, 0, 0)).AddText().SetFont("Helv", 12)
            .SetTextMatrix(1, 0, 0, 1, 4, (h - 12) / 2 + 1).ShowText(value).Build();
        appearance.Content.SetRgbFill(PdfColor.Rgb(0.3, 0.3, 0.3))
            .MoveTo(w - 16, h - 9).LineTo(w - 6, h - 9).LineTo(w - 11, h - 17).Fill();

        var field = Widget("Ch", name, rect);
        field.SetInteger("Ff", FlagCombo | (editable ? FlagEdit : 0));
        field.Add("Opt", Options(options));
        field.SetString("V", value);
        field.SetString("DA", "/Helv 12 Tf 0 g");
        var ap = new PdfDictionary();
        ap.Add("N", Build(appearance));
        field.Add("AP", ap);
        Register(page, field);
    }

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
                appearance.Content.SetRgbFill(PdfColor.Rgb(0.6, 0.75, 1.0)).Rectangle(1, y - 3, w - 2, lineHeight).Fill();
            }
            appearance.Content.SetRgbFill(PdfColor.Rgb(0, 0, 0)).AddText().SetFont("Helv", fontSize)
                .SetTextMatrix(1, 0, 0, 1, 4, y).ShowText(options[i]).Build();
        }

        var field = Widget("Ch", name, rect);
        field.Add("Opt", Options(options));
        if (selectedIndex >= 0 && selectedIndex < options.Length)
        {
            field.SetString("V", options[selectedIndex]);
            field.Add("I", new PdfArray(new PdfNumber((long)selectedIndex)));
        }
        field.SetString("DA", "/Helv 12 Tf 0 g");
        var ap = new PdfDictionary();
        ap.Add("N", Build(appearance));
        field.Add("AP", ap);
        Register(page, field);
    }

    public void RadioGroup(PdfPage page, string name, (string Export, PdfRectangle Rect)[] buttons, string selected)
    {
        var group = new PdfDictionary();
        group.SetName("FT", "Btn");
        group.SetString("T", name);
        group.SetInteger("Ff", FlagRadio | FlagNoToggleToOff);
        group.SetName("V", selected);
        var groupRef = _doc.AddObject(group);

        var kids = new PdfArray();
        foreach (var (export, rect) in buttons)
        {
            var on = NewAppearance(rect, out double w, out double h);
            DrawRadio(on, w, h, filled: true);
            var off = NewAppearance(rect, out _, out _);
            DrawRadio(off, w, h, filled: false);

            var widget = new PdfDictionary();
            widget.SetName("Type", "Annot");
            widget.SetName("Subtype", "Widget");
            widget.Add("Rect", rect.ToArray());
            widget.Add("Parent", groupRef);
            widget.SetInteger("F", AnnotationPrint);
            widget.SetName("AS", export == selected ? export : "Off");
            var inner = new PdfDictionary();
            inner.Add(export, Build(on));
            inner.Add("Off", Build(off));
            var ap = new PdfDictionary();
            ap.Add("N", inner);
            widget.Add("AP", ap);
            kids.Add(page.AddAnnotation(widget));
        }
        group.Add("Kids", kids);
        _doc.RegisterFormField(groupRef);
    }

    private static void DrawRadio(FormXObject form, double w, double h, bool filled)
    {
        double cx = w / 2, cy = h / 2, r = Math.Min(w, h) / 2 - 1;
        form.Content.SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetLineWidth(1).Circle(cx, cy, r).Stroke();
        if (filled)
        {
            form.Content.SetRgbFill(PdfColor.Rgb(0, 0, 0)).Circle(cx, cy, r * 0.5).Fill();
        }
    }

    private static PdfArray Options(string[] options)
    {
        var array = new PdfArray();
        foreach (string option in options) array.Add(new PdfString(option));
        return array;
    }

    private FormXObject NewAppearance(PdfRectangle rect, out double width, out double height)
    {
        width = rect.Width;
        height = rect.Height;
        var form = new FormXObject(_doc, PdfRectangle.FromSize(width, height));
        form.Resources.AddFont("Helv", _fontRef);
        return form;
    }

    private static void DrawBorder(FormXObject form, double w, double h) =>
        form.Content.SetRgbStroke(PdfColor.Rgb(0.4, 0.4, 0.4)).SetLineWidth(1).Rectangle(0.5, 0.5, w - 1, h - 1).Stroke();

    private PdfReference Build(FormXObject form) => _doc.AddObject(form.Build());

    private static PdfDictionary Widget(string fieldType, string name, PdfRectangle rect)
    {
        var d = new PdfDictionary();
        d.SetName("Type", "Annot");
        d.SetName("Subtype", "Widget");
        d.SetName("FT", fieldType);
        d.SetString("T", name);
        d.Add("Rect", rect.ToArray());
        d.SetInteger("F", AnnotationPrint);
        return d;
    }

    private void Register(PdfPage page, PdfDictionary field) =>
        _doc.RegisterFormField(page.AddAnnotation(field));
}
