using PdfSpec.Geometry;
using PdfSpec.Text;

namespace PdfSpec;

internal static class Program
{
    private const double LabelX = 40;
    private const double DemoX = 130;

    public static void Main(string[] args)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Text Operators";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";

        var page = doc.AddPage(PageSizes.A4);
        var cs = page.Content;

        double y = PageSizes.A4.Height - 50;

        // Every demo wraps its own state changes in q…Q so Tc/Tw/Tz/TL/Tf/Ts/Tr/colour
        // don't leak across rows. The Label helper does the same for the operator name
        // in the left gutter.
        void Label(string text)
        {
            cs.Save();
            cs.BeginText();
            cs.SetFont(Standard14Font.Courier, 9);
            cs.SetTextMatrix(1, 0, 0, 1, LabelX, y);
            cs.SetGrayFill(0.25);
            cs.ShowText(text);
            cs.EndText();
            cs.Restore();
        }

        // ===== Title =====
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaBold, 18);
        cs.SetTextMatrix(1, 0, 0, 1, LabelX, y);
        cs.ShowText("PDF Text Operators");
        cs.EndText();
        cs.Restore();
        y -= 20;

        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 9);
        cs.SetTextMatrix(1, 0, 0, 1, LabelX, y);
        cs.ShowText("ISO 32000-1 §9.3 (text state) and §9.4 (text objects)");
        cs.EndText();
        cs.Restore();
        y -= 26;

        // ===== BT / ET =====
        Label("BT/ET");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Every demo opens BT and closes ET.");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tf — SetFont =====
        Label("Tf");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Helvetica 10   ");
        cs.SetFont(Standard14Font.TimesItalic, 12);
        cs.ShowText("Times-Italic 12   ");
        cs.SetFont(Standard14Font.Courier, 10);
        cs.ShowText("Courier 10");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tj — ShowText =====
        Label("Tj");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("ShowText: one literal string ending in Tj.");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== ' — NextLineShowText (consumes TL) =====
        Label("'");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetLeading(12);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Line 1 (Tj).");
        cs.NextLineShowText("Line 2 (' moves down by TL then shows).");
        cs.NextLineShowText("Line 3 (').");
        cs.EndText();
        cs.Restore();
        y -= 44;

        // ===== " — NextLineShowText with Tw / Tc =====
        Label("\"");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetLeading(12);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Default Tw/Tc.");
        cs.NextLineShowText(8, 2, "\" sets Tw=8 Tc=2 then shows.");
        cs.EndText();
        cs.Restore();
        y -= 32;

        // ===== TJ — ShowTextWithKerning =====
        Label("TJ");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowTextWithKerning(
            "S", -200, "p", -200, "a", -200, "c", -200, "e", -200, "d",
            "    (negative number widens, positive tightens)");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Td — MoveText =====
        Label("Td");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("[Td origin]");
        cs.MoveText(70, 0);
        cs.ShowText("[+70,0]");
        cs.MoveText(70, 0);
        cs.ShowText("[+70,0]");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== TD — MoveTextSetLeading =====
        Label("TD");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Top line.");
        cs.MoveTextSetLeading(0, -12);
        cs.ShowText("TD(0,-12): moves and sets TL=12.");
        cs.NextLine();
        cs.ShowText("T* now uses the leading TD set.");
        cs.EndText();
        cs.Restore();
        y -= 44;

        // ===== Tm — SetTextMatrix (rotated) =====
        Label("Tm");
        {
            double angle = 14 * Math.PI / 180;
            double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
            cs.Save();
            cs.BeginText();
            cs.SetFont(Standard14Font.Helvetica, 11);
            cs.SetTextMatrix(cosA, sinA, -sinA, cosA, DemoX, y - 18);
            cs.ShowText("Rotated 14° via Tm.");
            cs.EndText();
            cs.Restore();
        }
        y -= 40;

        // ===== T* — NextLine (consumes TL) =====
        Label("T*");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetLeading(11);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("Line A (T* advances by TL).");
        cs.NextLine();
        cs.ShowText("Line B.");
        cs.NextLine();
        cs.ShowText("Line C.");
        cs.EndText();
        cs.Restore();
        y -= 42;

        // ===== TL — SetLeading =====
        Label("TL");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 9);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("TL sets the leading consumed by T*, TD, ' and \". (Demonstrated above.)");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tc — SetCharSpacing =====
        Label("Tc");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetCharSpacing(0);
        cs.ShowText("Tc=0  normal character spacing");
        cs.EndText();
        cs.Restore();
        y -= 14;
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetCharSpacing(2);
        cs.ShowText("Tc=2  wider character spacing");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tw — SetWordSpacing =====
        Label("Tw");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetWordSpacing(0);
        cs.ShowText("Tw=0  normal word spacing between words");
        cs.EndText();
        cs.Restore();
        y -= 14;
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetWordSpacing(8);
        cs.ShowText("Tw=8  wider word spacing between words");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tz — SetHorizontalScaling =====
        Label("Tz");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 10);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetHorizontalScaling(100);
        cs.ShowText("Tz=100  ");
        cs.SetHorizontalScaling(150);
        cs.ShowText("Tz=150  ");
        cs.SetHorizontalScaling(60);
        cs.ShowText("Tz=60");
        cs.EndText();
        cs.Restore();
        y -= 22;

        // ===== Tr — SetTextRenderMode (0..3) =====
        Label("Tr 0-3");
        cs.Save();
        cs.SetRgbFill(0.10, 0.10, 0.10);
        cs.SetRgbStroke(0.86, 0.15, 0.15);
        cs.SetLineWidth(0.6);
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaBold, 16);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y - 4);
        cs.SetTextRenderMode(0); cs.ShowText("Fill ");
        cs.SetTextRenderMode(1); cs.ShowText("Stroke ");
        cs.SetTextRenderMode(2); cs.ShowText("Fill+Stroke ");
        cs.SetTextRenderMode(3); cs.ShowText("Invisible");
        cs.EndText();
        cs.Restore();

        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaOblique, 8);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y - 20);
        cs.SetGrayFill(0.45);
        cs.ShowText("(Tr=3 'Invisible' is emitted but not rendered.)");
        cs.EndText();
        cs.Restore();
        y -= 38;

        // ===== Tr=7 — Clip path = glyph silhouettes =====
        Label("Tr=7");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaBold, 36);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y - 28);
        cs.SetTextRenderMode(7);
        cs.ShowText("CLIP");
        cs.EndText();
        // Subsequent drawing is clipped to the glyph silhouettes of "CLIP".
        cs.SetRgbFill(0.13, 0.31, 0.78);
        cs.Rectangle(DemoX, y - 34, 140, 38);
        cs.Fill();
        cs.Restore();
        y -= 46;

        // ===== Ts — SetTextRise (sub/superscript) =====
        Label("Ts");
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.Helvetica, 12);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.ShowText("H");
        cs.SetFont(Standard14Font.Helvetica, 8);
        cs.SetTextRise(-3);
        cs.ShowText("2");
        cs.SetTextRise(0);
        cs.SetFont(Standard14Font.Helvetica, 12);
        cs.ShowText("O      E = mc");
        cs.SetFont(Standard14Font.Helvetica, 8);
        cs.SetTextRise(5);
        cs.ShowText("2");
        cs.EndText();
        cs.Restore();
        y -= 26;

        // ===== Colour operators valid in text state =====
        Label("rg g k / RG G K");
        cs.Save();
        cs.SetLineWidth(0.5);
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaBold, 14);
        cs.SetTextMatrix(1, 0, 0, 1, DemoX, y);
        cs.SetTextRenderMode(0);
        cs.SetRgbFill(0.86, 0.15, 0.15); cs.ShowText("rg ");
        cs.SetGrayFill(0.40); cs.ShowText("g ");
        cs.SetCmykFill(0.90, 0.50, 0.00, 0.00); cs.ShowText("k    ");
        cs.SetTextRenderMode(1);
        cs.SetRgbStroke(0.13, 0.31, 0.78); cs.ShowText("RG ");
        cs.SetGrayStroke(0.30); cs.ShowText("G ");
        cs.SetCmykStroke(0.00, 0.70, 0.90, 0.10); cs.ShowText("K");
        cs.EndText();
        cs.Restore();
        y -= 28;

        // ===== Footer =====
        cs.Save();
        cs.BeginText();
        cs.SetFont(Standard14Font.HelveticaOblique, 8);
        cs.SetTextMatrix(1, 0, 0, 1, LabelX, 35);
        cs.SetGrayFill(0.55);
        cs.ShowText("Generated by PdfSpec.");
        cs.EndText();
        cs.Restore();

        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}
