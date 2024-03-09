using System.Globalization;
using BenchmarkDotNet.Running;
using Benchmarks;

IList<T> ParseList<T>(string argString)
    where T : IParsable<T>
    => argString
        .Split(",")
        .Select(s => T.Parse(s, CultureInfo.CurrentCulture))
        .ToList();

var counts          = ParseList<int>(args[0]);
var sparsityFactors = ParseList<int>(args[1]);

SumBenchmarks.Counts          = counts         ;
SumBenchmarks.SparsityFactors = sparsityFactors;

BenchmarkRunner.Run<SumBenchmarks>();