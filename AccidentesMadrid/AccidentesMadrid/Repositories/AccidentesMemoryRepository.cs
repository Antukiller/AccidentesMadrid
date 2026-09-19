using System.Globalization;
using System.Text;
using AccidentesMadrid.Mapper;
using AccidentesMadrid.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Serilog;

namespace AccidentesMadrid.Repositories;

public class AccidentesMemoryRepository : IAccidenteMemoryRepository
{
    private static readonly Lazy<AccidentesMemoryRepository> Lazy = new(() => new AccidentesMemoryRepository());

    public static AccidentesMemoryRepository Instance => Lazy.Value;

    private readonly ILogger _logger = Log.ForContext<AccidentesMemoryRepository>();

    private readonly List<Accidentes> _accidentes = [];

    private readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null,
    };

    private AccidentesMemoryRepository()
    {
    }

    public Task<IEnumerable<Accidentes>> GetAllAsync(int pagina, int tamanhoPagina, bool isDeleteInclude)
    {
        _logger.Debug("Obtenemos accidentes - página {Pagina}, tamaño {Tamanho}", pagina, tamanhoPagina);
        return Task.FromResult(_accidentes.Skip(pagina * tamanhoPagina).Take(tamanhoPagina));
    }

    public IEnumerable<Accidentes> GetAll() => _accidentes;

    public Task<IEnumerable<Accidentes>> CargarArchivoCsvAsync()
    {
        var dataDir = ResolverDirectorioDatos();
        var ficheros = Directory.GetFiles(dataDir, "*.csv").OrderBy(f => f).ToList();

        if (ficheros.Count == 0)
            throw new FileNotFoundException($"No se encontraron ficheros CSV en {dataDir}");

        _accidentes.Clear();

        var id = 0;
        foreach (var fichero in ficheros)
        {
            var registros = LeerFichero(fichero).ToList();
            foreach (var registro in registros)
                registro.Id = ++id;

            _logger.Information("Leídos {N} accidentes de {Fichero}", registros.Count, Path.GetFileName(fichero));
            _accidentes.AddRange(registros);
        }

        _logger.Information("Total accidentes combinados: {Total}", _accidentes.Count);
        return Task.FromResult<IEnumerable<Accidentes>>(_accidentes);
    }

    private IEnumerable<Accidentes> LeerFichero(string fichero)
    {
        using var reader = new StreamReader(fichero, Encoding.UTF8);
        using var csv = new CsvReader(reader, _csvConfiguration);
        csv.Context.RegisterClassMap<AccidenteMapper>();
        foreach (var registro in csv.GetRecords<Accidentes>())
            yield return registro;
    }

    private static string ResolverDirectorioDatos()
    {
        var directorio = Directory.GetCurrentDirectory();
        while (directorio is not null)
        {
            var candidato = Path.Combine(directorio, "data");
            if (Directory.Exists(candidato))
                return candidato;
            directorio = Directory.GetParent(directorio)?.FullName;
        }

        throw new DirectoryNotFoundException("No se encontró el directorio data/ con los ficheros CSV.");
    }
}