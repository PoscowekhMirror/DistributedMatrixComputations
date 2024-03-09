namespace Common.Collections.Vector.Serialization;

public readonly record struct DeserializedVector<T>(
         string VectorTypeName
        ,IVector<T> Vector
    ) : IDeserializedVector<T>
{
    
}