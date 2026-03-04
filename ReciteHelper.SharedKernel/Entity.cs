using System.Text.Json.Serialization;

namespace ReciteHelper.SharedKernel;

public class Entity : IEquatable<Entity>
{
    [JsonPropertyName("id")]
    public int Id { get; protected set; }

    protected Entity() { Id = 0; }

    protected Entity(int id)
    {
        if (id < 0)
            throw new ArgumentException("Property id must greater than 0.", nameof(id));
        Id = id;
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;

        if (Id == 0 || other.Id == 0)
            return ReferenceEquals(this, other);

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity left, Entity? right) => !Equals(left, right);

    public bool IsTransient => Id == 0;

    public void SetId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Property id must greater than 0.", nameof(id));
        if (!IsTransient)
            throw new InvalidOperationException("The ID of a persistent entity cannot be modified.");

        Id = id;
    }

    //protected void AddDomainEvent(IDomainEvent domainEvent)
    //{
    //    _domainEvents.Add(domainEvent);
    //}

    //public void ClearDomainEvents() => _domainEvents.Clear();

    public override string ToString()
    {
        return $"{GetType().Name} [Id={Id}]";
    }

}
