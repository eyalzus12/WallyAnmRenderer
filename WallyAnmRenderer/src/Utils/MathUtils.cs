using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace WallyAnmRenderer;

public static class MathUtils
{
    public static long SafeMod(long x, long m)
    {
        x %= m;
        if (x < 0) x += m;
        return x;
    }

    private const double DELETION_COST = 2;
    private const double INSERTION_COST = 1;
    private const double SUBSTITUTION_COST = 2;
    private const int STACKALLOC_CAP = 1024;
    public static double LevenshteinDistance(scoped ReadOnlySpan<char> s, scoped ReadOnlySpan<char> t)
    {
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;
        if (s == t) return 0;

        int n = s.Length;
        int m = t.Length;

        bool canStackAlloc = m + 1 <= STACKALLOC_CAP;
        Span<double> v0 = canStackAlloc ? stackalloc double[STACKALLOC_CAP] : new double[m + 1];
        Span<double> v1 = canStackAlloc ? stackalloc double[STACKALLOC_CAP] : new double[m + 1];
        for (int i = 0; i <= m; i++)
            v0[i] = i;

        for (int i = 0; i < n; i++)
        {
            v1[0] = i + 1;
            for (int j = 0; j < m; j++)
            {
                double deletion = v0[j + 1] + DELETION_COST;
                double insertion = v1[j] + INSERTION_COST;
                bool canSubstitute = CompareCharsIgnoreCase(s[i], t[j]) != 0;
                double substitution = v0[j] + (canSubstitute ? SUBSTITUTION_COST : 0);

                v1[j + 1] = Math.Min(Math.Min(deletion, insertion), substitution);
            }
            // swap
            v1.CopyTo(v0);
        }
        return v0[m];
    }

    public static IEnumerable<(T, double)> FuzzySearch<T>(
        string query,
        IEnumerable<T> candidates,
        Func<T, string> selector,
        int maxDistance = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        return candidates
                .Select(c => (obj: c, str: selector(c)))
                // compute distance
                .Select(c => (c.obj, c.str, distance: LevenshteinDistance(query, c.str)))
                // take into account length
                //.Select(c => (c.obj, score: ))
                // apply threshold
                .Where(c => c.distance <= maxDistance)
                // sort
                .OrderBy(c => c.distance)
                .Select(c => (c.obj, c.distance));
        //.Select(c => c.obj);
    }

    public static int CompareCharsIgnoreCase(char a, char b)
    {
        ReadOnlySpan<char> aSpan = MemoryMarshal.CreateReadOnlySpan(ref a, 1);
        ReadOnlySpan<char> bSpan = MemoryMarshal.CreateReadOnlySpan(ref b, 1);
        return aSpan.CompareTo(bSpan, StringComparison.CurrentCultureIgnoreCase);
    }
}