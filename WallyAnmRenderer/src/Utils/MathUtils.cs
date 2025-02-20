namespace WallyAnmRenderer;

public static class MathUtils
{
    public static long SafeMod(long x, long m)
    {
        x %= m;
        if (x < 0) x += m;
        return x;
    }
}