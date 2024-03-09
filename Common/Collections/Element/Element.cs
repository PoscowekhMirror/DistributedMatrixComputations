using Parquet.Schema;
using Parquet.Serialization.Attributes;

namespace Common.Collections.Element;

[Obsolete("For serialization only")]
public record struct Element<T> 
{
    [ParquetRequired] 
    public T Value { internal set; get; }

    [Obsolete("For serialization only")]
    public Element() : this(default) { }
    public Element(T value) => Value = value;

    public override string ToString()
        // => $"{GetType().Name}<{typeof(T).Name}>{{{nameof(Value)}={Value}}}";
        => $"{{{nameof(Value)}={Value}}}";

    public static ParquetSchema ParquetSchema 
        => new(new DataField<T>("value", typeof(T).IsNullable()));
}