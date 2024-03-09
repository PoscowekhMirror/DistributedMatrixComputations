using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Common.Vector;
using Common.Vector.Operations;
using Common.Vector.Serialization;
using Common.Vector.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    options.JsonSerializerOptions.AllowTrailingCommas = false;
    options.JsonSerializerOptions.IncludeFields = true;
    options.JsonSerializerOptions.IgnoreReadOnlyFields = false;
    options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.DefaultBufferSize = 16 * 1024 * 1024;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI();
app.MapSwagger().AllowAnonymous().CacheOutput();

// var jsonSerializerSettings = new JsonSerializerSettings()
// {
//     Formatting = Formatting.None
// };
// 
// var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);

var jsonSerializerOptions = new JsonSerializerOptions()
{
    IgnoreReadOnlyProperties = false,
    IncludeFields = true,
    IgnoreReadOnlyFields = false,
    WriteIndented = true
};

var executorApi = app.MapGroup("/execute");

var vectorApi = executorApi.MapGroup("/vector");

vectorApi.MapPost("/multiply", async context =>
{
    var vectorMultiplicationTask = await context.Request.ReadFromJsonAsync<MultiplicationTask>(jsonSerializerOptions);
    var finalDataType = (DataType) Math.Max(
         (byte) vectorMultiplicationTask.LeftVectorDataType
        ,(byte) vectorMultiplicationTask.RightVectorDataType
    );

    object result;
    
    switch (finalDataType)
    {
        case DataType.Float:
            result = VectorMultiplicationExtensions.Multiply(
                 (await vectorMultiplicationTask.LeftVector .DeserializeAsync<float>()).Vector
                ,(await vectorMultiplicationTask.RightVector.DeserializeAsync<float>()).Vector
            );
            break;
        
        case DataType.Double:
            result = VectorMultiplicationExtensions.Multiply(
                 (await vectorMultiplicationTask.LeftVector .DeserializeAsync<double>()).Vector
                ,(await vectorMultiplicationTask.RightVector.DeserializeAsync<double>()).Vector
            );
            break;
        
        case DataType.Decimal:
            result = VectorMultiplicationExtensions.Multiply(
                 (await vectorMultiplicationTask.LeftVector .DeserializeAsync<decimal>()).Vector
                ,(await vectorMultiplicationTask.RightVector.DeserializeAsync<decimal>()).Vector
            );
            break;
        
        default:
            throw new ArgumentException();
    }

    var httpResult = Results.Ok(new MultiplicationTaskResult(result, finalDataType));
    await httpResult.ExecuteAsync(context);
});

vectorApi.MapPost("/sum", async context =>
{
    var vectorSumTask = await context.Request.ReadFromJsonAsync<SumTask>(jsonSerializerOptions);
    if (vectorSumTask is null)
    {
        await Results.BadRequest(vectorSumTask).ExecuteAsync(context);
    }
    
    var finalDataType = (DataType) Math.Max(
         (byte) vectorSumTask.LeftVectorDataType
        ,(byte) vectorSumTask.RightVectorDataType
    );

    if (finalDataType == DataType.Unknown)
    {
        await Results.BadRequest(finalDataType).ExecuteAsync(context);
    }
    
    ISerializedVector result;
    string vectorTypeName;
    
    switch (finalDataType)
    {
        case DataType.Float:
            var floatVector = VectorSumExtensions.Sum(
                 (await vectorSumTask. LeftVector.DeserializeAsync<float>()).Vector
                ,(await vectorSumTask.RightVector.DeserializeAsync<float>()).Vector
            );
            vectorTypeName = (floatVector is Vector<float>, floatVector is SparseVector<float>) switch
            {
                (true , false) => nameof(      Vector<float>),
                (false,  true) => nameof(SparseVector<float>),
                (_    , _    ) => throw new ArgumentException()
            };
            result = vectorTypeName switch 
            {
                nameof(      Vector<float>) => await (floatVector as       Vector<float>).SerializeAsync(),
                nameof(SparseVector<float>) => await (floatVector as SparseVector<float>).SerializeAsync(),
                _                           => throw new ArgumentException()
            };
            break;
        
        case DataType.Double:
            var doubleVector = VectorSumExtensions.Sum(
                 (await vectorSumTask.LeftVector .DeserializeAsync<double>()).Vector
                ,(await vectorSumTask.RightVector.DeserializeAsync<double>()).Vector
            );
            vectorTypeName = (doubleVector is Vector<double>, doubleVector is SparseVector<double>) switch
            {
                (true , false) => nameof(      Vector<double>),
                (false, true ) => nameof(SparseVector<double>),
                (_    , _    ) => throw new ArgumentException()
            };
            result = vectorTypeName switch 
            {
                nameof(      Vector<double>) => await (doubleVector as       Vector<double>).SerializeAsync(),
                nameof(SparseVector<double>) => await (doubleVector as SparseVector<double>).SerializeAsync(),
                _                            => throw new ArgumentException()
            };
            break;
        
        case DataType.Decimal:
            var decimalVector = VectorSumExtensions.Sum(
                 (await vectorSumTask.LeftVector .DeserializeAsync<decimal>()).Vector
                ,(await vectorSumTask.RightVector.DeserializeAsync<decimal>()).Vector
            );
            vectorTypeName = (decimalVector is Vector<decimal>, decimalVector is SparseVector<decimal>) switch
            {
                (true , false) => nameof(      Vector<decimal>),
                (false, true ) => nameof(SparseVector<decimal>),
                (_    , _    ) => throw new ArgumentException()
            };
            result = vectorTypeName switch 
            {
                nameof(      Vector<decimal>) => await (decimalVector as       Vector<decimal>).SerializeAsync(),
                nameof(SparseVector<decimal>) => await (decimalVector as SparseVector<decimal>).SerializeAsync(),
                _                             => throw new ArgumentException()
            };
            break;
        
        default:
            throw new ArgumentException();
    }

    var taskResult = new SumTaskResult(result, finalDataType);
    
    var httpResult = Results.Ok(taskResult);
    await httpResult.ExecuteAsync(context);
});

app.Run();