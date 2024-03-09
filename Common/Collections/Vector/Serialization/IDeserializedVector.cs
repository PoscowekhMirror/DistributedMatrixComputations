namespace Common.Collections.Vector.Serialization;

public interface IDeserializedVector<T>
{
    string VectorTypeName { get; }
    IVector<T> Vector { get; }
}