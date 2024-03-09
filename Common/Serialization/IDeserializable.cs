namespace Common.Serialization;

public interface IDeserializable<in TSelf, TResult> 
    where TResult : ISerializable<TSelf>
    where TSelf : 
         IDeserializable<TSelf, TResult>
        ,IDeserializable<TSelf, ISerializable<TSelf>>
{
              TResult  Deserialize     ();
    ValueTask<TResult> DeserializeAsync();
}