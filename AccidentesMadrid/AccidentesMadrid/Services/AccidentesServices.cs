using AccidentesMadrid.Dto;
using AccidentesMadrid.Enum;
using AccidentesMadrid.Models;
using AccidentesMadrid.Repositories;
using Microsoft.Data.Analysis;

namespace AccidentesMadrid.Services;

public class AccidentesServices : IAccidentesServices {
    public Task<IEnumerable<Accidentes>> GetAllAsync(int page = 1, int pageSize = 10, bool includeDeleted = false)
        => AccidentesMemoryRepository.Instance.GetAllAsync(page - 1, pageSize, includeDeleted);

    private static readonly string[] DiasSemana =
        ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    public IReadOnlyList<ResultadoConsulta> ConsultasLinq(IEnumerable<Accidentes> datos)
    {
        // Materializamos UNA sola vez para no re-enumerar el IEnumerable en cada consulta
        var lista = datos.ToList();
        var resultados = new List<ResultadoConsulta>();
        var sw = new System.Diagnostics.Stopwatch();

        // Consulta 1: Total de accidentes
        sw.Restart();
        int total = lista.Count;
        sw.Stop();
        resultados.Add(new ResultadoConsulta(1, "Total de accidentes", total.ToString("N0"), sw.Elapsed));

        // Consulta 2: Accidentes por distrito (top 5)
        sw.Restart();
        var topDistritos = lista
            .GroupBy(a => a.Distrito)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(2, "Accidentes por distrito (top 5)", string.Join(", ", topDistritos), sw.Elapsed));

        // Consulta 3: Accidentes por tipo
        sw.Restart();
        var tipoAccidentes = lista
            .GroupBy(a => a.TipoAccidente)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(3, "Tipo de accidente", string.Join(", ", tipoAccidentes), sw.Elapsed));

        // Consulta 4: Accidentes por estado meteorológico
        sw.Restart();
        var accidenteEstadoMeteorologico = lista
            .GroupBy(a => a.EstadoMeteorologico)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(4, "Accidentes por estado meteorológico", string.Join(", ", accidenteEstadoMeteorologico), sw.Elapsed));

        // Consulta 5: Accidentes por sexo
        sw.Restart();
        var accidentePorSexo = lista
            .GroupBy(a => a.Sexo)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(5, "Accidentes por sexo", string.Join(", ", accidentePorSexo), sw.Elapsed));

        // Consulta 6: Accidentes por rango de edad
        sw.Restart();
        var rangoEdad = lista
            .GroupBy(a => a.RangoEdad)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(6, "Accidentes por rango de edad", string.Join(", ", rangoEdad), sw.Elapsed));

        // Consulta 7: Positivos en alcohol
        sw.Restart();
        var positivoAlcohol = lista
            .GroupBy(a => a.PositivaAlcohol)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(7, "Positivos en alcohol", string.Join(", ", positivoAlcohol), sw.Elapsed));

        // Consulta 8: Positivos en drogas
        sw.Restart();
        var positivoDroga = lista
            .GroupBy(a => a.PositivaDroga)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(8, "Positivos en drogas", string.Join(", ", positivoDroga), sw.Elapsed));

        // Consulta 9: Accidentes por día de la semana (Fecha.DayOfWeek, no Dia del mes)
        sw.Restart();
        var accidentesPorDiaSemana = lista
            .GroupBy(a => a.Fecha.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{DiasSemana[(int)g.Key]} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(9, "Accidentes por día de la semana", string.Join(", ", accidentesPorDiaSemana), sw.Elapsed));

        // Consulta 10: Accidentes por mes (Fecha.Month)
        sw.Restart();
        var accidentePorMes = lista
            .GroupBy(a => a.Fecha.Month)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key:00} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(10, "Accidentes por mes", string.Join(", ", accidentePorMes), sw.Elapsed));

        // Consulta 11: Hora con más accidentes (agrupamos por hora, no por Hora:min:seg)
        sw.Restart();
        var horaTop = lista
            .GroupBy(a => a.Hora.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => $"Las {g.Key:00}:00 ({g.Count()} accidentes)")
            .FirstOrDefault();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(11, "Hora con más accidentes", horaTop ?? "Sin datos", sw.Elapsed));

        // Consulta 12: Lesiones más frecuentes
        sw.Restart();
        var lesionesMasFrecuentes = lista
            .GroupBy(a => a.Lesividad)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(12, "Lesiones más frecuentes", string.Join(", ", lesionesMasFrecuentes), sw.Elapsed));

        // Consulta 13: Tipo de vehículo más implicado
        sw.Restart();
        var tipoVehiculo = lista
            .GroupBy(a => a.TipoVehiculo)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();
        sw.Stop();
        resultados.Add(new ResultadoConsulta(13, "Tipo de vehículo más implicado", string.Join(", ", tipoVehiculo), sw.Elapsed));

        // Consulta 14: Accidentes con peatones (valores reales: "Peatón", "Peatón (atropello sc)")
        sw.Restart();
        var accidentesPeatones = lista
            .Count(a => a.TipoPersona.StartsWith("Peatón", StringComparison.Ordinal));
        sw.Stop();
        resultados.Add(new ResultadoConsulta(14, "Accidentes con peatones", accidentesPeatones.ToString("N0"), sw.Elapsed));

        // Consulta 15: Proporción hombre/mujer
        sw.Restart();
        var hombres = lista.Count(a => a.Sexo == Sexo.Hombre);
        var mujeres = lista.Count(a => a.Sexo == Sexo.Mujer);
        var proporcion = mujeres == 0 ? "Sin mujeres" : $"{hombres / (double)mujeres:F2}";
        sw.Stop();
        resultados.Add(new ResultadoConsulta(15, "Proporción hombre/mujer", proporcion, sw.Elapsed));

        return resultados;
    }

    // ---- Construcción del DataFrame a partir de los objetos ya mapeados ----
    // No usamos DataFrame.LoadCsv porque el CSV crudo no tiene columnas derivadas
    // (mes, dia_semana, fin_semana...) ni el alcohol/droga como booleano.
    public static DataFrame ConstruirDataFrame(List<Accidentes> datos) => new(
        new PrimitiveDataFrameColumn<int>("anio", datos.Select(a => a.Fecha.Year)),
        new PrimitiveDataFrameColumn<int>("mes", datos.Select(a => a.Fecha.Month)),
        new PrimitiveDataFrameColumn<int>("dia", datos.Select(a => a.Fecha.Day)),
        new StringDataFrameColumn("dia_semana", datos.Select(a => DiasSemana[(int)a.Fecha.DayOfWeek])),
        new PrimitiveDataFrameColumn<int>("hora", datos.Select(a => a.Hora.Hour)),
        new StringDataFrameColumn("fecha", datos.Select(a => a.Fecha.ToString("yyyy-MM-dd"))),
        new PrimitiveDataFrameColumn<int>("cod_distrito", datos.Select(a => a.CodigoDistrito)),
        new StringDataFrameColumn("distrito", datos.Select(a => a.Distrito)),
        new StringDataFrameColumn("tipo_accidente", datos.Select(a => a.TipoAccidente)),
        new StringDataFrameColumn("estado_meteorológico", datos.Select(a => a.EstadoMeteorologico)),
        new StringDataFrameColumn("tipo_vehiculo", datos.Select(a => a.TipoVehiculo)),
        new StringDataFrameColumn("tipo_persona", datos.Select(a => a.TipoPersona)),
        new StringDataFrameColumn("rango_edad", datos.Select(a => a.RangoEdad)),
        new StringDataFrameColumn("sexo", datos.Select(a => a.Sexo.ToString())),
        new StringDataFrameColumn("lesividad", datos.Select(a => a.Lesividad)),
        new PrimitiveDataFrameColumn<bool>("alcohol", datos.Select(a => a.PositivaAlcohol)),
        new PrimitiveDataFrameColumn<bool>("droga", datos.Select(a => a.PositivaDroga)),
        new PrimitiveDataFrameColumn<bool>("fin_semana",
            datos.Select(a => a.Fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)),
        new StringDataFrameColumn("anio_mes", datos.Select(a => $"{a.Fecha.Year}-{a.Fecha.Month:00}")),
        new StringDataFrameColumn("anio_distrito", datos.Select(a => $"{a.Fecha.Year}|{a.Distrito}")),
        new PrimitiveDataFrameColumn<double>("uno", datos.Select(_ => 1.0)));

    public IReadOnlyList<ResultadoConsulta> ConsultasDataFrame(DataFrame df)
    {
        var resultados = new List<ResultadoConsulta>();
        var sw = new System.Diagnostics.Stopwatch();

        // 1. Total de accidentes
        sw.Restart();
        int total = (int)df.Rows.Count;
        sw.Stop();
        resultados.Add(new ResultadoConsulta(1, "Total de accidentes", total.ToString("N0"), sw.Elapsed));

        // 2. Accidentes por distrito (top 5)
        sw.Restart();
        var distritosGroup = Agrupar(df, "distrito").OrderByDescending("uno", false);
        var top5Distritos = new List<string>();
        for (int i = 0; i < Math.Min(5, distritosGroup.Rows.Count); i++)
            top5Distritos.Add($"{distritosGroup["distrito"][i]} ({Convert.ToInt64(distritosGroup["uno"][i]):0})");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(2, "Accidentes por distrito (top 5)", string.Join(", ", top5Distritos), sw.Elapsed));

        // 3. Tipo de accidente
        sw.Restart();
        var tiposList = ObtenerListaAgrupada(Agrupar(df, "tipo_accidente").OrderByDescending("uno", false), "tipo_accidente");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(3, "Tipo de accidente", string.Join(", ", tiposList), sw.Elapsed));

        // 4. Estado meteorológico
        sw.Restart();
        var meteoList = ObtenerListaAgrupada(Agrupar(df, "estado_meteorológico").OrderByDescending("uno", false), "estado_meteorológico");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(4, "Estado meteorológico", string.Join(", ", meteoList), sw.Elapsed));

        // 5. Sexo
        sw.Restart();
        var sexoList = ObtenerListaAgrupada(Agrupar(df, "sexo").OrderByDescending("uno", false), "sexo");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(5, "Accidentes por sexo", string.Join(", ", sexoList), sw.Elapsed));

        // 6. Rango de edad
        sw.Restart();
        var edadList = ObtenerListaAgrupada(Agrupar(df, "rango_edad").OrderByDescending("uno", false), "rango_edad");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(6, "Accidentes por rango de edad", string.Join(", ", edadList), sw.Elapsed));

        // 7. Positivos en alcohol
        sw.Restart();
        var alcList = ObtenerListaAgrupada(Agrupar(df, "alcohol"), "alcohol");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(7, "Positivos en alcohol", string.Join(", ", alcList), sw.Elapsed));

        // 8. Positivos en drogas
        sw.Restart();
        var drogaList = ObtenerListaAgrupada(Agrupar(df, "droga"), "droga");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(8, "Positivos en drogas", string.Join(", ", drogaList), sw.Elapsed));

        // 9. Día de la semana
        sw.Restart();
        var diaList = ObtenerListaAgrupada(Agrupar(df, "dia_semana").OrderByDescending("uno", false), "dia_semana");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(9, "Accidentes por día de la semana", string.Join(", ", diaList), sw.Elapsed));

        // 10. Mes
        sw.Restart();
        var mesList = ObtenerListaAgrupada(Agrupar(df, "mes").OrderByDescending("uno", false), "mes");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(10, "Accidentes por mes", string.Join(", ", mesList), sw.Elapsed));

        // 11. Hora con más accidentes
        sw.Restart();
        var horaGroup = Agrupar(df, "hora").OrderByDescending("uno", false);
        string horaTop = horaGroup.Rows.Count > 0
            ? $"Las {Convert.ToInt32(horaGroup["hora"][0]):00}:00 ({Convert.ToInt64(horaGroup["uno"][0]):0} casos)"
            : "Sin datos";
        sw.Stop();
        resultados.Add(new ResultadoConsulta(11, "Hora con más accidentes", horaTop, sw.Elapsed));

        // 12. Lesiones más frecuentes
        sw.Restart();
        var lesList = ObtenerListaAgrupada(Agrupar(df, "lesividad").OrderByDescending("uno", false), "lesividad");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(12, "Lesiones más frecuentes", string.Join(", ", lesList), sw.Elapsed));

        // 13. Tipo de vehículo
        sw.Restart();
        var vehList = ObtenerListaAgrupada(Agrupar(df, "tipo_vehiculo").OrderByDescending("uno", false), "tipo_vehiculo");
        sw.Stop();
        resultados.Add(new ResultadoConsulta(13, "Tipo de vehículo más implicado", string.Join(", ", vehList), sw.Elapsed));

        return resultados;
    }

    // Agrupar por una clave y contar usando la columna "uno" de 1s:
    // GroupBy(x).Count() NO crea una columna "count" (replica el conteo en todas
    // las columnas), así que la forma correcta es Sum("uno") -> [clave, uno].
    private static DataFrame Agrupar(DataFrame df, string columna) => df.GroupBy(columna).Sum("uno");

    private static List<string> ObtenerListaAgrupada(DataFrame dfGroup, string colNombre)
    {
        var lista = new List<string>();
        for (int i = 0; i < dfGroup.Rows.Count; i++)
            lista.Add($"{dfGroup[colNombre][i]} ({Convert.ToInt64(dfGroup["uno"][i]):0})");
        return lista;
    }
}