namespace CSharpPdf.Layout;

/// <summary>
/// Shares an available length across items using their min and preferred sizes
/// (CSS-table-style): preferred when it all fits, proportional shrink by flex
/// (preferred − min) otherwise, with min as the floor.
/// </summary>
internal static class Distribution
{
    public static double[] Across(double[] min, double[] preferred, double available)
    {
        int n = min.Length;
        double sumMin = 0, sumPreferred = 0;
        for (int i = 0; i < n; i++)
        {
            sumMin += min[i];
            sumPreferred += preferred[i];
        }

        var widths = new double[n];
        if (sumPreferred <= available || sumPreferred <= sumMin)
        {
            System.Array.Copy(preferred, widths, n);
        }
        else if (sumMin >= available)
        {
            System.Array.Copy(min, widths, n);
        }
        else
        {
            double scale = (available - sumMin) / (sumPreferred - sumMin);
            for (int i = 0; i < n; i++)
            {
                widths[i] = min[i] + (preferred[i] - min[i]) * scale;
            }
        }
        return widths;
    }
}
