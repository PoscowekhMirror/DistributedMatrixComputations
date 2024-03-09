using Parquet.Schema;
using Parquet.Serialization.Attributes;

namespace Common.Collections.Element;

public record struct IndexedElement<T>
    // where T : struct
{
    [ParquetRequired] 
    public int Index { internal set; get; }
    [ParquetRequired] 
    public T Value { internal set; get; }

    [Obsolete("For serialization only")]
    public IndexedElement() : this(default, default) { }
    public IndexedElement(int index, T value)
    {
        Index = index;
        Value = value;
    }

    public static IndexedElement<T> Default 
        => new(-1, default);

    public override string ToString() 
        // => $"{GetType().Name}<{typeof(T).Name}>{{{nameof(Index)}={Index},{nameof(Value)}={Value}}}";
        => $"{{{nameof(Index)}={Index},{nameof(Value)}={Value}}}";

    public static ParquetSchema ParquetSchema
        => new(new Field[]
        {
             new DataField<int>(nameof(Index).ToLower(), false)
            ,new DataField<T  >(nameof(Value).ToLower(), typeof(T).IsNullable())
        });
}