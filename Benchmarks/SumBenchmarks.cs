using BenchmarkDotNet.Attributes;
using Common.Vector;
using Common.Vector.Operations;

namespace Benchmarks;

[MemoryDiagnoser(true)]
public class SumBenchmarks
{
    [Params(/*500_000/*, 1_000_000, */5_000_000, 10_000_000, 15_000_000)] 
    public int count;

    [Params(2, 4)]
    public int sparsity;

    private         List<decimal> _rawVectorLeft     ;
    private         List<decimal> _rawVectorRight    ;
    private SparseVector<decimal> _sparseVectorLeft  ;
    private SparseVector<decimal> _sparseVectorRight ;
    private       Vector<decimal> _regularVectorLeft ;
    private       Vector<decimal> _regularVectorRight;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(DateTime.Now.Second * 1000 + DateTime.Now.Microsecond);
        _rawVectorLeft  = Enumerable.Repeat(false, count).Select(_ => rng.Next(0, 2 + sparsity) == 0 ? decimal.One : decimal.Zero).ToList();
        _rawVectorRight = Enumerable.Repeat(false, count).Select(_ => rng.Next(0, 2 + sparsity) == 0 ? decimal.One : decimal.Zero).ToList();
        
        _sparseVectorLeft  = new SparseVector<decimal>(_rawVectorLeft );
        _sparseVectorRight = new SparseVector<decimal>(_rawVectorRight);
        
        _regularVectorLeft  = new Vector<decimal>(_rawVectorLeft );
        _regularVectorRight = new Vector<decimal>(_rawVectorRight);
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        _rawVectorLeft     = _rawVectorRight     = new List<decimal>();
        _regularVectorLeft = _regularVectorRight = new Vector<decimal>(_rawVectorLeft);
        _sparseVectorLeft  = _sparseVectorRight  = new SparseVector<decimal>(_rawVectorLeft);
        GC.Collect();
    }
    
    /*
    [Benchmark(Baseline = true)]
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
        var sum = _sparseVectorLeft.SumLinq(_sparseVectorRight);
    }
    
    // [Benchmark]
    public void SparseVectorSumLinqParallel() 
    {
        var sum = _sparseVectorLeft.SumLinqParallel(_sparseVectorRight);
    }
    
    [Benchmark]
    public void SparseVectorSumEnumSorted() 
    {
        var sum = _sparseVectorLeft.SumEnumSorted(_sparseVectorRight);
    }
    
    // [Benchmark]
    public void SparseVectorSumForLoopSorted() 
    {
        var sum = _sparseVectorLeft.SumForLoopSorted(_sparseVectorRight);
    }
}