public static class GlobalRng
{
    private static readonly Pcg32 rng = new();

    static GlobalRng()
    {
        // Initialisation automatique pour éviter d'oublier Init()
        rng.Init();
    }

    public static uint NextUInt()
    {
        return rng.NextUInt();
    }

    // -------- RANGE [min, max[ --------
    public static int Range(int min, int max)
    {
        return rng.Range(min, max);
    }

    public static float NextFloat()
    {
        return rng.NextFloat();
    }
}