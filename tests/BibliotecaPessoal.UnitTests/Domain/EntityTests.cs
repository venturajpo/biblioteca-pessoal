using BibliotecaPessoal.Domain.Abstractions;

namespace BibliotecaPessoal.UnitTests.Domain;

public sealed class EntityTests
{
    private sealed class EntidadeFalsa(Guid id) : Entity(id);

    private sealed class OutraEntidadeFalsa(Guid id) : Entity(id);

    [Fact]
    public void Entidades_com_o_mesmo_id_sao_iguais()
    {
        var id = Guid.NewGuid();

        var primeira = new EntidadeFalsa(id);
        var segunda = new EntidadeFalsa(id);

        Assert.Equal(primeira, segunda);
        Assert.True(primeira == segunda);
        Assert.Equal(primeira.GetHashCode(), segunda.GetHashCode());
    }

    [Fact]
    public void Entidades_com_ids_diferentes_nao_sao_iguais()
    {
        var primeira = new EntidadeFalsa(Guid.NewGuid());
        var segunda = new EntidadeFalsa(Guid.NewGuid());

        Assert.NotEqual(primeira, segunda);
        Assert.True(primeira != segunda);
    }

    [Fact]
    public void Entidades_de_tipos_diferentes_nao_sao_iguais_mesmo_com_o_mesmo_id()
    {
        var id = Guid.NewGuid();

        var livro = new EntidadeFalsa(id);
        var leitura = new OutraEntidadeFalsa(id);

        Assert.False(livro.Equals(leitura));
    }

    [Fact]
    public void Nao_e_possivel_criar_uma_entidade_com_id_vazio()
        => Assert.Throws<ArgumentException>(() => new EntidadeFalsa(Guid.Empty));
}
