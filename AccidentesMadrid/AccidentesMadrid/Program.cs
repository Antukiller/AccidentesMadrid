using System.Diagnostics;
using System.Runtime.InteropServices;
using AccidentesMadrid.Dto;
using AccidentesMadrid.Repositories;
using AccidentesMadrid.Services;

// Cuántas veces se ejecuta cada técnica para medir mejor la diferencia
const int Rondas = 5;

// ---------------------------------------------------------------------------
// 0. Entorno
// ---------------------------------------------------------------------------
Console.WriteLine($"Entorno   : .NET {Environment.Version}");
Console.WriteLine($"CPU       : {Environment.ProcessorCount} núcleos lógicos | {RuntimeInformation.OSArchitecture}");
Console.WriteLine($"Medición  : media de {Rondas} ejecuciones por consulta (tras calentamiento del JIT)");
Console.WriteLine();

// ---------------------------------------------------------------------------
// 1. Carga de datos
// ---------------------------------------------------------------------------
var swCarga = Stopwatch.StartNew();
var repo = AccidentesMemoryRepository.Instance;
var datos = (await repo.CargarArchivoCsvAsync()).ToList();
swCarga.Stop();
Console.WriteLine($"Lectura de ficheros  : {datos.Count:N0} accidentes en {swCarga.ElapsedMilliseconds} ms");

var swDf = Stopwatch.StartNew();
var df = AccidentesServices.ConstruirDataFrame(datos);
swDf.Stop();
Console.WriteLine($"Construcción DataFrame: {swDf.ElapsedMilliseconds} ms");
Console.WriteLine();

var servicio = new AccidentesServices();

// Calentamiento: la primera ejecución paga la compilación JIT y no es representativa
servicio.ConsultasLinq(datos);
servicio.ConsultasDataFrame(df);

// ---------------------------------------------------------------------------
// 2. Ejecutar las N rondas y promediar por número de consulta.
//    Cada técnica se mide en su propia fase y se fuerza la recolección de basura
//    entre rondas: sin esto, el GC de una fase contamina las mediciones de la otra.
// ---------------------------------------------------------------------------
var rondasLinq = new List<IReadOnlyList<ResultadoConsulta>>();
var rondasDf = new List<IReadOnlyList<ResultadoConsulta>>();

for (int i = 0; i < Rondas; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    rondasLinq.Add(servicio.ConsultasLinq(datos));
}

for (int i = 0; i < Rondas; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    rondasDf.Add(servicio.ConsultasDataFrame(df));
}

var resultadosLinq = Promediar(rondasLinq);
var resultadosDf = Promediar(rondasDf);

// ---------------------------------------------------------------------------
// 3. Tabla comparativa
// ---------------------------------------------------------------------------
const int anchoConsulta = 42;

Console.WriteLine($"{"#",3} {"Consulta",-anchoConsulta} {"LINQ (ms)",11} {"DF (ms)",9}  Ganador");
Console.WriteLine(new string('-', 3 + 1 + anchoConsulta + 1 + 11 + 1 + 9 + 1 + 8));

for (int i = 0; i < resultadosLinq.Count; i++)
{
    var l = resultadosLinq[i];
    var d = resultadosDf[i];

    var ganador = l.Tiempo < d.Tiempo ? "LINQ" : d.Tiempo < l.Tiempo ? "DF" : "=";
    var tLinq = $"{l.Tiempo.TotalMilliseconds,7:F2}";
    var tDf = $"{d.Tiempo.TotalMilliseconds,7:F2}";

    // El tiempo del ganador se pinta en verde para leer la diferencia de un vistazo
    Console.ForegroundColor = ganador == "LINQ" ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.Write($"  {l.Numero,2} {Recortar(l.Descripcion, anchoConsulta),-anchoConsulta} {tLinq}");
    Console.ForegroundColor = ganador == "DF" ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.WriteLine($" {tDf}  {ganador,4}");
    Console.ResetColor();
}

Console.WriteLine(new string('-', 3 + 1 + anchoConsulta + 1 + 11 + 1 + 9 + 1 + 8));

// ---------------------------------------------------------------------------
// 4. Resumen
// ---------------------------------------------------------------------------
var totalLinq = resultadosLinq.Sum(r => r.Tiempo.TotalMilliseconds);
var totalDf = resultadosDf.Sum(r => r.Tiempo.TotalMilliseconds);
var ganaLinq = resultadosLinq.Count(r => r.Tiempo < resultadosDf.First(d => d.Numero == r.Numero).Tiempo);
var ganaDf = resultadosDf.Count(r => r.Tiempo < resultadosLinq.First(l => l.Numero == r.Numero).Tiempo);

Console.WriteLine($"TOTAL ({Rondas} rondas promediadas) -> LINQ: {totalLinq,7:F2} ms | DataFrame: {totalDf,7:F2} ms");
Console.WriteLine($"Diferencias detectadas  -> LINQ ganó en {ganaLinq} consultas, DataFrame en {ganaDf}");
Console.WriteLine($"Nota: las consultas 25, 28 y 29 del lado LINQ usan PLINQ (AsParallel).");
Console.WriteLine();

// ---------------------------------------------------------------------------
// 5. Detalle de resultados (valor truncado a ~95 caracteres)
// ---------------------------------------------------------------------------
Console.WriteLine("RESULTADOS DE LAS CONSULTAS (LINQ = DataFrame porque consultan los mismos datos):");
foreach (var r in resultadosLinq)
    Console.WriteLine($"{"#"}{r.Numero,-3} {Recortar(r.Descripcion, anchoConsulta),-anchoConsulta} -> {Recortar(r.Valor, 95)}");

// ---------------------------------------------------------------------------
// Averigua quién gana: promedio de N rondas por número de consulta
// ---------------------------------------------------------------------------
static IReadOnlyList<ResultadoConsulta> Promediar(IReadOnlyList<IReadOnlyList<ResultadoConsulta>> rondas)
{
    var acumulado = new Dictionary<int, (long Ticks, ResultadoConsulta Muestra)>();

    foreach (var ronda in rondas)
        foreach (var r in ronda)
        {
            if (acumulado.TryGetValue(r.Numero, out var actual))
                acumulado[r.Numero] = (actual.Ticks + r.Tiempo.Ticks, actual.Muestra);
            else
                acumulado[r.Numero] = (r.Tiempo.Ticks, r);
        }

    return acumulado
        .OrderBy(x => x.Key)
        .Select(x => new ResultadoConsulta(
            x.Value.Muestra.Numero,
            x.Value.Muestra.Descripcion,
            x.Value.Muestra.Valor,
            TimeSpan.FromTicks(x.Value.Ticks / rondas.Count)))
        .ToList();
}

static string Recortar(string texto, int max)
    => texto.Length <= max ? texto : texto[..(max - 1)] + "…";