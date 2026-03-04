namespace ReciteHelper.SharedKernel;

public abstract class ValueObject
{
    protected ValueObject() { }

    protected abstract void Validate();

    protected abstract IEnumerable<object> GetEqualityComponents();

    public abstract T Clone<T>() where T : ValueObject;

    public static T Create<T>(Func<T> factory) where T : ValueObject
    {
        var instance = factory();
        instance.Validate();
        return instance;
    }

    public bool Equals(ValueObject? other) => Equals(other as object);

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        using var thisValues = this.GetEqualityComponents().GetEnumerator();
        using var thatValues = other.GetEqualityComponents().GetEnumerator();

        while (thisValues.MoveNext() && thatValues.MoveNext())
        {
            if (thisValues.Current is null && thatValues.Current is null)
                continue;

            if (thisValues.Current is null || thisValues.Current.Equals(thatValues.Current))
                return false;
        }

        return !thisValues.MoveNext() && !thatValues.MoveNext();
    }

    public override int GetHashCode()
    {
        // Let's use curly braces; writing it as a lambda expression looks a bit ugly
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? that) => Equals(left, that);
    public static bool operator !=(ValueObject? left, ValueObject? that) => !Equals(left, that);
}
