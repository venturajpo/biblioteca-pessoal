using BibliotecaPessoal.Domain.Abstractions;

namespace BibliotecaPessoal.UnitTests.Domain;

public sealed class ValueObjectTests
{
    private sealed class Medida(int quantidade, string unidade) : ValueObject
    {
        protected override IEnumerable<object?> ObterComponentesDeIgualdade()
        {
            yield return quantidade;
            yield return unidade;
        }
    }

    [Fact]
    public void Objetos_de_valor_com_os_mesmos_componentes_sao_iguais()
    {
        var primeiro = new Medida(320, "páginas");
        var segundo = new Medida(320, "páginas");

        Assert.Equal(primeiro, segundo);
        Assert.True(primeiro == segundo);
        Assert.Equal(primeiro.GetHashCode(), segundo.GetHashCode());
    }

    [Fact]
    public void Objetos_de_valor_com_componentes_diferentes_nao_sao_iguais()
    {
        var primeiro = new Medida(320, "páginas");
        var segundo = new Medida(180, "páginas");

        Assert.NotEqual(primeiro, segundo);
        Assert.True(primeiro != segundo);
    }
}
