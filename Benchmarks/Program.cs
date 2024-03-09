using System.Globalization;
using BenchmarkDotNet.Running;
using Benchmarks;

IList<T> ParseList<T>(string argString)
    where T : IParsable<T>
    => argString
        .Split(",")
        .Select(s => T.Parse(s, CultureInfo.CurrentCulture))
        .ToList();

var counts          = args.Length >= 1 ? ParseList<int>(args[0]) : SumBenchmarks.Counts         ;
var sparsityFactors = args.Length >= 2 ? ParseList<int>(args[1]) : SumBenchmarks.SparsityFactors;
// var chunkSizes      = args.Length >= 3 ? ParseList<int>(args[2]) : SumBenchmarks.ChunkSizes     ;

SumBenchmarks.Counts          = counts         ;
SumBenchmarks.SparsityFactors = sparsityFactors;
// SumBenchmarks.ChunkSizes      = chunkSizes     ;

BenchmarkRunner.Run<SumBenchmarks>();