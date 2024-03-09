using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Common.Collections.Chunked;
using Common.Collections.Vector.Operations;
using Common.Collections.Vector.Regular;
using Common.Collections.Vector.Sparse.Indexed;

namespace Benchmarks;

[MemoryDiagnoser(true)]
[ThreadingDiagnoser()]
public class SumBenchmarks
{
    public static IList<int> Counts { set; get; } = new List<int>()
    {
             1_000_000
        ,   10_000_000
        ,  100_000_000
        ,  500_000_000
        ,1_000_000_000
    };

    [ParamsSource(nameof(Counts))]
    public int count;

    public static IList<int> SparsityFactors { set; get; } = new List<int>()
    {
         2
        ,5
        ,8
    };

    [ParamsSource(nameof(SparsityFactors))]
    public int sparsity;

    public static IList<int> ChunkSizes { get; set; } = new List<int>()
    {
               1024 * 1024
        , 32 * 1024 * 1024
        ,128 * 1024 * 1024
    };

    private                List<int> _rawVectorLeft           ;
    private                List<int> _rawVectorRight          ;
    private SparseIndexedVector<int> _sparseIndexedVectorLeft ;
    private SparseIndexedVector<int> _sparseIndexedVectorRight;
    private              Vector<int> _regularVectorLeft       ;
    private              Vector<int> _regularVectorRight      ;
    private         ChunkedList<int> _chunkedListLeft         ;
    private         ChunkedList<int> _chunkedListRight        ;

    private Random _rng;
    
    [GlobalSetup]
    [ArgumentsSource(nameof(ChunkSizes))]
    public void GlobalSetup(int chunkSize)
    {
        _rng = new Random(DateTime.Now.Second * 1000 + DateTime.Now.Microsecond);
        
        _rawVectorLeft  = Enumerable.Repeat(false, count).Select(_ => _rng.Next(0, 2 + sparsity) == 0 ? 1 : 0).ToList();
        _rawVectorRight = Enumerable.Repeat(false, count).Select(_ => _rng.Next(0, 2 + sparsity) == 0 ? 1 : 0).ToList();
        
        _sparseIndexedVectorLeft   = new SparseIndexedVector<int>(           _rawVectorLeft );
        _sparseIndexedVectorRight  = new SparseIndexedVector<int>(           _rawVectorRight);
        _regularVectorLeft         = new              Vector<int>(           _rawVectorLeft );
        _regularVectorRight        = new              Vector<int>(           _rawVectorRight);
        _chunkedListLeft           = new         ChunkedList<int>(chunkSize, _rawVectorLeft );
        _chunkedListRight          = new         ChunkedList<int>(chunkSize, _rawVectorRight);
        
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
        _chunkedListLeft          = null; _chunkedListRight         = null;
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
        var result = new List<int>(countBoth);

        while (index < countBoth)
        {
            result.Add(_rawVectorLeft[index] + _rawVectorRight[index]);
            index += 1;
        }

        var sum = result;
    }
    
    // [Benchmark]
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
        var sum = new List<int>(length);

        for (int i = 0; i < length; i++)
        {
            sum.Add(spanLeft[i] + spanRight[i]);
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
        var sum = new List<int>(length);

        for (int i = 0; i < length; i++)
        {
            var itemLeft  = Unsafe.Add(ref spanRefLeft , i);
            var itemRight = Unsafe.Add(ref spanRefRight, i);
            sum.Add(itemLeft + itemRight);
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


    /*
    [Benchmark]
    [ArgumentsSource(nameof(ChunkSizes))]
    public void ChunkedListSumDirectForLoop(int chunkSize)
    {
        var sum = new ChunkedList<int>(chunkSize);

        for (int i = 0; i < _chunkedListLeft.Count; ++i)
        {
            sum.Add(_chunkedListLeft[i] + _chunkedListRight[i]);
        }
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(ChunkSizes))]
    public void ChunkedListSumChunkedForLoop(int chunkSize)
    {
        var sum = new ChunkedList<int>(chunkSize);

        for (int i = 0; i < _chunkedListLeft.ChunkCount; ++i)
        {
            var chunkLeft  = _chunkedListLeft .Chunks[i];
            var chunkRight = _chunkedListRight.Chunks[i];
            var chunkSum   = new Chunk<int>(_chunkedListLeft.ChunkSize);
            
            for (int j = 0; j < _chunkedListLeft.ChunkSize; j++)
            {
                chunkSum.Add(chunkLeft[i] + chunkRight[i]);
            }
            
            sum.Add(chunkSum);
        }
    }
    */
}