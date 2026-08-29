namespace BibliotecaPessoal.Domain.Abstractions;

/// <summary>
/// Base de toda entidade do domínio: possui identidade própria e é comparada
/// pelo <see cref="Id"/>, nunca pelos seus atributos.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A identidade de uma entidade não pode ser vazia.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Construtor exigido por mapeadores objeto-relacional.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private init; }

    public bool Equals(Entity? other)
        => other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
