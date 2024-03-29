﻿using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Common.Collections.Chunked;
using Common.Collections.Element;
using Common.Collections.Vector.Operations;
using Common.Collections.Vector.Sparse.Indexed;
using Common.Serialization;
using Parquet.Data;
using SimdLinq;

IEnumerable<T> GenerateData<T>(int count, int sparsityFactor, Random rng)
    where T : INumber<T>
{
    return Enumerable
        .Repeat(false, count)
        .Select(_ => rng.Next(0, 2 + sparsityFactor) == 0 ? T.One : T.Zero);
}

IOrderedEnumerable<IndexedElement<T>> ToIndexedElements<T>(IList<T> elements)
    where T : INumber<T>
{
    return Enumerable
        .Range(0, elements.Count)
        .Select(i => new IndexedElement<T>(i, elements[i]))
        .Where(e => e.Value != T.Zero)
        .OrderBy(e => e.Index);
}

var count = 10000;
var sparsityFactor = -1;
var seed = unchecked((int) DateTime.Now.Ticks);
var rng = new Random(seed);
var serializerOptions = Common.Serialization.Parquet.DefaultSerializerOptions;

var filePath = @"C:\Users\1tira\Desktop\Data\DistributedMatrixComputations\data.parquet";
var inputData = GenerateData<int>(count, sparsityFactor, rng).ToList();
using (var inputFile = File.OpenWrite(filePath))
{ 
    Common.Serialization.Parquet.SerializeAsync(inputData, inputFile, serializerOptions).Wait();
}

var fileStreamOptions = new FileStreamOptions
{
    Access = FileAccess.Read,
    Mode = FileMode.Open,
    Options = FileOptions.SequentialScan,
    Share = FileShare.Read
};

Common.Collections.Sync.IVector<int> syncOutputData = null;
var outputFile = File.Open(filePath, fileStreamOptions);
syncOutputData = Common.Collections.Sync.Vector<int>.Create(outputFile, serializerOptions.ParquetOptions);
syncOutputData.SetDeserializer(columns => columns.First().Data.Cast<int>());

var outputData = Enumerable
    .Range(0, syncOutputData.RowGroupCount)
    .Select(rgIndex => syncOutputData.GetRowGroup(rgIndex))
    .ToList();

Console.WriteLine(nameof(inputData));
Console.WriteLine($"{nameof(inputData.Count)}={inputData.Count}");
Console.WriteLine($"{nameof(inputData)}.Sum()={inputData.Sum()}");
Console.WriteLine();

Console.WriteLine(nameof(outputData));
Console.WriteLine($"{nameof(outputData)}.Count()={outputData.Count}");
// Console.WriteLine($"{nameof(outputData)}.Sum()={outputData.Sum()}");
Console.WriteLine();

// foreach (var type in Common.Serialization.Parquet.Primitives)
// {
//     Console.WriteLine($"{nameof(type)}='{type.Name}'");
// }

/*
foreach (var (type, schema) in Common.Serialization.Parquet.PrimitiveTypeParquetSchemas)
{
    var cleanSchemaString = schema.ToString().Replace("\n", string.Empty);
    Console.WriteLine($"{nameof(type)}='{type}',{nameof(schema)}='{cleanSchemaString}'");
}
*/

/*
(int height, int width) shape = (1_000, 2_000_000);
var matrix = new int[shape.height,shape.width];
var sparsityFactor = 2; // 1+sparsityFactor zero elements to 1 non-zero element
var rng = new Random(DateTime.Now.Second * 1000 + DateTime.Now.Microsecond);

for (int i = 0; i < shape.height; i++)
{
    var data = GenerateData<int>(shape.width, sparsityFactor, rng);
    var j = 0;
    foreach (var e in data)
    {
        matrix[i, j++] = e * (rng.Next(0,1) == 0 ? -1 : 1);
    }
}

GC.Collect();

var startTs = TimeOnly.FromDateTime(DateTime.Now);
var sum = decimal.Zero;
for (int i = 0; i < shape.height; i++)
{
    for (int j = 0; j < shape.width; j++)
    {
        sum += matrix[i, j];
    }
}
var endTs = TimeOnly.FromDateTime(DateTime.Now);

GC.Collect();

var parStartTs = TimeOnly.FromDateTime(DateTime.Now);
var sumQueue = new ConcurrentQueue<decimal>();
var tasks = Enumerable
    .Range(0, shape.height)
    .Select(i => Task.Factory.StartNew(_ =>
    {
        var partialSum = decimal.Zero;
        for (int j = 0; j < shape.width; j++)
        {
            partialSum += matrix[i, j];
        }
        sumQueue.Enqueue(partialSum);
    }, null))
    .ToList();

await Task.WhenAll(tasks);

var parSum = sumQueue.Sum();
var parEndTs = TimeOnly.FromDateTime(DateTime.Now);

GC.Collect();

Console.WriteLine();
Console.WriteLine("Shape");
Console.WriteLine($"height={shape.height}");
Console.WriteLine($"width={shape.width}");

Console.WriteLine();
Console.WriteLine("Sequential");
Console.WriteLine($"sum={sum}");
Console.WriteLine($"startTs={startTs}");
Console.WriteLine($"endTs={endTs}");
Console.WriteLine($"diffTs={endTs - startTs}");

Console.WriteLine();
Console.WriteLine("Parallel");
Console.WriteLine($"sum={parSum}");
Console.WriteLine($"startTs={parStartTs}");
Console.WriteLine($"endTs={parEndTs}");
Console.WriteLine($"diffTs={parEndTs - parStartTs}");
*/

/*
var chunkedList = new ChunkedList<int>(-1, data);

for (int i = 0; i < count; i++)
{
    if (data[i] != chunkedList[i])
    {
        Console.WriteLine($"Index={i},RawDataValue={data[i]},ChunkedListValue={chunkedList[i]}");
    }
}
*/

/*
var lVector = new SparseIndexedVector<int>(count, ToIndexedElements(lData).AsEnumerable());
var rVector = new SparseIndexedVector<int>(count, ToIndexedElements(rData).AsEnumerable());

var sumData = lData.Zip(rData, (l, r) => l + r).ToList();

var sumVector = lVector.SumEnumSorted(rVector);

var lDataString = string.Join(",", lData);
Console.WriteLine(nameof(lData));
Console.WriteLine($"[{lDataString}]");
Console.WriteLine();

var rDataString = string.Join(",", rData);
Console.WriteLine(nameof(rData));
Console.WriteLine($"[{rDataString}]");
Console.WriteLine();

Console.WriteLine($"{nameof(sumData)}.{nameof(sumData.Count)}={sumData.Count}");
Console.WriteLine($"{nameof(sumVector)}.{nameof(sumVector.NonZeroCount)}={sumVector.NonZeroCount}");
Console.WriteLine();

Console.WriteLine("Diff:");
foreach (var e in sumVector)
{
    if (e.Value != sumData[e.Index])
    {
        Console.WriteLine(e);
    }
}
Console.WriteLine();

var sumDataString = string.Join(",", sumData);
Console.WriteLine(nameof(sumData));
Console.WriteLine($"[{sumDataString}]");
Console.WriteLine();

var sumVectorString = string.Join(",", sumVector);
Console.WriteLine(nameof(sumVector));
Console.WriteLine($"[{sumVectorString}]");
Console.WriteLine();
*/

/*
var rawData = Enumerable.Repeat(false, 100).ToList().AsReadOnly();
var records = rawData.Select(v => v.MakeRecord()).ToList().AsReadOnly();

using var memStream = new MemoryStream();

var singleField = new DataField<bool>("value", false);

var schema = new ParquetSchema(singleField);

using var writer = await ParquetWriter.CreateAsync(schema, memStream, new ParquetOptions()
{
    UseDateOnlyTypeForDates = true
});
writer.CompressionLevel = CompressionLevel.SmallestSize;
writer.CompressionMethod = CompressionMethod.Gzip;

using var rowGroupWriter = writer.CreateRowGroup();

await rowGroupWriter.WriteColumnAsync(new DataColumn(singleField, rawData.ToArray()));

rowGroupWriter.Dispose();
writer.Dispose();

var data = memStream.ToArray();

var outMemStream = new MemoryStream(data, false);

using var reader = await ParquetReader.CreateAsync(outMemStream, new ParquetOptions()
{
    UseDateOnlyTypeForDates = true
}, false);

using var rowGroupReader = reader.OpenRowGroupReader(0);

var outData = await rowGroupReader.ReadColumnAsync(singleField);

Console.WriteLine(nameof(rawData));
foreach (var g in rawData.GroupBy(v => v))
{
    Console.WriteLine($"Value={g.Key},Count={g.Count()}");
}
Console.WriteLine();

Console.WriteLine(nameof(outData));
foreach (var g in outData.Data.Cast<bool>().GroupBy(v => v))
{
    Console.WriteLine($"Value={g.Key},Count={g.Count()}");
}
Console.WriteLine();

static class Record
{
    public static Record<T> MakeRecord<T>(this T value) => new(value);
}

record struct Record<T>(T Value);
*/

/*
var jsonSerializerOptions = new JsonSerializerOptions()
{
    IgnoreReadOnlyProperties = false,
    IncludeFields = true,
    IgnoreReadOnlyFields = false,
    WriteIndented = true
};

var apiUri = new Uri("http://localhost:5263/");
var executorApi = new Uri(apiUri, "execute/");
var vectorApiUri = new Uri(executorApi, "vector/");
var vectorMultiplicationApiUri = new Uri(vectorApiUri, "multiply");
var vectorSumApiUri = new Uri(vectorApiUri, "sum");

var client = new HttpClient();

var rng = new Random(DateTime.Now.Second);

var count = 1000;
var dataType = DataType.Decimal;

var  leftVectorRaw = Enumerable.Repeat(false, count).Select(_ => (decimal) (rng.Next(0,2) == 0 ? 0.0 : 1.0)).ToList().AsReadOnly();
var rightVectorRaw = Enumerable.Repeat(false, count).Select(_ => (decimal) (rng.Next(0,2) == 0 ? 0.0 : 1.0)).ToList().AsReadOnly();

Console.WriteLine("Raw vector samples");
Console.WriteLine(string.Join(",",  leftVectorRaw.Take(15)));
Console.WriteLine(string.Join(",", rightVectorRaw.Take(15)));
Console.WriteLine();

var  leftVector = new SparseIndexedVector<decimal>( leftVectorRaw);
var rightVector = new SparseIndexedVector<decimal>(rightVectorRaw);

var  leftVectorSerialized = await  leftVector.SerializeAsync();
var rightVectorSerialized = await rightVector.SerializeAsync();

Console.WriteLine($"{nameof(count)}={count}");
Console.WriteLine($"{nameof( leftVector)} {nameof(SparseIndexedVector<decimal>.NonZeroCount)}={ leftVector.NonZeroCount}");
Console.WriteLine($"{nameof(rightVector)} {nameof(SparseIndexedVector<decimal>.NonZeroCount)}={rightVector.NonZeroCount}");
Console.WriteLine();

var sumTask = new SumTask(
     leftVectorSerialized
    ,rightVectorSerialized
    ,dataType
    ,dataType
);

var multiplicationTask = new MultiplicationTask(
     leftVectorSerialized
    ,rightVectorSerialized
    ,dataType
    ,dataType
);

// var response = await client.PostAsJsonAsync(vectorSumApiUri, sumTask, jsonSerializerOptions);
var response = await client.PostAsJsonAsync(vectorMultiplicationApiUri, multiplicationTask, jsonSerializerOptions);

// var sumResult = await response.Content.ReadFromJsonAsync<SumTaskResult>();
// if (sumResult is null)
// {
//     throw new ApplicationException();
// }
// 
// var resultVector = ((await sumResult.SerializedVector.DeserializeAsync<decimal>()).Vector as SparseIndexedVector<decimal>)!;

// var manualVectorSum = 
//     leftVectorRaw
//         .Zip(rightVectorRaw)
//         .Select(tuple => tuple.First + tuple.Second)
//         .ToList();
// 
// Console.WriteLine("Manual vector value counts");
// foreach (
//     var g 
//     in manualVectorSum
//         .GroupBy(e => e)
//         .OrderBy(e => e.Key)
//     )
// {
//     Console.WriteLine($"Value={g.Key},Count={g.Count()}");
// }
// Console.WriteLine();
// 
// Console.WriteLine("Execution result vector value counts");
// foreach (
//     var g 
//     in resultVector
//         .Select(e => e.Value)
//         .GroupBy(e => e)
//         .OrderBy(e => e.Key)
//     )
// {
//     Console.WriteLine($"Value={g.Key},Count={g.Count()}");
// }
// Console.WriteLine();
// 
// Console.WriteLine("Manual result vector non-zero element index bounds");
// Console.WriteLine(Math.Min(manualVectorSum.    IndexOf(1),manualVectorSum.    IndexOf(2)));
// Console.WriteLine(Math.Max(manualVectorSum.LastIndexOf(1),manualVectorSum.LastIndexOf(2)));
// Console.WriteLine();
// 
// Console.WriteLine("Execution result sparse vector element index bounds");
// Console.WriteLine(resultVector.Select(e => e.Index).Min());
// Console.WriteLine(resultVector.Select(e => e.Index).Max());
// Console.WriteLine();
// 
// Console.WriteLine("Differing element value indices");
// foreach (var e in resultVector)
// {
//     if (e.Value != manualVectorSum[e.Index])
//     {
//         Console.WriteLine($"Index={e.Index},ManualValue={manualVectorSum[e.Index]},ExecutionResultValue={e.Value}");
//     }
// }
// Console.WriteLine();

var manualMultiplicationResult =
    leftVectorRaw
        .Zip(rightVectorRaw)
        .Select(tuple => tuple.First * tuple.Second)
        .Sum();

var multiplicationTaskResult = await response.Content.ReadFromJsonAsync<MultiplicationTaskResult>();
if (multiplicationTaskResult is null)
{
    throw new ApplicationException();
}

var executionMultiplicationResult = multiplicationTaskResult.Result;

Console.WriteLine($"{nameof(manualMultiplicationResult)}={manualMultiplicationResult}");
Console.WriteLine($"{nameof(executionMultiplicationResult)}={executionMultiplicationResult}");
Console.WriteLine();
*/