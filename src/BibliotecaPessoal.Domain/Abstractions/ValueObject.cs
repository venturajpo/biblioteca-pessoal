namespace BibliotecaPessoal.Domain.Abstractions;

/// <summary>
/// Base de objetos de valor: não possuem identidade e são comparados pelo
/// conjunto dos seus componentes (ex.: Isbn, Avaliacao, Progresso).
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Componentes que definem a igualdade do objeto de valor.</summary>
    protected abstract IEnumerable<object?> ObterComponentesDeIgualdade();

    public bool Equals(ValueObject? other)
        => other is not null
           && other.GetType() == GetType()
           && other.ObterComponentesDeIgualdade().SequenceEqual(ObterComponentesDeIgualdade());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());

        foreach (var componente in ObterComponentesDeIgualdade())
        {
            hash.Add(componente);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
