// Signature: OpenAI-Codex_Realistic_City_Time
// Signature: OpenAI-Codex
using System;

namespace RealisticCityTime.Game
{
    /// <summary>
    /// Exact pure tick scaling without BigInteger or game references. This class does not read
    /// observations, manage sessions, select a precision policy, or write a game clock.
    /// Callers must serialize access to each instance.
    /// </summary>
    internal sealed class PortableClockScaler
    {
        private readonly int precision;
        private decimal remainder;

        public PortableClockScaler(int precision)
        {
            if (precision < 0 || precision > 28) throw new ArgumentOutOfRangeException("precision");
            this.precision = precision;
        }

        // Synthetic planning can discard a failed or stale proposal without advancing its remainder.
        internal PortableClockScaler Copy()
        {
            PortableClockScaler copy = new PortableClockScaler(precision);
            copy.remainder = remainder;
            return copy;
        }

        public long Advance(long elapsedTicks, decimal multiplier)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException("elapsedTicks");
            if (multiplier < 1m || multiplier > 10m || decimal.Round(multiplier, precision) != multiplier)
                throw new ArgumentException("Multiplier must satisfy the supplied 1–10 precision policy.", "multiplier");

            // Keep every fractional intermediate below 2. Even at decimal scale 28 its
            // coefficient fits within 96 bits, avoiding decimal multiplication rounding.
            long factorWhole = (long)decimal.Truncate(multiplier);
            decimal factorFraction = multiplier - factorWhole;
            long resultWhole = 0;
            decimal resultFraction = remainder;
            ulong remaining = (ulong)elapsedTicks;
            while (remaining != 0)
            {
                if ((remaining & 1UL) != 0)
                {
                    resultWhole = checked(resultWhole + factorWhole);
                    resultFraction += factorFraction;
                    if (resultFraction >= 1m)
                    {
                        resultWhole = checked(resultWhole + 1);
                        resultFraction -= 1m;
                    }
                }
                remaining >>= 1;
                if (remaining == 0) break;
                factorWhole = checked(factorWhole * 2);
                factorFraction += factorFraction;
                if (factorFraction >= 1m)
                {
                    factorWhole = checked(factorWhole + 1);
                    factorFraction -= 1m;
                }
            }

            // Commit only after every operation succeeded; overflow keeps the prior remainder.
            remainder = resultFraction;
            return resultWhole;
        }

        public void Reset() { remainder = 0m; }
    }
}
