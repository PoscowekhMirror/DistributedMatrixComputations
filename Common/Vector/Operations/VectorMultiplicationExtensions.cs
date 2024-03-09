using System.Numerics;

namespace Common.Vector.Operations;

public static class VectorMultiplicationExtensions
{
    public static T Multiply<T>(this Vector<T> l, Vector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return 
            Enumerable
                .Range(0, l.Count)
                .Select(i => l[i] * r[i])
                .Aggregate((sum, e) => sum + e);
    }
    
    public static T Multiply<T>(this Vector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        using var enumerator = r.GetEnumerator(false);
        var sum = T.Zero;
        
        while (enumerator.MoveNext())
        {
            var element = enumerator.Current;
            sum += l[element.Index] * element.Value;
        }

        return sum;
    }

    public static T Multiply<T>(this SparseVector<T> l, Vector<T> r)
        where T : struct, INumber<T>
    {
        return r.Multiply(l);
    }
    
    public static T Multiply<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return l
            .Union(r)
            .GroupBy(e => e.Index, e => e.Value)
            .Where(g => g.Count() == 2)
            .Select(g => g.Aggregate(T.One, (prod, e) => prod * e))
            .Aggregate(T.Zero, (sum, e) => sum + e);
    }

    public static T Multiply<T>(IVector<T> l, IVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }
        
        switch (l is Vector<T>, r is Vector<T>)
        {
            case (true  ,true ): return (l as       Vector<T>).Multiply(r as       Vector<T>);
            case (true  ,false): return (l as       Vector<T>).Multiply(r as SparseVector<T>);
            case (false ,true ): return (l as SparseVector<T>).Multiply(r as       Vector<T>);
            case (false ,false): return (l as SparseVector<T>).Multiply(r as SparseVector<T>);
        }
    }
}