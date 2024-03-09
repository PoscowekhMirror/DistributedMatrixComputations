using System.Collections;

namespace Common.Collections.Chunked;

public sealed class Chunk<T> : IChunk<T>
{
    // private readonly IList<T> _list;
    // internal IList<T> InternalChunkedList => _list;

    private readonly T[] _values;
    internal T[] InternalValues => _values;
    public ReadOnlySpan<T> Values => _values.AsSpan();

    public int Capacity => _values.Length;
    public int Count { private set; get; }

    public bool IsReadOnly => false;
    public bool IsFilled => Count == Capacity;

    public T this[int index]
    {
        get 
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }
            return _values[index];
        }
        set 
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }
            _values[index] = value;
        }
    }

    public Chunk(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        _values = new T[capacity];
        Count = 0;
    }
    public Chunk(int capacity, IEnumerable<T> elements)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        
        _values = new T[capacity];
        
        using var enumerator = elements.GetEnumerator();
        var index = 0;
        
        while (enumerator.MoveNext())
        {
            if (index >= Capacity)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }
            
            _values[index] = enumerator.Current;
            index += 1;
        }
        
        Count = index + 1;
    }
    internal Chunk(in T[] elements)
    {
        _values = elements;
        Count = elements.Length;
    }

    public void Add(T item)
    {
        if (Count == Capacity)
        {
            throw new OverflowException();
        }
        _values[Count] = item;
        Count += 1;
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index >= Count + 1)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }
        if (index >= Capacity)
        {
            throw new OverflowException();
        }

        if (index == Count)
        {
            _values[Count] = item;
            Count += 1;
            return;
        }
        
        var segment = new ArraySegment<T>(_values, index, Count - index + 1).ToArray();
        segment.CopyTo(_values, index + 1);

        _values[index] = item;
        Count += 1;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        if (index == Count - 1)
        {
            Count -= 1;
            _values[Count] = default;
            return;
        }

        var segment = new ArraySegment<T>(_values, index + 1, Count - index);
        segment.CopyTo(_values, index);
        Count -= 1;
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index == -1)
        {
            return false;
        }
        RemoveAt(index);
        return true;
    }

    public int IndexOf(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            var value = _values[i];
            if ((value is     null && item is     null                      ) || 
                (value is not null && item is not null && value.Equals(item)))
            {
                return i;
            }
        }
        return -1;
    }

    public bool Contains(T item) => IndexOf(item) != -1;
    
    public void Clear()
    {
        for (int i = 0; i < Count; i++)
        {
            _values[i] = default;
        }
        Count = 0;
    }

    public void CopyTo(T[] array, int arrayIndex) =>
        _values.CopyTo(ReferenceEquals(array, _values) ? array.ToArray() : array, arrayIndex);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<T> GetEnumerator() => GetEnumerator(-1);
    public IEnumerator<T> GetEnumerator(int startingIndex) 
        => new Enumerator(_values, Count, startingIndex);
    
    internal struct Enumerator : IEnumerator<T>
    {
        internal T[] ValueArray { private init; get; }
        internal int Count { private set; get; }
        internal int Index { private set; get; }

        public T Current => ValueArray[Index];
        object IEnumerator.Current => Current;
        
        public Enumerator(T[] valueArray, int count, int startingIndex)
        {
            if (count < 0)
            {
                throw new IndexOutOfRangeException(nameof(count));
            }
            if (startingIndex < -1 || startingIndex >= count)
            {
                throw new IndexOutOfRangeException(nameof(startingIndex));
            }
            ValueArray = valueArray;
            Count = count;
            Index = startingIndex;
        }

        public bool MoveNext() => ++Index < Count;
        public void Reset() => Index = -1;

        public void Dispose()
        {
            
        }
    }
}