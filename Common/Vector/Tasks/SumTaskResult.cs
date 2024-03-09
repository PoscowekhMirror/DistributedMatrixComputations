using Common.Vector.Serialization;

namespace Common.Vector.Tasks;

public sealed record class SumTaskResult(
      ISerializedVector SerializedVector
     ,DataType DataType
);