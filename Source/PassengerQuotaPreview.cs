// Signature: OpenAI-Codex
using System;
using System.Collections.Generic;
using System.Numerics;
namespace RealisticBorderControl
{
    public static class PassengerQuotaPreview
    {
        public static IDictionary<string, int> Allocate(int budget, IDictionary<string, decimal> weights)
        {
            if (budget < 0) throw new ArgumentOutOfRangeException("budget");
            if (weights == null) throw new ArgumentNullException("weights");
            var units = new SortedDictionary<string, BigInteger>(StringComparer.Ordinal);
            BigInteger total = BigInteger.Zero;
            foreach (var pair in weights)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0)
                    throw new ArgumentException("Connection keys must be nonempty and weights nonnegative.");
                int[] bits = decimal.GetBits(pair.Value);
                BigInteger raw = (BigInteger)(uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
                int scale = (bits[3] >> 16) & 255;
                BigInteger value = raw * BigInteger.Pow(10, 28 - scale);
                units.Add(pair.Key, value);
                total += value;
            }
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (total.IsZero) return result;
            var remainders = new List<KeyValuePair<string, BigInteger>>();
            int assigned = 0;
            foreach (var pair in units)
            {
                BigInteger remainder;
                int quota = (int)BigInteger.DivRem(budget * pair.Value, total, out remainder);
                result.Add(pair.Key, quota);
                assigned += quota;
                remainders.Add(new KeyValuePair<string, BigInteger>(pair.Key, remainder));
            }
            remainders.Sort((a, b) => {
                int order = b.Value.CompareTo(a.Value);
                return order != 0 ? order : StringComparer.Ordinal.Compare(a.Key, b.Key);
            });
            for (int i = 0; i < budget - assigned; i++) result[remainders[i].Key]++;
            return result;
        }
        public static long? ObservedPassengers { get { return null; } }
        public static string DataSource { get { return "Unknown: game observation is not connected."; } }
    }
}
