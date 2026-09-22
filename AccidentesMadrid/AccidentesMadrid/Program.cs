using System.Diagnostics;
using AccidentesMadrid.Repositories;
using AccidentesMadrid.Services;

// 1. Cargar y combinar los 3 ficheros mediante el repositorio (mapper + CsvHelper)
var swCarga = Stopwatch.StartNew();
var repo = AccidentesMemoryRepository.Instance;
var datos = (await repo.CargarArchivoCsvAsync()).ToList();
swCarga.Stop();
Console.WriteLine($"Lectura de ficheros: {datos.Count} accidentes en {swCarga.ElapsedMilliseconds} ms");

var servicio = new AccidentesServices();

// 2. Construir el DataFrame a partir de los objetos mapeados (columnas derivadas incluidas)
var swDf = Stopwatch.StartNew();
var df = AccidentesServices.ConstruirDataFrame(datos);
swDf.Stop();
Console.WriteLine($"Construcción del DataFrame: {swDf.ElapsedMilliseconds} ms");

// 3. Ejecutar consultas
var resultadosLinq = servicio.ConsultasLinq(datos);
var resultadosDataFrame = servicio.ConsultasDataFrame(df);

// 4. Imprimir la comparativa emparejando por número de consulta
Console.WriteLine("\n===============================================================================================");
Console.WriteLine($"{"#",-3} | {"Consulta",-38} | {"Tiempo LINQ",-12} | {"Tiempo DataFrame",-15}");
Console.WriteLine("===============================================================================================");

foreach (var r in resultadosLinq)
{
    var d = resultadosDataFrame.FirstOrDefault(x => x.Numero == r.Numero);
    var tiempoDf = d is null ? "-" : $"{d.Tiempo.TotalMilliseconds,8:F2} ms";
    Console.WriteLine($"{r.Numero,-3} | {r.Descripcion,-38} | {r.Tiempo.TotalMilliseconds,8:F2} ms | {tiempoDf} | {r.Valor}");
}