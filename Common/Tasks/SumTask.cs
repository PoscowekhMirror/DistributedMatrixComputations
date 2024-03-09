using Common.Collections.Vector.Serialization;

namespace Common.Tasks;

public sealed record class SumTask(
     ISerializedVector LeftVector
    ,ISerializedVector RightVector
    ,DataType LeftVectorDataType
    ,DataType RightVectorDataType
);