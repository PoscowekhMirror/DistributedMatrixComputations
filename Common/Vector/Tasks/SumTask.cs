using Common.Vector.Serialization;

namespace Common.Vector.Tasks;

public sealed record class SumTask(
     SerializedVector LeftVector
    ,SerializedVector RightVector
    ,DataType LeftVectorDataType
    ,DataType RightVectorDataType
);