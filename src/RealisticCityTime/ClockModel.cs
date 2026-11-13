// Signature: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex
using System;
using System.Globalization;
using System.Numerics;

namespace RealisticCityTime
{
    /// <summary>Pure conversion of eligible TimeSpan ticks; never reads or writes a game clock.</summary>
    public sealed class ClockModel
    {
        public const decimal DefaultMultiplier = 1m;
        private readonly int precision;
        private readonly BigInteger denominator;
        private BigInteger remainder;

        // Precision is an explicit arithmetic policy, not a UI or product default.
        public ClockModel(int precision)
        {
            if (precision < 0 || precision > 28)
                throw new ArgumentOutOfRangeException(nameof(precision));
            this.precision = precision;
            denominator = BigInteger.Pow(10, precision);
        }

        /// <summary>Parses invariant unsigned decimal text without silently rounding excess precision.</summary>
        public decimal ParseMultiplier(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("A decimal multiplier is required.", nameof(text));

            int point = -1;
            int digits = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' && point < 0) { point = i; continue; }
                if (c < '0' || c > '9')
                    throw new ArgumentException("Use unsigned invariant decimal notation.", nameof(text));
                digits++;
            }

            int end = text.Length;
            if (point >= 0)
                while (end > point + 1 && text[end - 1] == '0') end--;
            if (digits == 0 || (point >= 0 && end - point - 1 > precision))
                throw new ArgumentException("Input exceeds the supplied decimal precision policy.", nameof(text));

            decimal multiplier;
            if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out multiplier))
                throw new ArgumentException("Invalid decimal multiplier.", nameof(text));
            // Decimal parsing can round a 29-significant-digit input even when its scale is valid.
            BigInteger inputCoefficient = BigInteger.Zero;
            for (int i = 0; i < end; i++)
                if (text[i] != '.') inputCoefficient = inputCoefficient * 10 + (text[i] - '0');
            int inputScale = point < 0 ? 0 : end - point - 1;
            int[] parsedBits = decimal.GetBits(multiplier);
            int parsedScale = (parsedBits[3] >> 16) & 255;
            BigInteger parsedCoefficient = (BigInteger)(uint)parsedBits[0]
                + ((BigInteger)(uint)parsedBits[1] << 32)
                + ((BigInteger)(uint)parsedBits[2] << 64);
            if (inputCoefficient * BigInteger.Pow(10, parsedScale)
                != parsedCoefficient * BigInteger.Pow(10, inputScale))
                throw new ArgumentException("Input cannot be represented exactly as a decimal multiplier.", nameof(text));
            Validate(multiplier);
            return multiplier;
        }

        /// <summary>Returns whole desired game ticks and retains the exact fractional tick remainder.</summary>
        public long Advance(long elapsedTicks, decimal multiplier)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            Validate(multiplier);

            int[] bits = decimal.GetBits(multiplier);
            int scale = (bits[3] >> 16) & 255;
            BigInteger coefficient = (BigInteger)(uint)bits[0]
                + ((BigInteger)(uint)bits[1] << 32)
                + ((BigInteger)(uint)bits[2] << 64);
            // Trailing decimal zeros may make the stored scale exceed the accepted effective precision.
            BigInteger units = scale <= precision
                ? coefficient * BigInteger.Pow(10, precision - scale)
                : coefficient / BigInteger.Pow(10, scale - precision);

            BigInteger nextRemainder;
            BigInteger whole = BigInteger.DivRem(elapsedTicks * units + remainder, denominator, out nextRemainder);
            long result = (long)whole; // Overflow must leave the existing remainder untouched.
            remainder = nextRemainder;
            return result;
        }

        /// <summary>Clears fractional ticks only; lifecycle policy belongs to a future adapter.</summary>
        public void Reset() { remainder = BigInteger.Zero; }

        // Shared with the observation boundary so invalid baseline multipliers fail closed.
        internal void Validate(decimal multiplier)
        {
            if (multiplier < 1m || multiplier > 10m)
                throw new ArgumentException("Multiplier must be between 1 and 10 inclusive.", nameof(multiplier));
            if (decimal.Round(multiplier, precision) != multiplier)
                throw new ArgumentException("Multiplier exceeds the supplied decimal precision policy.", nameof(multiplier));
        }
    }
}
