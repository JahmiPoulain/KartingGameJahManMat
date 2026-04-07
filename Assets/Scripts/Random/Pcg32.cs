using System;
using System.Runtime.InteropServices;
using UnityEngine;

public struct Pcg32
{
    private ulong state;
    private ulong inc;

    private const ulong PCG32_MULT = 6364136223846793005UL;

    /// <summary>
    /// Initialise le générateur avec une graine (state) et une séquence (inc).
    /// </summary>
    public void Seed(ulong initstate, ulong initseq)
    {
        state = 0U;
        inc = (initseq << 1) | 1U;
        Next();
        unchecked { state += initstate; }
        Next();
    }

    /// <summary>
    /// Génère un nombre aléatoire 32-bit (équivalent de pcg32)
    /// </summary>
    public uint Next()
    {
        unchecked
        {
            ulong oldstate = state;
            state = oldstate * PCG32_MULT + inc;

            uint xorshifted = (uint)(((oldstate >> 18) ^ oldstate) >> 27);
            int rot = (int)(oldstate >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }
    }

    #region Helpers (Gamme de nombres)

    // [0, max[
    public uint Range(uint max)
    {
        if (max == 0) return 0;
        uint threshold = (uint)((-(int)max) % max);
        uint r;
        do { r = Next(); } while (r < threshold);
        return r % max;
    }

    // [min, max[
    public int Range(int min, int max)
    {
        if (max <= min) return min;
        return min + (int)Range((uint)(max - min));
    }

    // [0.0f, 1.0f[
    public float NextFloat()
    {
        return (Next() >> 8) * (1.0f / 16777216.0f);
    }

    #endregion

    #region Initialisations (Équivalents C)

    /// <summary>
    /// Méthode recommandée pour Unity : mélange l'heure, le temps écoulé et l'ID d'instance.
    /// </summary>
    public static Pcg32 CreateCombined()
    {
        Pcg32 rng = new Pcg32();
        
        // On récupère des données qui varient
        ulong t = (ulong)DateTime.Now.Ticks;
        ulong clock = (ulong)(Time.realtimeSinceStartupAsDouble * 1000000);
        
        // Utilisation de System.Security.Cryptography pour une graine "vraiment" aléatoire (équivalent v4 /dev/urandom)
        byte[] buffer = new byte[16];
        using (var crypto = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            crypto.GetBytes(buffer);
        }

        ulong s1 = BitConverter.ToUInt64(buffer, 0);
        ulong s2 = BitConverter.ToUInt64(buffer, 8);

        rng.Seed(s1 ^ t, s2 ^ clock);
        return rng;
    }

    #endregion
}