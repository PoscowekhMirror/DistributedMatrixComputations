using System.Numerics;

namespace Common.Vector.Serialization;

public readonly record struct VectorDeserializationResult<T>(
         string VectorTypeName
        ,IVector<T> Vector
    )
    // where T : struct, INumber<T>
{
    
}