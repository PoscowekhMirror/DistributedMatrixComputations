using Common.Collections.Vector.Serialization;

namespace Common.Tasks;

public sealed record class MultiplicationTask(
     ISerializedVector LeftVector
    ,ISerializedVector RightVector
    ,DataType LeftVectorDataType
    ,DataType RightVectorDataType
);