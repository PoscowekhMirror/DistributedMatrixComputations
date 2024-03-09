using System.Numerics;

namespace Common.Vector.Operations;

public static class VectorSumExtensions
{
    public static int VectorParallelSumExecutionThreshold { set; get; } = 1_000_000;

    public static Vector<T> Sum<T>(this Vector<T> l, Vector<T> r)
        where T : INumber<T> 
        => l.Count >= VectorParallelSumExecutionThreshold 
            ? l.SumLinqParallel(r) 
            : l.SumForLoop(r);

    public static Vector<T> SumForLoop<T>(this Vector<T> l, Vector<T> r)
        where T : INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<T>(l.Count);

        for (int i = 0; i < l.Count; i++)
        {
            data.Add(l[i] + r[i]);
        }

        return new Vector<T>(data);
    }
    
    public static Vector<T> SumLinq<T>(this Vector<T> l, Vector<T> r)
        where T : INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<T>(l.Count);
        data.AddRange(
            l
                .GetValuesOnly()
                .Zip(r
                    .GetValuesOnly()
                )
                .Select(tuple => tuple.First + tuple.Second)
        );

        return new Vector<T>(data);
    }
    
    public static Vector<T> SumLinqParallel<T>(this Vector<T> l, Vector<T> r)
        where T : INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<T>(l.Count);
        data.AddRange(
            l
                .GetValuesOnly()
                .Zip(r
                    .GetValuesOnly()
                )
                .AsParallel()
                .Select(tuple => tuple.First + tuple.Second)
        );
        
        return new Vector<T>(data);
    }

    
    // TODO: optimize
    public static Vector<T> Sum<T>(this Vector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return new Vector<T>(
            l
                .Union(r)
                .GroupBy(e => e.Index, e => e.Value)
                .OrderBy(e => e.Key)
                .Select(
                    g => g.Aggregate(T.Zero, (sum, e) => sum + e)
                )
        );
    }

    public static Vector<T> Sum<T>(this SparseVector<T> l, Vector<T> r)
        where T : struct, INumber<T> 
        => r.Sum(l);


    public static int SparseVectorParallelSumExecutionThreshold { set; get; } = 1_000_000;
    
    public static SparseVector<T> Sum<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T> 
        => l.Count >= SparseVectorParallelSumExecutionThreshold 
            ? l.SumLinqParallel(r) 
            : l.SumEnumSorted(r);
    
    public static SparseVector<T> SumLinq<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return new SparseVector<T>(
            l.Count
            ,l
                .Union(r)
                .GroupBy(e => e.Index, e => e.Value)
                .Select(g => 
                    new IndexedElement<T>(
                        g.Key
                        ,g.Aggregate(T.Zero, (sum, e) => sum + e)
                    )
                )
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index)
        );
    }

    public static SparseVector<T> SumEnumSorted<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        if (l.NonZeroCount < 1)
        {
            return r;
        }
        if (r.NonZeroCount < 1)
        {
            return l;
        }
        
        var elements = new List<IndexedElement<T>>(
            Math.Min(l.Count, l.NonZeroCount + r.NonZeroCount)
        );

        using var lEnum = l/*.OrderBy(e => e.Index)*/.GetEnumerator();
        using var rEnum = r/*.OrderBy(e => e.Index)*/.GetEnumerator();

        IEnumerator<IndexedElement<T>>? leftToEnumerate = null;

        bool lEnumMoved = lEnum.MoveNext();
        bool rEnumMoved = rEnum.MoveNext();
        
        while (lEnumMoved && rEnumMoved)
        {
            var lEnumCur = lEnum.Current;
            var rEnumCur = rEnum.Current;
            
            if (lEnumCur.Index == rEnumCur.Index)
            {
                var sum = lEnumCur.Value + rEnumCur.Value;
                if (sum != T.Zero)
                {
                    elements.Add(new IndexedElement<T>(lEnumCur.Index, sum));    
                }
                
                lEnumMoved = lEnum.MoveNext();
                rEnumMoved = rEnum.MoveNext();
                
                if (!lEnumMoved)
                {
                    leftToEnumerate = rEnum;
                    break;
                }  
                if (!rEnumMoved)
                {
                    leftToEnumerate = lEnum;
                    break;
                }
            } 
            
            else if (lEnumCur.Index < rEnumCur.Index)
            {
                elements.Add(lEnumCur);
                lEnumMoved = lEnum.MoveNext();
                if (!lEnumMoved)
                {
                    leftToEnumerate = rEnum;
                    break;
                }  
            }
            
            else// if (lEnumCur.Index > rEnumCur.Index)
            {
                elements.Add(rEnumCur);
                rEnumMoved = rEnum.MoveNext();
                if (!rEnumMoved)
                {
                    leftToEnumerate = lEnum;
                    break;
                }  
            }
        }

        if (leftToEnumerate is not null)
        {
            while (leftToEnumerate.MoveNext())
            {
                elements.Add(leftToEnumerate.Current);
            }
        }
        
        return new SparseVector<T>(l.Count, elements);
    }
    
    public static SparseVector<T> SumForLoopSorted<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        if (l.NonZeroCount < 1)
        {
            return r;
        }
        if (r.NonZeroCount < 1)
        {
            return l;
        }
        
        var elements = new List<IndexedElement<T>>(
            Math.Min(l.Count, l.NonZeroCount + r.NonZeroCount)
        );
        
        var lElements = l.IndexedElements;
        var rElements = r.IndexedElements;
        
        var lLastIndex = 0;
        var rLastIndex = 0;
        
        for (; lLastIndex < lElements.Count; lLastIndex++)
        {
            var lElement = lElements[lLastIndex];
            
            if (lElement.Index < rLastIndex)
            {
                elements.Add(lElement);
                lLastIndex += 1;
                break;
            }
            
            for (int i = rLastIndex; i < rElements.Count; ++i)
            {
                var rElement = rElements[i];
                if (rElement.Index < lElement.Index)
                {
                    elements.Add(rElement);
                } 
                else 
                {
                    if (rElement.Index == lElement.Index)
                    {
                        var sum = lElement.Value + rElement.Value;
                        if (sum != T.Zero)
                        {
                            elements.Add(new IndexedElement<T>(rElement.Index, sum));
                        }
                    }
                    else
                    {
                        rLastIndex = i;
                        break;
                    }
                }
                rLastIndex = i;
            }
            
        }

        for (int i = rLastIndex; i < rElements.Count; i++)
        {
            elements.Add(rElements[i]);
        }
        
        return new SparseVector<T>(l.Count, elements);
    }
    
    public static SparseVector<T> SumLinqParallel<T>(this SparseVector<T> l, SparseVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<IndexedElement<T>>(l.NonZeroCount + r.NonZeroCount);
        data.AddRange(
            l
                .Union(r)
                .AsParallel()
                .GroupBy(e => e.Index, e => e.Value)
                .Select(g => 
                    new IndexedElement<T>(
                        g.Key
                        ,g.Aggregate(T.Zero, (sum, e) => sum + e)
                    )
                )
                .Where(e => e.Value != T.Zero)
                .OrderBy(e => e.Index)
        );
        
        return new SparseVector<T>(l.Count, data);
    }
    
    public static IVector<T> Sum<T>(IVector<T> l, IVector<T> r)
        where T : struct, INumber<T> 
        => (l is Vector<T>, r is Vector<T>) switch
        {
            (true , true ) => (l as       Vector<T>).Sum(r as       Vector<T>),
            (true , false) => (l as       Vector<T>).Sum(r as SparseVector<T>),
            (false, true ) => (l as SparseVector<T>).Sum(r as       Vector<T>),
            (false, false) => (l as SparseVector<T>).Sum(r as SparseVector<T>)
        };
}