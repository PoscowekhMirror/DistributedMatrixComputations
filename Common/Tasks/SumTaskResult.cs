using Common.Collections.Vector.Serialization;

namespace Common.Tasks;

public sealed record class SumTaskResult(
      ISerializedVector SerializedVector
     ,DataType DataType
);