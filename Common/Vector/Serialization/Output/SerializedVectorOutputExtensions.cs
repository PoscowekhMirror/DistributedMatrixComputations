using System.Text;

namespace Common.Vector.Serialization.Output;

public static class SerializedVectorOutputExtensions
{
    public static void OutputToConsole(this ISerializedVector v, string delimiter = "")
    {
        var numberOfBytesToOutput = Math.Min(10, v.SerializedElements.Data.Length);
        var output = new StringBuilder(100);

        // var parquetSchemaToOutput = 
        //     v
        //         .SerializedElements
        //         .DataSchema
        //         .ToString()
        //         .Replace("\r", string.Empty)
        //         .Replace("\n", string.Empty);

        var byteOutput = 
            Convert.ToHexString(v.SerializedElements.Data[..numberOfBytesToOutput])
            + 
            (numberOfBytesToOutput < v.SerializedElements.Data.Length ? "..." : string.Empty);
        
        output.AppendLine($"{delimiter}{nameof(ISerializedVector)}");
        output.AppendLine($"{delimiter}{{");
        output.AppendLine($"{delimiter}   {nameof(v.VectorTypeName)}='{v.VectorTypeName}'");
        output.AppendLine($"{delimiter}  ,{nameof(v.Count)}='{v.Count}'");
        output.AppendLine($"{delimiter}  ,{nameof(v.SerializedElements)}");
        output.AppendLine($"{delimiter}  {{");
        //output.AppendLine($"{delimiter}     {nameof(v.SerializedElements.DataSchema)}='{parquetSchemaToOutput}'");
        output.AppendLine($"{delimiter}    {nameof(v.SerializedElements.Data)}='{byteOutput}'");
        output.AppendLine($"{delimiter}  }}");
        output.AppendLine($"{delimiter}}}");
        
        Console.Write(output.ToString());
    }
}