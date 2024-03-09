using Common.Vector.Serialization.Output;

namespace Common.Vector.Tasks;

public static class TaskConsoleOutputExtensions
{
    public static void OutputToConsole(this SumTask sumTask)
    {
        Console.WriteLine($"{nameof(SumTask)}");
        Console.WriteLine($"{{");
        Console.WriteLine($"   {nameof(sumTask.LeftVector)}");
        Console.WriteLine($"  {{");
        sumTask.LeftVector.OutputToConsole("    ");
        Console.WriteLine($"  }}");
        Console.WriteLine($"  ,{nameof(sumTask.RightVector)}");
        Console.WriteLine($"  {{");
        sumTask.RightVector.OutputToConsole("    ");
        Console.WriteLine($"  }}");
        Console.WriteLine($"  ,{nameof(sumTask.LeftVectorDataType)}='{sumTask.LeftVectorDataType}'");
        Console.WriteLine($"  ,{nameof(sumTask.RightVectorDataType)}='{sumTask.RightVectorDataType}'");
        Console.WriteLine($"}}");
    }
}