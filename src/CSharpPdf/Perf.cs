using System.Collections.Generic;
using System.Diagnostics;

namespace CSharpPdf;

/// <summary>
/// Lightweight per-call counters and timers. Enable with <see cref="Enabled"/>;
/// dump with <see cref="Report"/>. Designed for ad-hoc profiling, not
/// production use.
/// </summary>
public static class Perf
{
    public static bool Enabled = false;

    private static readonly Dictionary<string, long> _counts = new();
    private static readonly Dictionary<string, long> _ticks = new();

    public static void Inc(string key)
    {
        if (!Enabled) return;
        _counts.TryGetValue(key, out var v);
        _counts[key] = v + 1;
    }

    public static void Add(string key, long ticks)
    {
        if (!Enabled) return;
        _counts.TryGetValue(key, out var c);
        _counts[key] = c + 1;
        _ticks.TryGetValue(key, out var t);
        _ticks[key] = t + ticks;
    }

    public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0;
    public static void End(string key, long start)
    {
        if (!Enabled) return;
        Add(key, Stopwatch.GetTimestamp() - start);
    }

    public static void Reset()
    {
        _counts.Clear();
        _ticks.Clear();
    }

    public static string Report()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Perf report (count, total ms, ms/call)\n");
        var keys = new List<string>(_counts.Keys);
        keys.Sort();
        double freq = Stopwatch.Frequency;
        foreach (var k in keys)
        {
            long c = _counts[k];
            _ticks.TryGetValue(k, out var t);
            double ms = t / freq * 1000.0;
            double per = c > 0 ? ms / c : 0;
            sb.AppendFormat("  {0,-40} count={1,12:N0}  total={2,10:F1}ms  per={3,10:F4}ms\n", k, c, ms, per);
        }
        return sb.ToString();
    }
}
