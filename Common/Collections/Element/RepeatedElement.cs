using Parquet.Schema;
using Parquet.Serialization.Attributes;

namespace Common.Collections.Element;

public record struct RepeatedElement<T>
{
    [ParquetRequired] public int Index { set; get; }
    [ParquetRequired] public int Count { set; get; }
    [ParquetRequired] public T   Value { set; get; }
    
    [Obsolete("For serialization only")]
    public RepeatedElement() : this(default, default, default) { }
    public RepeatedElement(int index, int count, T value)
    {
        Index = index;
        Count = count;
        Value = value;
    }
    
    public static RepeatedElement<T> Default
        => new(-1, 0, default);
    
    public override string ToString() 
        => $"{{{nameof(Index)}={Index},{{{nameof(Count)}={Count},{nameof(Value)}={Value}}}";

    public static ParquetSchema ParquetSchema
        => new(new Field[]
        {
             new DataField<int>(nameof(Index).ToLower(), false)
            ,new DataField<int>(nameof(Count).ToLower(), false)
            ,new DataField<T  >(nameof(Value).ToLower(), typeof(T).IsNullable())
        });
}