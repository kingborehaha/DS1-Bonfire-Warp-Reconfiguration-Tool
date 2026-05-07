using System.Numerics;


namespace BoreSoulsResource
{
    public static class UtilMath
    {

        public static bool Vector3IsOne(Vector3 v1)
        {
            return Vector3IsEqual(v1, new Vector3(1.0f, 1.0f, 1.0f));
        }

        public static bool Vector3IsEqual(Vector3 v1, Vector3 v2)
        {
            if (Vector3.Distance(v1, v2) < 0.001f)
            {
                return true;
            }
            return false;
        }

        public static bool IsEqualish(this Vector3 v1, Vector3 v2)
        {
            if (Vector3.Distance(v1, v2) < 0.001f)
            {
                return true;
            }
            return false;
        }

        public static bool FloatIsEqual(float f1, float f2)
        {
            if (Math.Abs(f1 - f2) < 0.001f)
            {
                return true;
            }
            return false;
        }

        public static bool IsEqualish(this float f1, float f2)
        {
            if (Math.Abs(f1 - f2) < 0.001f)
            {
                return true;
            }
            return false;
        }

        public class RngFloatIncremental
        {
            public float ValueMin { get; set; }
            public float ValueMax { get; set; }

            public float IncrementAmount { get; set; }
            public int IncrementOdds { get; set; }

            public RngFloatIncremental(float min, float max, int incrementOdds = 50, float incrementAmount = .1f)
            {
                ValueMin = min;
                ValueMax = max;
                IncrementAmount = incrementAmount;
                IncrementOdds = incrementOdds;
            }

            public float GetValue(Random rng)
            {
                var value = ValueMin;
                while (rng.Next(1, 101) <= IncrementOdds)
                {
                    value += IncrementAmount;
                    if (value >= ValueMax)
                        return ValueMax;
                }

                return value;
            }

            public override string ToString()
            {
                return $"{ValueMin}-{ValueMax}, {IncrementOdds}%";
            }
        }

        public class RngIntIncremental
        {
            public int ValueMin { get; set; }
            public int ValueMax { get; set; }

            public int IncrementOdds { get; set; }

            public RngIntIncremental(int min, int max, int incrementOdds = 50)
            {
                ValueMin = min;
                ValueMax = max;
                IncrementOdds = incrementOdds;
            }

            public int GetValue(Random rng)
            {
                var value = ValueMin;
                while (rng.Next(1, 101) <= IncrementOdds)
                {
                    value++;
                    if (value >= ValueMax)
                        return ValueMax;
                }

                return value;
            }

            public override string ToString()
            {
                return $"{ValueMin}-{ValueMax}, {IncrementOdds}%";
            }
        }
    }
}
