using Parquet.Schema;

namespace Common.Serialization;

public interface ISerializable<T> // : System.Runtime.Serialization.ISerializable
    where T : IDeserializable<T, ISerializable<T>>
{ 
    ParquetSchema GetParquetSchema();
    
              T  Serialize     ();
    ValueTask<T> SerializeAsync();
}