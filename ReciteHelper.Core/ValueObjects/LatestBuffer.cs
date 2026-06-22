using ReciteHelper.SharedKernel;

namespace ReciteHelper.Core.ValueObjects;

public class LatestBuffer<TValue> : ValueObject where TValue : struct
{
    private TValue?[] _internalArray;
    private int _pivot = 0;

    private LatestBuffer(int size)
    {
        _internalArray = new TValue?[size + 1];

        for (int i = 0; i < size; i++) _internalArray[i] = null;
    }

    public static LatestBuffer<T> Create<T>(int size) where T : struct
    {
        return new LatestBuffer<T>(size);
    }

    public void Add(TValue value)
    {
        _internalArray[_pivot] = value;
        _pivot++;

        if (_pivot > _internalArray.Length - 1)
        {
            for (int i = 0; i < _internalArray.Length - 1; i++)
                _internalArray[i] = _internalArray[i + 1];
            _pivot--;
        }
    }

    public void Clear()
    {
        _pivot = 0;
    }

    public override R Clone<R>()
    {
        throw new NotImplementedException();
    }

    public bool EqualsTo(TValue value)
    {
        for (int i = 0; i < _internalArray.Length - 1; i++)
        {
            if (!_internalArray[i].Equals(value))
                return false;
        }

        return true;
    }

    public void Println()
    {
        for (int i = 0; i < _internalArray.Length - 1; i++)
            Console.WriteLine(_internalArray[i]);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _internalArray;
    }

    protected override void Validate()
    {
        return;
    }
}