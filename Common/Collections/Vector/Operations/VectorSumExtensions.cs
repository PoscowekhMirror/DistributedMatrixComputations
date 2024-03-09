using System.Numerics;
using Common.Collections.Element;
using Common.Collections.Vector.Sparse.Indexed;

namespace Common.Collections.Vector.Operations;

public static class VectorSumExtensions
{
    public static int VectorParallelSumExecutionThreshold { set; get; } = 1_000_000;

    public static Regular.Vector<T> Sum<T>(this Regular.Vector<T> l, Regular.Vector<T> r)
        where T : INumber<T> 
        => l.Count >= VectorParallelSumExecutionThreshold 
            ? l.SumLinqParallel(r) 
            : l.SumForLoop(r);

    public static Regular.Vector<T> SumForLoop<T>(this Regular.Vector<T> l, Regular.Vector<T> r)
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

        return new Regular.Vector<T>(data);
    }
    
    public static Regular.Vector<T> SumLinq<T>(this Regular.Vector<T> l, Regular.Vector<T> r)
        where T : INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<T>(l.Count);
        data.AddRange(
            l
                .GetValuesOnly(true)
                .Zip(r
                    .GetValuesOnly(true)
                )
                .Select(tuple => tuple.First + tuple.Second)
        );

        return new Regular.Vector<T>(data);
    }
    
    public static Regular.Vector<T> SumLinqParallel<T>(this Regular.Vector<T> l, Regular.Vector<T> r)
        where T : INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<T>(l.Count);
        data.AddRange(
            l
                .GetValuesOnly(true)
                .Zip(r
                    .GetValuesOnly(true)
                )
                .AsParallel()
                .Select(tuple => tuple.First + tuple.Second)
        );
        
        return new Regular.Vector<T>(data);
    }

    
    // TODO: optimize
    public static Regular.Vector<T> Sum<T>(this Regular.Vector<T> l, SparseIndexedVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return new Regular.Vector<T>(
            l
                .GetIndexedElements(true)
                .Union(r
                    .GetIndexedElements(false)
                )
                .GroupBy(e => e.Index, e => e.Value)
                .OrderBy(e => e.Key)
                .Select(
                    g => g.Aggregate(T.Zero, (sum, e) => sum + e)
                )
        );
    }

    public static Regular.Vector<T> Sum<T>(this SparseIndexedVector<T> l, Regular.Vector<T> r)
        where T : struct, INumber<T> 
        => r.Sum(l);


    public static int SparseVectorParallelSumExecutionThreshold { set; get; } = 1_000_000;
    
    public static SparseIndexedVector<T> Sum<T>(this SparseIndexedVector<T> l, SparseIndexedVector<T> r)
        where T : struct, INumber<T> 
        => l.Count >= SparseVectorParallelSumExecutionThreshold 
            ? l.SumLinqParallel(r) 
            : l.SumEnumSorted(r);
    
    public static SparseIndexedVector<T> SumLinq<T>(this SparseIndexedVector<T> l, SparseIndexedVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        return new SparseIndexedVector<T>(
            l.Count
            ,l
                .GetIndexedElements(false)
                .Union(r
                    .GetIndexedElements(false)
                )
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

    public static SparseIndexedVector<T> SumEnumSorted<T>(this SparseIndexedVector<T> l, SparseIndexedVector<T> r)
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

        using var lEnum = l.GetIndexedElements(false)/*.OrderBy(e => e.Index)*/.GetEnumerator();
        using var rEnum = r.GetIndexedElements(false)/*.OrderBy(e => e.Index)*/.GetEnumerator();

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
        
        return new SparseIndexedVector<T>(l.Count, elements);
    }
    
    public static SparseIndexedVector<T> SumForLoopSorted<T>(this SparseIndexedVector<T> l, SparseIndexedVector<T> r)
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
        
        return new SparseIndexedVector<T>(l.Count, elements);
    }
    
    public static SparseIndexedVector<T> SumLinqParallel<T>(this SparseIndexedVector<T> l, SparseIndexedVector<T> r)
        where T : struct, INumber<T>
    {
        if (l.Count != r.Count)
        {
            throw new ArgumentException();
        }

        var data = new List<IndexedElement<T>>(l.NonZeroCount + r.NonZeroCount);
        data.AddRange(
            l
                .GetIndexedElements(false)
                .Union(r
                    .GetIndexedElements(false)
                )
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
        
        return new SparseIndexedVector<T>(l.Count, data);
    }
    
    public static IVector<T> Sum<T>(IVector<T> l, IVector<T> r)
        where T : struct, INumber<T> 
        => (l is Regular.Vector<T>, r is Regular.Vector<T>) switch
        {
            (true , true ) => (l as Regular.Vector<T>).Sum(r as Regular.Vector<T>),
            (true , false) => (l as Regular.Vector<T>).Sum(r as SparseIndexedVector<T>),
            (false, true ) => (l as SparseIndexedVector<T>).Sum(r as Regular.Vector<T>),
            (false, false) => (l as SparseIndexedVector<T>).Sum(r as SparseIndexedVector<T>)
        };
}