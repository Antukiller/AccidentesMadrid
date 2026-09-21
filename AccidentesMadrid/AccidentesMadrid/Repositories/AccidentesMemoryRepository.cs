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

    private AccidentesMemoryRepository() { }
    
    public Task<IEnumerable<Accidentes>> GetAllAsync(int pagina, int tamanhoPagina, bool isDeleteInclude = false) {
        _logger.Debug("Obtenemos accidentes - página {Pagina}, tamaño {Tamanho}", pagina, tamanhoPagina);

        // Aplicamos la fórmula de paginación (pagina - 1) para que la página 1 empiece desde el inicio
        var resultado = _accidentes
            .OrderBy(a => a.Id) // Garantiza que las páginas siempre vengan en el mismo orden
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return Task.FromResult<IEnumerable<Accidentes>>(resultado);
    }

    public async Task<IEnumerable<Accidentes>> CargarArchivoCsvAsync() {
        var dataDir = ResolverDirectorioDatos();
        var ficheros = Directory.GetFiles(dataDir, "*.csv").OrderBy(f => f).ToList();

        if (ficheros.Count == 0)
            throw new FileNotFoundException($"No se encontraron ficheros CSV en {dataDir}");

        _accidentes.Clear();

        // 1. Lanzamos la lectura y parseo de los 3 archivos a la vez en paralelo
        var tareasLectura = ficheros.Select(fichero => Task.Run(() => LeerFichero(fichero))).ToArray();

        // 2. Esperamos a que los 3 archivos terminen de procesarse en segundo plano
        var resultadosPorFichero = await Task.WhenAll(tareasLectura);

        // 3. Combinamos todas las listas procesadas en tu lista principal
        var todosLosRegistros = resultadosPorFichero.SelectMany(r => r).ToList();

        // 4. Asignamos los IDs de forma secuencial y segura
        var id = 0;
        foreach (var registro in todosLosRegistros)
        {
            registro.Id = ++id;
            _accidentes.Add(registro);
        }

        _logger.Information("Total accidentes combinados: {Total}", _accidentes.Count);
        return _accidentes;
    }

    private List<Accidentes> LeerFichero(string fichero) {
        using var reader = new StreamReader(fichero, Encoding.UTF8);
        using var csv = new CsvReader(reader, _csvConfiguration);

        csv.Context.RegisterClassMap<AccidenteMapper>();

        // CsvHelper parsea todo el archivo a memoria RAM antes de cerrar el stream
        var registros = csv.GetRecords<Accidentes>().ToList();

        _logger.Information("Leídos {N} accidentes de {Fichero}", registros.Count, Path.GetFileName(fichero));
        return registros;
    }

    private static string ResolverDirectorioDatos() {
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