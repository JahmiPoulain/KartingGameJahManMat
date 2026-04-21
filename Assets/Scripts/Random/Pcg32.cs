using System;
using System.Security.Cryptography;

public class Pcg32
{
    // Etat interne (équivalent struct C)
    private ulong state;
    private ulong inc;

    // Constante LCG
    private const ulong PCG32_MULT = 6364136223846793005UL;

    // -------- INITIALISATION --------
    public void Init()
    {
        // Génère 128 bits aléatoires
        // Cryptographically Secure Pseudo-Random Number Generator
        byte[] buffer = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }

        ulong initstate = BitConverter.ToUInt64(buffer, 0);
        ulong initseq   = BitConverter.ToUInt64(buffer, 8);

        Seed(initstate, initseq);
    }

    // -------- SEED --------
    public void Seed(ulong initstate, ulong initseq)
    {
        // On s'assure de commencer avec un état connu pour éviter les effets imprévisibles
        state = 0;

        // On force le nombre de la séquence à être impair
        // Pour ça on décale tous les bits vers la gauche, puis on force le bit de droite à 1
        inc = (initseq << 1) | 1;

        // On fait évoluer l'état du générateur une première fois
        NextUInt();

        // On injecte maintenant initstate pour décorréler state et inc.
        state += initstate;

        // On fait évoluer l'état une deuxième fois 
        NextUInt();
    }

    // -------- PCG32 ALGORYTHME --------
    // Retourne un entier non signé 32 bits : 0 → 4 294 967 295
    public uint NextUInt()
    {
        ulong oldstate = state;

        // LCG 64 bits
        state = oldstate * PCG32_MULT + inc;

        // Transformation PCG
        uint xorshifted = (uint)(((oldstate >> 18) ^ oldstate) >> 27);
        uint rot = (uint)(oldstate >> 59);

        return (xorshifted >> (int)rot) | (xorshifted << (int)((-rot) & 31));
    }

    // -------- RANGE [0, max[ --------
    public uint Range(uint max)
    {
        // Si la valeur transmise est 0, on renvoie juste 0
        if (max == 0) return 0;

        uint threshold = (uint)(-max % max);
        uint r;

        do
        {
            r = NextUInt();
        } while (r < threshold);

        return r % max;
    }

    // -------- RANGE [min, max[ --------
    public int Range(int min, int max)
    {
        // Si max est plus petit que min, on renvoie juste min
        if (max <= min) return min;

        return min + (int)Range((uint)(max - min));
    }

    // -------- FLOAT [0,1[ --------
    public float NextFloat()
    {
        // On enlève les 8 bits de poids faible pour en garder 24 / 32
        // Car un float IEEE 754 = 23 bits de mantisse + 1 implicite
        // ON fait la division de avec 16777216 qui vaut 2^24
        return (NextUInt() >> 8) * (1.0f / 16777216.0f);
    }
}