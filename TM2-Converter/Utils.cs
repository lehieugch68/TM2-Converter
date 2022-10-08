using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TM2_Converter
{
    internal static class Utils
    {
        public static double CosineSimilarity(int[] a, int[] b)
        {
            double dotProduct = 0;
            double mA = 0, mB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                mA += a[i] * a[i];
                mB += b[i] * b[i];
            }
            double cosine = dotProduct / (Math.Sqrt(mA) * Math.Sqrt(mB));
            return cosine;
        }
        public static double EuclideanDistance(int[] a, int[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Pow(a[i] - b[i], 2);
            }
            double distance = Math.Sqrt(sum);
            return distance;
        }
    }
}
