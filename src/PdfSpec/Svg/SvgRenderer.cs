using PdfSpec.Content;
using PdfSpec.Geometry;

namespace PdfSpec.Svg;

/// <summary>
/// Walks an <see cref="SvgDocument"/> and emits the equivalent path /
/// paint operators into a <see cref="ContentStream"/>.
///
/// <para>
/// The strategy is to fold the viewBox + every per-node
/// <c>transform="…"</c> into a single <see cref="SvgMatrix"/> applied
/// in code to every coordinate. The transformed point then goes through
/// <see cref="ContentStream.MoveTo"/> / <c>LineTo</c> / <c>CurveTo</c>
/// which carry the existing top-left-origin Y-flip — that way SVG and
/// PDF coordinate systems stay reconciled without us emitting a raw
/// <c>cm</c> in the wrong sense.
/// </para>
///
/// <para>
/// Per-shape paint goes: <c>Save</c> → set fill / stroke / line width
/// / opacity → construct the path with the transformed coords →
/// <c>Fill</c> / <c>Stroke</c> / <c>FillStroke</c> → <c>Restore</c>.
/// Stroke width is multiplied by the matrix's linear scale so a
/// <c>scale(2)</c> on a parent group widens the stroke too — matching
/// SVG's geometric-bounding semantics.
/// </para>
/// </summary>
internal static class SvgRenderer
{
    public static void Render(ContentStream cs, SvgDocument doc, double targetWidth, double targetHeight)
    {
        var rootMatrix = ComputeViewportMatrix(doc, targetWidth, targetHeight);
        RenderNode(cs, doc.Root, rootMatrix, StyleState.Defaults);
    }

    // ===== style cascade ====================================================

    private struct StyleState
    {
        public SvgPaint Fill;
        public SvgPaint Stroke;
        public double StrokeWidth;
        public double Opacity;
        public double FillOpacity;
        public double StrokeOpacity;

        public static StyleState Defaults => new()
        {
            Fill           = SvgPaint.Of(PdfColor.FromHex(0x000000)),
            Stroke         = SvgPaint.None,
            StrokeWidth    = 1,
            Opacity        = 1,
            FillOpacity    = 1,
            StrokeOpacity  = 1,
        };

        public StyleState With(SvgAttrs a)
        {
            var s = this;
            if (a.Fill           is not null)     s.Fill          = a.Fill;
            if (a.Stroke         is not null)     s.Stroke        = a.Stroke;
            if (a.StrokeWidth    is { } sw)       s.StrokeWidth   = sw;
            if (a.Opacity        is { } o)        s.Opacity       = o;
            if (a.FillOpacity    is { } fo)       s.FillOpacity   = fo;
            if (a.StrokeOpacity  is { } so)       s.StrokeOpacity = so;
            return s;
        }
    }

    // ===== walk =============================================================

    private static void RenderNode(ContentStream cs, SvgNode node, SvgMatrix matrix, StyleState style)
    {
        var s = style.With(node.Attrs);
        var m = node.Attrs.Transform is { } t ? matrix.Multiply(t) : matrix;

        switch (node)
        {
            case SvgGroup g:
                foreach (var child in g.Children)
                    RenderNode(cs, child, m, s);
                break;
            case SvgRect r:
                DrawShape(cs, m, s, c => BuildRectPath(c, m, r));
                break;
            case SvgCircle ci:
                DrawShape(cs, m, s, c => BuildEllipsePath(c, m, ci.Cx, ci.Cy, ci.R, ci.R));
                break;
            case SvgEllipse e:
                DrawShape(cs, m, s, c => BuildEllipsePath(c, m, e.Cx, e.Cy, e.Rx, e.Ry));
                break;
            case SvgLine ln:
                DrawShape(cs, m, s, c =>
                {
                    var (x1, y1) = m.Apply(ln.X1, ln.Y1);
                    var (x2, y2) = m.Apply(ln.X2, ln.Y2);
                    c.MoveTo(x1, y1).LineTo(x2, y2);
                });
                break;
            case SvgPolyline poly:
                DrawShape(cs, m, s, c => BuildPolyPath(c, m, poly));
                break;
            case SvgPath path:
                DrawShape(cs, m, s, c => BuildPathDataPath(c, m, path.D));
                break;
        }
    }

    private static void DrawShape(ContentStream cs, SvgMatrix matrix, StyleState style, Action<ContentStream> buildPath)
    {
        bool hasFill   = !style.Fill.IsNone   && style.Fill.Color   is not null;
        bool hasStroke = !style.Stroke.IsNone && style.Stroke.Color is not null && style.StrokeWidth > 0;
        if (!hasFill && !hasStroke) return;

        cs.Save();

        if (hasFill)
        {
            cs.SetFillColor(style.Fill.Color!);
            double a = style.Opacity * style.FillOpacity;
            if (a < 1) cs.SetFillOpacity(a);
        }
        if (hasStroke)
        {
            cs.SetStrokeColor(style.Stroke.Color!);
            cs.SetLineWidth(style.StrokeWidth * matrix.LinearScale());
            double a = style.Opacity * style.StrokeOpacity;
            if (a < 1) cs.SetStrokeOpacity(a);
        }

        buildPath(cs);

        if (hasFill && hasStroke) cs.FillStroke();
        else if (hasFill)         cs.Fill();
        else                      cs.Stroke();

        cs.Restore();
    }

    // ===== shape builders ===================================================

    private static void BuildRectPath(ContentStream cs, SvgMatrix m, SvgRect r)
    {
        double rx = r.Rx, ry = r.Ry;
        // Per SVG: missing rx/ry inherits the other.
        if (rx <= 0 && ry > 0) rx = ry;
        else if (ry <= 0 && rx > 0) ry = rx;
        rx = Math.Min(rx, r.Width  / 2);
        ry = Math.Min(ry, r.Height / 2);

        if (rx <= 0 || ry <= 0)
        {
            var (p1x, p1y) = m.Apply(r.X,             r.Y);
            var (p2x, p2y) = m.Apply(r.X + r.Width,   r.Y);
            var (p3x, p3y) = m.Apply(r.X + r.Width,   r.Y + r.Height);
            var (p4x, p4y) = m.Apply(r.X,             r.Y + r.Height);
            cs.MoveTo(p1x, p1y).LineTo(p2x, p2y).LineTo(p3x, p3y).LineTo(p4x, p4y).ClosePath();
            return;
        }

        const double K = 0.5522847498307936;
        double kx = rx * K, ky = ry * K;
        double x = r.X, y = r.Y, right = x + r.Width, bottom = y + r.Height;

        // Start at top edge, just right of TL corner; trace clockwise.
        Move(cs, m, x + rx, y);
        Line(cs, m, right - rx, y);
        Curve(cs, m, right - rx + kx, y, right, y + ry - ky, right, y + ry);
        Line(cs, m, right, bottom - ry);
        Curve(cs, m, right, bottom - ry + ky, right - rx + kx, bottom, right - rx, bottom);
        Line(cs, m, x + rx, bottom);
        Curve(cs, m, x + rx - kx, bottom, x, bottom - ry + ky, x, bottom - ry);
        Line(cs, m, x, y + ry);
        Curve(cs, m, x, y + ry - ky, x + rx - kx, y, x + rx, y);
        cs.ClosePath();
    }

    private static void BuildEllipsePath(ContentStream cs, SvgMatrix m, double cx, double cy, double rx, double ry)
    {
        if (rx <= 0 || ry <= 0) return;
        const double K = 0.5522847498307936;
        double kx = rx * K, ky = ry * K;

        Move(cs, m, cx + rx, cy);
        Curve(cs, m, cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
        Curve(cs, m, cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
        Curve(cs, m, cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
        Curve(cs, m, cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
        cs.ClosePath();
    }

    private static void BuildPolyPath(ContentStream cs, SvgMatrix m, SvgPolyline p)
    {
        if (p.Points.Length < 2) return;
        Move(cs, m, p.Points[0], p.Points[1]);
        for (int i = 2; i + 1 < p.Points.Length; i += 2)
            Line(cs, m, p.Points[i], p.Points[i + 1]);
        if (p.Closed) cs.ClosePath();
    }

    // ===== path data builder ================================================

    private static void BuildPathDataPath(ContentStream cs, SvgMatrix m, string d)
    {
        var ops = SvgPathParser.Parse(d);
        double curX = 0, curY = 0;
        double startX = 0, startY = 0;
        double prevCtrlX = 0, prevCtrlY = 0;
        char prevCmd = '\0';

        foreach (var op in ops)
        {
            char c = op.Cmd;
            switch (c)
            {
                case 'M':
                    curX = op.Args[0]; curY = op.Args[1];
                    startX = curX; startY = curY;
                    Move(cs, m, curX, curY);
                    break;
                case 'm':
                    curX += op.Args[0]; curY += op.Args[1];
                    startX = curX; startY = curY;
                    Move(cs, m, curX, curY);
                    break;
                case 'L':
                    curX = op.Args[0]; curY = op.Args[1];
                    Line(cs, m, curX, curY);
                    break;
                case 'l':
                    curX += op.Args[0]; curY += op.Args[1];
                    Line(cs, m, curX, curY);
                    break;
                case 'H': curX = op.Args[0];  Line(cs, m, curX, curY); break;
                case 'h': curX += op.Args[0]; Line(cs, m, curX, curY); break;
                case 'V': curY = op.Args[0];  Line(cs, m, curX, curY); break;
                case 'v': curY += op.Args[0]; Line(cs, m, curX, curY); break;

                case 'C':
                case 'c':
                {
                    bool rel = c == 'c';
                    double c1x = rel ? curX + op.Args[0] : op.Args[0];
                    double c1y = rel ? curY + op.Args[1] : op.Args[1];
                    double c2x = rel ? curX + op.Args[2] : op.Args[2];
                    double c2y = rel ? curY + op.Args[3] : op.Args[3];
                    double x   = rel ? curX + op.Args[4] : op.Args[4];
                    double y   = rel ? curY + op.Args[5] : op.Args[5];
                    Curve(cs, m, c1x, c1y, c2x, c2y, x, y);
                    prevCtrlX = c2x; prevCtrlY = c2y;
                    curX = x; curY = y;
                    break;
                }

                case 'S':
                case 's':
                {
                    bool rel = c == 's';
                    double c2x = rel ? curX + op.Args[0] : op.Args[0];
                    double c2y = rel ? curY + op.Args[1] : op.Args[1];
                    double x   = rel ? curX + op.Args[2] : op.Args[2];
                    double y   = rel ? curY + op.Args[3] : op.Args[3];
                    double c1x, c1y;
                    if (prevCmd is 'C' or 'c' or 'S' or 's')
                    {
                        c1x = 2 * curX - prevCtrlX;
                        c1y = 2 * curY - prevCtrlY;
                    }
                    else { c1x = curX; c1y = curY; }
                    Curve(cs, m, c1x, c1y, c2x, c2y, x, y);
                    prevCtrlX = c2x; prevCtrlY = c2y;
                    curX = x; curY = y;
                    break;
                }

                case 'Q':
                case 'q':
                {
                    bool rel = c == 'q';
                    double qx1 = rel ? curX + op.Args[0] : op.Args[0];
                    double qy1 = rel ? curY + op.Args[1] : op.Args[1];
                    double x   = rel ? curX + op.Args[2] : op.Args[2];
                    double y   = rel ? curY + op.Args[3] : op.Args[3];
                    EmitQuadAsCubic(cs, m, curX, curY, qx1, qy1, x, y);
                    prevCtrlX = qx1; prevCtrlY = qy1;
                    curX = x; curY = y;
                    break;
                }

                case 'T':
                case 't':
                {
                    bool rel = c == 't';
                    double x = rel ? curX + op.Args[0] : op.Args[0];
                    double y = rel ? curY + op.Args[1] : op.Args[1];
                    double qx1, qy1;
                    if (prevCmd is 'Q' or 'q' or 'T' or 't')
                    {
                        qx1 = 2 * curX - prevCtrlX;
                        qy1 = 2 * curY - prevCtrlY;
                    }
                    else { qx1 = curX; qy1 = curY; }
                    EmitQuadAsCubic(cs, m, curX, curY, qx1, qy1, x, y);
                    prevCtrlX = qx1; prevCtrlY = qy1;
                    curX = x; curY = y;
                    break;
                }

                case 'A':
                case 'a':
                {
                    bool rel = c == 'a';
                    double rx  = op.Args[0];
                    double ry  = op.Args[1];
                    double phi = op.Args[2] * Math.PI / 180.0;
                    int fA = op.Args[3] != 0 ? 1 : 0;
                    int fS = op.Args[4] != 0 ? 1 : 0;
                    double ex = rel ? curX + op.Args[5] : op.Args[5];
                    double ey = rel ? curY + op.Args[6] : op.Args[6];
                    EmitArc(cs, m, curX, curY, rx, ry, phi, fA, fS, ex, ey);
                    curX = ex; curY = ey;
                    break;
                }

                case 'Z':
                case 'z':
                    cs.ClosePath();
                    curX = startX; curY = startY;
                    break;
            }

            prevCmd = c;
        }
    }

    private static void EmitQuadAsCubic(ContentStream cs, SvgMatrix m,
        double x0, double y0, double qx1, double qy1, double x, double y)
    {
        // Standard quadratic-to-cubic up-conversion: the cubic control
        // points sit at the 1/3 and 2/3 points along the legs of the
        // quadratic triangle.
        double c1x = x0 + 2 * (qx1 - x0) / 3.0;
        double c1y = y0 + 2 * (qy1 - y0) / 3.0;
        double c2x = x  + 2 * (qx1 - x ) / 3.0;
        double c2y = y  + 2 * (qy1 - y ) / 3.0;
        Curve(cs, m, c1x, c1y, c2x, c2y, x, y);
    }

    // ===== arc → cubic bezier ==============================================

    /// <summary>
    /// Approximate an SVG endpoint-parametrised elliptical arc with one
    /// or more cubic-bezier segments. Follows the conversion in the
    /// SVG implementation notes — endpoint → center parametrisation,
    /// split into ≤ 90° segments, each segment built with the standard
    /// alpha tangent factor.
    /// </summary>
    private static void EmitArc(ContentStream cs, SvgMatrix m,
        double x1, double y1, double rx, double ry, double phi,
        int fA, int fS, double x2, double y2)
    {
        // Degenerate cases collapse to a straight line.
        if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2))
        {
            Line(cs, m, x2, y2);
            return;
        }

        rx = Math.Abs(rx);
        ry = Math.Abs(ry);

        double cosphi = Math.Cos(phi);
        double sinphi = Math.Sin(phi);

        // F.6.5.1 — point at (x1, y1) − (x2, y2) / 2 in transformed space
        double dx = (x1 - x2) / 2.0;
        double dy = (y1 - y2) / 2.0;
        double x1p =  cosphi * dx + sinphi * dy;
        double y1p = -sinphi * dx + cosphi * dy;

        double rx2 = rx * rx;
        double ry2 = ry * ry;
        double x1p2 = x1p * x1p;
        double y1p2 = y1p * y1p;

        // F.6.6 — radius scaling so the chord fits.
        double lambda = x1p2 / rx2 + y1p2 / ry2;
        if (lambda > 1)
        {
            double scale = Math.Sqrt(lambda);
            rx *= scale; ry *= scale;
            rx2 = rx * rx; ry2 = ry * ry;
        }

        // F.6.5.2 — center prime
        double sign = fA == fS ? -1 : 1;
        double numerator = rx2 * ry2 - rx2 * y1p2 - ry2 * x1p2;
        double denominator = rx2 * y1p2 + ry2 * x1p2;
        double coef = sign * Math.Sqrt(Math.Max(0, numerator / denominator));
        double cxp =  coef * (rx * y1p / ry);
        double cyp = -coef * (ry * x1p / rx);

        // F.6.5.3 — center in user space
        double cx = cosphi * cxp - sinphi * cyp + (x1 + x2) / 2.0;
        double cy = sinphi * cxp + cosphi * cyp + (y1 + y2) / 2.0;

        // F.6.5.5/6 — start angle and sweep
        double theta1 = SignedAngle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        double dtheta = SignedAngle(
            (x1p - cxp) / rx, (y1p - cyp) / ry,
            (-x1p - cxp) / rx, (-y1p - cyp) / ry);

        if (fS == 0 && dtheta > 0) dtheta -= 2 * Math.PI;
        else if (fS == 1 && dtheta < 0) dtheta += 2 * Math.PI;

        // Split into ≤ 90° segments.
        int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(dtheta) / (Math.PI / 2)));
        double delta = dtheta / segments;
        double t = Math.Tan(delta / 2.0);
        double alpha = Math.Sin(delta) * (Math.Sqrt(4 + 3 * t * t) - 1) / 3.0;

        double theta = theta1;
        for (int i = 0; i < segments; i++)
        {
            double thetaNext = theta + delta;
            double sint = Math.Sin(theta),     cost = Math.Cos(theta);
            double sintn = Math.Sin(thetaNext), costn = Math.Cos(thetaNext);

            // Endpoint + control points in the centered, axis-aligned ellipse.
            double e1x = rx * cost,  e1y = ry * sint;
            double c1x = e1x - alpha * rx * sint;
            double c1y = e1y + alpha * ry * cost;
            double e2x = rx * costn, e2y = ry * sintn;
            double c2x = e2x + alpha * rx * sintn;
            double c2y = e2y - alpha * ry * costn;

            // Rotate by phi and translate to the arc's center.
            var (cp1x, cp1y) = (cosphi * c1x - sinphi * c1y + cx, sinphi * c1x + cosphi * c1y + cy);
            var (cp2x, cp2y) = (cosphi * c2x - sinphi * c2y + cx, sinphi * c2x + cosphi * c2y + cy);
            var (endx, endy) = (cosphi * e2x - sinphi * e2y + cx, sinphi * e2x + cosphi * e2y + cy);

            Curve(cs, m, cp1x, cp1y, cp2x, cp2y, endx, endy);

            theta = thetaNext;
        }
    }

    private static double SignedAngle(double ux, double uy, double vx, double vy)
    {
        double dot = ux * vx + uy * vy;
        double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        if (len == 0) return 0;
        double a = Math.Acos(Math.Clamp(dot / len, -1, 1));
        return (ux * vy - uy * vx < 0) ? -a : a;
    }

    // ===== viewport =========================================================

    private static SvgMatrix ComputeViewportMatrix(SvgDocument doc, double targetW, double targetH)
    {
        // preserveAspectRatio default — "xMidYMid meet": uniform scale,
        // centered, no clipping.
        if (doc.ViewBox is { } vb && vb.Width > 0 && vb.Height > 0)
        {
            double s = Math.Min(targetW / vb.Width, targetH / vb.Height);
            double offX = (targetW - vb.Width  * s) / 2;
            double offY = (targetH - vb.Height * s) / 2;
            return SvgMatrix.Translate(offX, offY)
                .Multiply(SvgMatrix.Scale(s, s))
                .Multiply(SvgMatrix.Translate(-vb.X, -vb.Y));
        }

        if (doc.IntrinsicWidth > 0 && doc.IntrinsicHeight > 0)
        {
            double s = Math.Min(targetW / doc.IntrinsicWidth, targetH / doc.IntrinsicHeight);
            double offX = (targetW - doc.IntrinsicWidth  * s) / 2;
            double offY = (targetH - doc.IntrinsicHeight * s) / 2;
            return SvgMatrix.Translate(offX, offY).Multiply(SvgMatrix.Scale(s, s));
        }

        return SvgMatrix.Identity;
    }

    // ===== path emission helpers ============================================

    private static void Move(ContentStream cs, SvgMatrix m, double x, double y)
    {
        var (px, py) = m.Apply(x, y);
        cs.MoveTo(px, py);
    }

    private static void Line(ContentStream cs, SvgMatrix m, double x, double y)
    {
        var (px, py) = m.Apply(x, y);
        cs.LineTo(px, py);
    }

    private static void Curve(ContentStream cs, SvgMatrix m,
        double c1x, double c1y, double c2x, double c2y, double x, double y)
    {
        var (p1x, p1y) = m.Apply(c1x, c1y);
        var (p2x, p2y) = m.Apply(c2x, c2y);
        var (ex,  ey ) = m.Apply(x,   y);
        cs.CurveTo(p1x, p1y, p2x, p2y, ex, ey);
    }
}
