using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Common.Collections.Vector.Operations;
using Common.Collections.Vector.Regular;
using Common.Collections.Vector.Sparse.Indexed;

namespace Benchmarks;

[MemoryDiagnoser(true)]
[ThreadingDiagnoser()]
public class SumBenchmarks
{
    public static IList<int> Counts { set; get; } = new List<int>(){5_000_000, 10_000_000, 15_000_000};

    [ParamsSource(nameof(Counts))]
    public int count;

    public static IList<int> SparsityFactors { set; get; } = new List<int>() {2, 4};

    [ParamsSource(nameof(SparsityFactors))]
    public int sparsity;

    private                List<decimal> _rawVectorLeft           ;
    private                List<decimal> _rawVectorRight          ;
    private SparseIndexedVector<decimal> _sparseIndexedVectorLeft ;
    private SparseIndexedVector<decimal> _sparseIndexedVectorRight;
    private              Vector<decimal> _regularVectorLeft       ;
    private              Vector<decimal> _regularVectorRight      ;

    private Random _rng;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _rng = new Random(DateTime.Now.Second * 1000 + DateTime.Now.Microsecond);
        
        _rawVectorLeft  = Enumerable.Repeat(false, count).Select(_ => _rng.Next(0, 2 + sparsity) == 0 ? decimal.One : decimal.Zero).ToList();
        _rawVectorRight = Enumerable.Repeat(false, count).Select(_ => _rng.Next(0, 2 + sparsity) == 0 ? decimal.One : decimal.Zero).ToList();
        
        _sparseIndexedVectorLeft   = new SparseIndexedVector<decimal>(_rawVectorLeft );
        _sparseIndexedVectorRight  = new SparseIndexedVector<decimal>(_rawVectorRight);
        _regularVectorLeft         = new              Vector<decimal>(_rawVectorLeft );
        _regularVectorRight        = new              Vector<decimal>(_rawVectorRight);
        GC.Collect();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        GC.Collect();
    }
    
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _rawVectorLeft            = null; _rawVectorRight           = null;
        _regularVectorLeft        = null; _regularVectorRight       = null;
        _sparseIndexedVectorLeft  = null; _sparseIndexedVectorRight = null;
        GC.Collect();
    }

    /*
    [Benchmark(Baseline)]
    public void ListSumLinq()
    {
        var sum =
            _rawVectorLeft
                .Zip(_rawVectorRight)
                .Select(t => t.First + t.Second)
                .ToList();
    }
    */
    
    [Benchmark(Baseline = true)]
    public void ListSumForLoop() 
    {
        var index = 0;
        var countBoth = _rawVectorLeft.Count;
        var result = new List<decimal>(countBoth);

        while (index < countBoth)
        {
            result.Add(_rawVectorLeft[index] + _rawVectorRight[index]);
            index += 1;
        }

        var sum = result;
    }
    
    [Benchmark]
    public void ListSumLinqParallel()
    {
        var sum =
            _rawVectorLeft
                .Zip(_rawVectorRight)
                .AsParallel()
                .Select(t => t.First + t.Second)
                .ToList();
    }
    
    [Benchmark]
    public void ListSumSpanForLoop()
    {
        var spanLeft  = CollectionsMarshal.AsSpan(_rawVectorLeft );
        var spanRight = CollectionsMarshal.AsSpan(_rawVectorRight);

        var length = spanLeft.Length;
        var sum = decimal.Zero;

        for (int i = 0; i < length; i++)
        {
            sum += spanLeft[i] + spanRight[i];
        }
    }
    
    [Benchmark]
    public void ListSumSpanRefForLoop()
    {
        var spanLeft  = CollectionsMarshal.AsSpan(_rawVectorLeft );
        var spanRight = CollectionsMarshal.AsSpan(_rawVectorRight);

        ref var spanRefLeft  = ref MemoryMarshal.GetReference(spanLeft );
        ref var spanRefRight = ref MemoryMarshal.GetReference(spanRight);
        
        var length = spanLeft.Length;
        var sum = decimal.Zero;

        for (int i = 0; i < length; i++)
        {
            var itemLeft  = Unsafe.Add(ref spanRefLeft , i);
            var itemRight = Unsafe.Add(ref spanRefRight, i);
            sum += itemLeft + itemRight;
        }
    }

    // [Benchmark]
    public void RegularVectorSumForLoop()
    {
        var sum = _regularVectorLeft.SumForLoop(_regularVectorRight);
    }

    // [Benchmark]
    public void RegularVectorSumLinq() 
    {
        var sum = _regularVectorLeft.SumLinq(_regularVectorRight);
    }

    // [Benchmark]
    public void RegularVectorSumLinqParallel()
    {
        var sum = _regularVectorLeft.SumLinqParallel(_regularVectorRight);
    }

    
    // [Benchmark]
    public void SparseVectorSumLinq()
    {
        var sum = _sparseIndexedVectorLeft.SumLinq(_sparseIndexedVectorRight);
    }
    
    // [Benchmark]
    public void SparseVectorSumLinqParallel() 
    {
        var sum = _sparseIndexedVectorLeft.SumLinqParallel(_sparseIndexedVectorRight);
    }
    
    [Benchmark]
    public void SparseVectorSumEnumSorted() 
    {
        var sum = _sparseIndexedVectorLeft.SumEnumSorted(_sparseIndexedVectorRight);
    }
    
    // [Benchmark]
    public void SparseVectorSumForLoopSorted() 
    {
        var sum = _sparseIndexedVectorLeft.SumForLoopSorted(_sparseIndexedVectorRight);
    }
}