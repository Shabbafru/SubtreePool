using System;
using System.Collections.Generic;
using System.Linq;

public static class FeatureMapping
{
    // Compute MMD between two 1D samples (single columns).
    public static double Compute1D(double[] x, double[] y, double? gamma = null)
    {
        int m = x.Length;
        int n = y.Length;

        // Auto gamma: median heuristic
        double g = gamma ?? ComputeMedianGamma(x, y);

        // K(X, X) term 
        double sumKxx = 0;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < m; j++)
                if (i != j)
                    sumKxx += RbfKernel1D(x[i], x[j], g);

        // K(Y, Y) term 
        double sumKyy = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j)
                    sumKyy += RbfKernel1D(y[i], y[j], g);

        // K(X, Y) term
        double sumKxy = 0;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                sumKxy += RbfKernel1D(x[i], y[j], g);

        double mmdSq = sumKxx / (m * (m - 1))
                     + sumKyy / (n * (n - 1))
                     - 2 * sumKxy / (m * n);

        return Math.Sqrt(Math.Max(0, mmdSq));
    }

    // Compare all numeric columns between two datasets.
    // Returns MMD score for each column, sorted by largest difference.
    public static (string Column, double MMD)[] CompareAllColumns(
        double[][] dataset1, double[][] dataset2, string[] columnNames)
    {
        var results = new (string, double)[columnNames.Length];

        for (int col = 0; col < columnNames.Length; col++)
        {
            double[] col1 = dataset1.Select(row => row[col]).ToArray();
            double[] col2 = dataset2.Select(row => row[col]).ToArray();
            results[col] = (columnNames[col], Compute1D(col1, col2));
        }

        return results.OrderByDescending(r => r.Item2).ToArray();
    }

    private static double RbfKernel1D(double a, double b, double gamma)
    {
        double diff = a - b;
        return Math.Exp(-gamma * diff * diff);
    }

    private static double ComputeMedianGamma(double[] x, double[] y)
    {
        var allData = x.Concat(y).ToArray();
        var distances = new List<double>();

        // sample pairwise distances 
        int maxSamples = Math.Min(1000, allData.Length);
        var rng = new Random();
        var sampled = allData.OrderBy(_ => rng.Next()).Take(maxSamples).ToArray();

        for (int i = 0; i < sampled.Length; i++)
            for (int j = i + 1; j < sampled.Length; j++)
                distances.Add(Math.Abs(sampled[i] - sampled[j]));

        distances.Sort();
        double median = distances[distances.Count / 2];

        return 1.0 / (2 * median * median + 1e-10);
    }

    public static double Wasserstein1D(double[] x, double[] y)
    {
        var xSorted = x.OrderBy(v => v).ToArray();
        var ySorted = y.OrderBy(v => v).ToArray();

        // quantile-based approximation
        int n = Math.Min(x.Length, y.Length);
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            double qx = xSorted[(int)((double)i / n * x.Length)];
            double qy = ySorted[(int)((double)i / n * y.Length)];
            sum += Math.Abs(qx - qy);
        }
        return sum / n;
    }
}

