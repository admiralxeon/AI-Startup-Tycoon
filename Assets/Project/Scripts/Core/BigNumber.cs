using System;

namespace AIStartupTycoon.Utils
{

    [Serializable]
    public struct BigNumber : IComparable<BigNumber>
    {
        public double Mantissa;
        public int Exponent;

        public static readonly BigNumber Zero = new BigNumber(0, 0);

        public BigNumber(double mantissa, int exponent)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize();
        }

        public static implicit operator BigNumber(double value)
        {
            if (value == 0) return Zero;
            int exp = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            double mant = value / Math.Pow(10, exp);
            return new BigNumber(mant, exp);
        }

        /// <summary>Keeps mantissa in [1, 10) range and normalizes zero.</summary>
        private void Normalize()
        {
            if (Mantissa == 0) { Exponent = 0; return; }

            while (Math.Abs(Mantissa) >= 10)
            {
                Mantissa /= 10;
                Exponent++;
            }
            while (Math.Abs(Mantissa) < 1)
            {
                Mantissa *= 10;
                Exponent--;
            }
        }

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.Mantissa == 0) return b;
            if (b.Mantissa == 0) return a;

            int expDiff = a.Exponent - b.Exponent;
            if (expDiff > 15) return a;   // b is negligibly small
            if (expDiff < -15) return b;  // a is negligibly small

            double aligned = expDiff >= 0
                ? a.Mantissa + b.Mantissa / Math.Pow(10, expDiff)
                : a.Mantissa * Math.Pow(10, expDiff) + b.Mantissa;

            int resultExp = expDiff >= 0 ? a.Exponent : b.Exponent;
            return new BigNumber(aligned, resultExp);
        }

        public static BigNumber operator -(BigNumber a, BigNumber b)
        {
            return a + new BigNumber(-b.Mantissa, b.Exponent);
        }

        public static BigNumber operator *(BigNumber a, double scalar)
        {
            return new BigNumber(a.Mantissa * scalar, a.Exponent);
        }

        public static BigNumber operator *(BigNumber a, BigNumber b)
        {
            return new BigNumber(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;

        public int CompareTo(BigNumber other)
        {
            if (Exponent != other.Exponent) return Exponent.CompareTo(other.Exponent);
            return Mantissa.CompareTo(other.Mantissa);
        }

        /// <summary>Formats as e.g. "1.23K", "4.56M", "7.89B", "1.20T", or raw number below 1000.</summary>
        public override string ToString()
        {
            if (Exponent < 3) return (Mantissa * Math.Pow(10, Exponent)).ToString("0.##");

            string[] suffixes = { "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
            int suffixIndex = (Exponent / 3) - 1;
            int remainder = Exponent % 3;
            double displayValue = Mantissa * Math.Pow(10, remainder);

            if (suffixIndex < 0 || suffixIndex >= suffixes.Length)
                return $"{Mantissa:0.##}e{Exponent}"; // fallback for absurdly large numbers

            return $"{displayValue:0.##}{suffixes[suffixIndex]}";
        }

        public double ToDouble() => Mantissa * Math.Pow(10, Exponent);
    }
}