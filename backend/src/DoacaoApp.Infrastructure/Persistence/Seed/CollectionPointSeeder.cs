using System.Globalization;
using System.Text;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Infrastructure.Persistence.Seed;

/// <summary>
/// Popula a base com 50 pontos de coleta de demonstração — 10 no Oeste de Santa
/// Catarina (incluindo Concórdia) e 40 distribuídos pelo restante do país. Só roda
/// quando ainda não existe nenhum ponto cadastrado.
/// </summary>
public static class CollectionPointSeeder
{
    /// <summary>Cidade-base usada para gerar um ponto (UF, DDD e prefixo de CEP).</summary>
    private readonly record struct Place(string City, string State, string Ddd, string Cep5);

    private static readonly Place[] OesteSantaCatarina =
    [
        new("Concórdia", "SC", "49", "89700"),
        new("Chapecó", "SC", "49", "89800"),
        new("Xanxerê", "SC", "49", "89820"),
        new("Joaçaba", "SC", "49", "89600"),
        new("Videira", "SC", "49", "89560"),
        new("Caçador", "SC", "49", "89500"),
        new("São Miguel do Oeste", "SC", "49", "89900"),
        new("Seara", "SC", "49", "89770"),
        new("Pinhalzinho", "SC", "49", "89870"),
        new("Maravilha", "SC", "49", "89874"),
    ];

    private static readonly Place[] Brasil =
    [
        new("Florianópolis", "SC", "48", "88010"),
        new("Joinville", "SC", "47", "89201"),
        new("Blumenau", "SC", "47", "89010"),
        new("Itajaí", "SC", "47", "88301"),
        new("Criciúma", "SC", "48", "88801"),
        new("Lages", "SC", "49", "88501"),
        new("Balneário Camboriú", "SC", "47", "88330"),
        new("Brusque", "SC", "47", "88350"),
        new("Curitiba", "PR", "41", "80010"),
        new("Londrina", "PR", "43", "86010"),
        new("Maringá", "PR", "44", "87010"),
        new("Cascavel", "PR", "45", "85801"),
        new("Foz do Iguaçu", "PR", "45", "85851"),
        new("Ponta Grossa", "PR", "42", "84010"),
        new("Pato Branco", "PR", "46", "85501"),
        new("Umuarama", "PR", "44", "87501"),
        new("Porto Alegre", "RS", "51", "90010"),
        new("Caxias do Sul", "RS", "54", "95010"),
        new("Pelotas", "RS", "53", "96010"),
        new("Santa Maria", "RS", "55", "97010"),
        new("Passo Fundo", "RS", "54", "99010"),
        new("São Paulo", "SP", "11", "01001"),
        new("São Paulo", "SP", "11", "04001"),
        new("Campinas", "SP", "19", "13010"),
        new("Santos", "SP", "13", "11010"),
        new("Ribeirão Preto", "SP", "16", "14010"),
        new("São José dos Campos", "SP", "12", "12210"),
        new("Sorocaba", "SP", "15", "18010"),
        new("Bauru", "SP", "14", "17010"),
        new("Rio de Janeiro", "RJ", "21", "20010"),
        new("Niterói", "RJ", "21", "24020"),
        new("Campos dos Goytacazes", "RJ", "22", "28010"),
        new("Belo Horizonte", "MG", "31", "30110"),
        new("Uberlândia", "MG", "34", "38400"),
        new("Juiz de Fora", "MG", "32", "36010"),
        new("Contagem", "MG", "31", "32010"),
        new("Salvador", "BA", "71", "40010"),
        new("Brasília", "DF", "61", "70040"),
        new("Recife", "PE", "81", "50010"),
        new("Fortaleza", "CE", "85", "60010"),
    ];

    private static readonly string[] AssociationNames =
    [
        "Casa Solidária {city}",
        "Instituto Esperança {city}",
        "Associação Mãos que Ajudam {city}",
        "Centro Comunitário {bairro}",
        "Ação Solidária {city}",
    ];

    private static readonly string[] ChurchNames =
    [
        "Paróquia São {saint}",
        "Igreja Boa Nova {city}",
        "Comunidade Cristã {bairro}",
        "Paróquia Sagrado Coração {city}",
        "Igreja da Paz {city}",
    ];

    private static readonly string[] PublicNames =
    [
        "CRAS {bairro}",
        "Centro de Referência de Assistência Social {city}",
        "Secretaria de Assistência Social de {city}",
        "Posto de Coleta Municipal {bairro}",
        "Casa dos Conselhos {city}",
    ];

    private static readonly string[] Saints =
        ["Pedro", "João", "José", "Antônio", "Francisco", "Lucas", "Mateus", "Sebastião"];

    private static readonly string[] Streets =
    [
        "Rua XV de Novembro",
        "Avenida Brasil",
        "Rua Sete de Setembro",
        "Avenida Getúlio Vargas",
        "Rua do Comércio",
        "Avenida Santa Catarina",
        "Rua Marechal Deodoro",
        "Avenida Nereu Ramos",
        "Rua Coronel Bertaso",
        "Rua Anita Garibaldi",
    ];

    private static readonly string[] Districts =
    [
        "Centro",
        "São Cristóvão",
        "Jardim América",
        "Vila Nova",
        "Industrial",
        "Santa Maria",
        "Bela Vista",
        "São José",
        "Universitário",
        "Cidade Alta",
    ];

    private static readonly (string Name, string Icon)[] SampleCategories =
    [
        ("Alimentos", "apple"),
        ("Roupas", "shirt"),
        ("Calçados", "footprints"),
        ("Higiene", "spray-can"),
        ("Limpeza", "brush"),
        ("Móveis", "armchair"),
        ("Eletrodomésticos", "refrigerator"),
        ("Brinquedos", "puzzle"),
        ("Livros", "book-open"),
        ("Medicamentos", "pill"),
    ];

    private static readonly string[] Responsibles =
    [
        "Maria Aparecida Souza",
        "João Carlos Pereira",
        "Ana Paula Lima",
        "Carlos Eduardo Santos",
        "Fernanda Oliveira",
        "Roberto Alves",
        "Juliana Costa",
        "Marcos Vinícius Rocha",
        "Patrícia Gonçalves",
        "Luiz Henrique Martins",
        "Sandra Regina Dias",
        "Paulo Sérgio Fernandes",
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("CollectionPointSeeder");
        var dbContext = services.GetRequiredService<AppDbContext>();

        if (await dbContext.CollectionPoints.AnyAsync(cancellationToken))
        {
            return;
        }

        var repository = services.GetRequiredService<ICollectionPointRepository>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var now = services.GetRequiredService<IDateTimeProvider>().UtcNow;

        var random = new Random(20240622);
        var places = new List<Place>(OesteSantaCatarina.Length + Brasil.Length);
        places.AddRange(OesteSantaCatarina);
        places.AddRange(Brasil);

        var created = 0;
        for (var i = 0; i < places.Count; i++)
        {
            var place = places[i];
            var type = (CollectionPointType)((i % 3) + 1);
            var district = Districts[random.Next(Districts.Length)];

            var name = BuildName(type, place.City, district, random);

            var address = Address.Create(
                Streets[random.Next(Streets.Length)],
                random.Next(1, 3000).ToString(CultureInfo.InvariantCulture),
                complement: null,
                district,
                place.City,
                place.State,
                $"{place.Cep5}{random.Next(0, 1000):D3}");

            var contact = ContactInfo.Create(
                Responsibles[random.Next(Responsibles.Length)],
                $"contato.{Slug(place.City)}{i}@doacao.org.br",
                $"{place.Ddd}9{random.Next(0, 10000):D4}{random.Next(0, 10000):D4}");

            if (address.IsError || contact.IsError)
            {
                logger.LogWarning("Seed: dados inválidos para ponto em {City}, ignorado.", place.City);
                continue;
            }

            var point = CollectionPoint.Create(name, type, address.Value, contact.Value, now);
            if (point.IsError)
            {
                logger.LogWarning("Seed: falha ao criar ponto em {City}: {Error}", place.City, point.FirstError.Description);
                continue;
            }

            AddSampleCategories(point.Value, random);

            repository.Add(point.Value);
            created++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seed de pontos de coleta: {Created} criados ({Oeste} no Oeste de SC).",
            created,
            OesteSantaCatarina.Length);
    }

    private static void AddSampleCategories(CollectionPoint point, Random random)
    {
        var count = random.Next(1, 5);
        var pickedIndexes = new HashSet<int>();
        while (pickedIndexes.Count < count)
        {
            pickedIndexes.Add(random.Next(SampleCategories.Length));
        }

        foreach (var index in pickedIndexes)
        {
            var (categoryName, icon) = SampleCategories[index];
            point.AddCategory(categoryName, icon);
        }
    }

    private static string BuildName(CollectionPointType type, string city, string district, Random random)
    {
        var template = type switch
        {
            CollectionPointType.Association => AssociationNames[random.Next(AssociationNames.Length)],
            CollectionPointType.Church => ChurchNames[random.Next(ChurchNames.Length)],
            _ => PublicNames[random.Next(PublicNames.Length)],
        };

        return template
            .Replace("{city}", city)
            .Replace("{bairro}", district)
            .Replace("{saint}", Saints[random.Next(Saints.Length)]);
    }

    /// <summary>Slug ASCII (sem acentos/espaços) para compor e-mails válidos.</summary>
    private static string Slug(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }
}
