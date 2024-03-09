using System.Numerics;
using System.Text;

namespace Common.Vector.Output;

public static class VectorConsoleOutputExtensions
{
    public static int DefaultElementCountToOutput { set; get; } = 10;
    
    public static void OutputInConsole<T>(this IVector<T> v, int outputElementCount = -1, string delimiter = "")
        where T : struct, INumber<T>
    {
        var output = new StringBuilder(100);
        var actualOutputElementCount = Math.Min(
             outputElementCount == -1 ? DefaultElementCountToOutput : outputElementCount
            ,v.Count
        );

        var elementsToOutput = v.Take(actualOutputElementCount);
        
        output.AppendLine($"{delimiter}{v.GetType().Name}<{typeof(T).Name}>");
        output.AppendLine($"{delimiter}{{");
        output.AppendLine($"{delimiter}   {nameof(v.Count)}='{v.Count}'");
        output.AppendLine($"{delimiter}  ,{nameof(v.Sparsity)}='{v.Sparsity}'");
        output.AppendLine($"{delimiter}  ,Elements=[");
        output.Append($"{delimiter}     ");
        output.AppendJoin($"\r\n{delimiter}    ,", elementsToOutput);
        output.AppendLine();
        if (actualOutputElementCount < v.Count)
        {
            output.AppendLine($"{delimiter}    ,...");
        }
        output.AppendLine($"{delimiter}}}");
        
        Console.WriteLine(output.ToString());
    }
}