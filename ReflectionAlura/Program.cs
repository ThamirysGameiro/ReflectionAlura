using System;
using System.Collections.Generic;
using System.Linq;

namespace ReflectionAlura;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Controle de versões de dados \r\n");

        var pessoa = new Pessoa()
        {
            Id = 1,
            Nome = "Thamirys",
            Sobrenome = "Gameiro",
            DataNascimento = DateTime.Now,
            Salario = 1000,
            Endereco = new Endereco { Id = 1, Logradouro = "Rua 1" }
        };

        VersionadorDeObjetos.Versionar(pessoa);

        pessoa.Sobrenome = "Cavalcante";

        VersionadorDeObjetos.Versionar(pessoa);

        pessoa.Endereco.Logradouro = "Rua 2";

        VersionadorDeObjetos.Versionar(pessoa);



        var tipo = Type.GetType("ReflectionAlura.Pessoa");
        var novaPessoa = Activator.CreateInstance(tipo) as Pessoa;



        novaPessoa.Id = 1;
        novaPessoa.Nome = "Diego";
        novaPessoa.Sobrenome = "Cavalcante";
        novaPessoa.DataNascimento = DateTime.Now;
        novaPessoa.Salario = 100000;
        novaPessoa.Endereco = new Endereco { Id = 1, Logradouro = "Rua 3" };

        VersionadorDeObjetos.Versionar(novaPessoa);


        var versoes = VersionadorDeObjetos.ObterVersoesPorNomeTipoObjeto(typeof(Pessoa).Name);




        foreach (var item in versoes)
        {
            Console.WriteLine($"Versão: {item.DataHora:O}");

            foreach (var p in item.Propriedades)
            {
                Console.WriteLine($"    {p.Key} = {p.Value}");
            }

            Console.WriteLine("\r\n#################################\r\n");
        }


        

    }   
}


public class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Sobrenome { get; set; }
    public Endereco Endereco { get; set; }
    public DateTime DataNascimento { get; set; }
    public decimal Salario { get; set; }

}

public class Endereco
{
    public int Id { get; set; }
    public string Logradouro { get; set; }
}

static class VersionadorDeObjetos
{
    private static List<VersaoDoObjeto> bancoDeDados = new List<VersaoDoObjeto>();

    public static void Versionar(object instancia)
    {
        var nomeTipoObjeto = instancia.GetType().Name;
        var propriedades = ObterPropriedades(instancia);
        var ultimaVersao = bancoDeDados.LastOrDefault(x => x.NomeTipoObjeto == nomeTipoObjeto);

        var versao = new VersaoDoObjeto()
        {
            NomeTipoObjeto = nomeTipoObjeto,
            Propriedades = propriedades
        };

        bancoDeDados.Add(versao);

    }

    public static List<VersaoDoObjeto> ObterVersoesPorNomeTipoObjeto(string nomeTipoObjeto)
    {
        return bancoDeDados.Where(x => x.NomeTipoObjeto == nomeTipoObjeto).ToList();
    }

    private static Dictionary<string, object> ObterPropriedades(object instancia)
    {
        var tipo = instancia.GetType();
        var propriedades = tipo.GetProperties();

        var dicionario = new Dictionary<string, object>();

        foreach (var p in propriedades)
        {
            var valor = p.GetValue(instancia);

            if (TipoPrimitivo(p.PropertyType))
            {
                dicionario.Add($"{tipo.Name}.{p.Name}", valor);
            }
            else
            {
                var recursao = ObterPropriedades(valor);
                foreach (var item in recursao)
                {
                    dicionario.Add(item.Key, item.Value);
                }
            }

        }

        return dicionario;
    }

    private static bool TipoPrimitivo(Type type)
    {
        return (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(char) || type == typeof(decimal) || type == typeof(double)
            || type == typeof(float) || type == typeof(int) || type == typeof(uint)
            || type == typeof(long) || type == typeof(ulong) || type == typeof(short)
            || type == typeof(ushort) || type == typeof(string) || type == typeof(DateTime));
    }

}

class VersaoDoObjeto
{
    public DateTime DataHora { get; set; } = DateTime.Now;
    public string NomeTipoObjeto { get; set; }
    public Dictionary<string, object> Propriedades { get; set; }


}