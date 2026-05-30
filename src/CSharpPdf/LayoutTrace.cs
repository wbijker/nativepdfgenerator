namespace CSharpPdf;

/// <summary>
/// Diagnostic breadcrumb for layout-engine work. Each layout step calls
/// <see cref="Mark"/> with a short description; on a hang the timeout wrapper
/// reports the last value so you can see where the engine got stuck.
/// </summary>
internal static class LayoutTrace
{
    private const int RingSize = 30;
    private static readonly string[] Ring = new string[RingSize];
    private static int _head;

    public static int Ticks { get; private set; }

    public static string LastOp => Ring[(_head - 1 + RingSize) % RingSize] ?? "<none>";

    /// <summary>The most recent N marks in order from oldest to newest.</summary>
    public static string Tail()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < RingSize; i++)
        {
            string? line = Ring[(_head + i) % RingSize];
            if (line is not null)
            {
                sb.Append("  ").Append(line).Append('\n');
            }
        }
        return sb.ToString();
    }

    public static void Mark(string op)
    {
        Ring[_head] = op;
        _head = (_head + 1) % RingSize;
        Ticks++;
    }

    public static void Reset(string op = "<reset>")
    {
        System.Array.Clear(Ring, 0, Ring.Length);
        _head = 0;
        Ticks = 0;
        Mark(op);
    }
}
