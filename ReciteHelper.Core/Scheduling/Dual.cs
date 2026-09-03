using System.Runtime.CompilerServices;

namespace ReciteHelper.Core.Scheduling;

/// <summary>Gradient with respect to the 21 scheduler parameters (inline, no heap allocation).</summary>
[InlineArray(SchedulerParameters.Count)]
public struct Grad
{
    private double _element0;
}

/// <summary>
/// Forward-mode automatic-differentiation number: a value together with its
/// partial derivatives with respect to every scheduler parameter.  All operators
/// are exact (no finite differences), so gradients are accurate to rounding error.
/// </summary>
public readonly struct Dual
{
    public readonly double V;
    public readonly Grad G;

    public Dual(double v)
    {
        V = v;
        G = default;
    }

    public Dual(double v, in Grad g)
    {
        V = v;
        G = g;
    }

    /// <summary>The parameter with the given index, seeded with a unit derivative.</summary>
    public static Dual Parameter(double value, int index)
    {
        var g = new Grad();
        g[index] = 1.0;
        return new Dual(value, g);
    }

    public static implicit operator Dual(double v) => new(v);

    public static Dual operator +(in Dual a, in Dual b)
    {
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = a.G[i] + b.G[i];
        return new Dual(a.V + b.V, g);
    }

    public static Dual operator -(in Dual a, in Dual b)
    {
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = a.G[i] - b.G[i];
        return new Dual(a.V - b.V, g);
    }

    public static Dual operator -(in Dual a)
    {
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = -a.G[i];
        return new Dual(-a.V, g);
    }

    public static Dual operator *(in Dual a, in Dual b)
    {
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = a.G[i] * b.V + a.V * b.G[i];
        return new Dual(a.V * b.V, g);
    }

    public static Dual operator /(in Dual a, in Dual b)
    {
        var inv = 1.0 / b.V;
        var q = a.V * inv;
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = (a.G[i] - q * b.G[i]) * inv;
        return new Dual(q, g);
    }

    public static Dual Exp(in Dual a)
    {
        var e = Math.Exp(a.V);
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = a.G[i] * e;
        return new Dual(e, g);
    }

    public static Dual Log(in Dual a)
    {
        var inv = 1.0 / a.V;
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = a.G[i] * inv;
        return new Dual(Math.Log(a.V), g);
    }

    /// <summary>a^b for a &gt; 0.</summary>
    public static Dual Pow(in Dual a, in Dual b)
    {
        var v = Math.Pow(a.V, b.V);
        var da = b.V * v / a.V;          // ∂/∂a
        var db = v * Math.Log(a.V);      // ∂/∂b
        var g = new Grad();
        for (var i = 0; i < SchedulerParameters.Count; i++) g[i] = da * a.G[i] + db * b.G[i];
        return new Dual(v, g);
    }

    public static Dual Min(in Dual a, in Dual b) => a.V <= b.V ? a : b;

    public static Dual Max(in Dual a, in Dual b) => a.V >= b.V ? a : b;

    /// <summary>Clamps the value; derivatives vanish where the clamp is active.</summary>
    public static Dual Clamp(in Dual a, double lo, double hi)
    {
        if (a.V < lo) return new Dual(lo);
        if (a.V > hi) return new Dual(hi);
        return a;
    }
}
