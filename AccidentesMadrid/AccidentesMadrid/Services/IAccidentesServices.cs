using AccidentesMadrid.Dto;
using AccidentesMadrid.Models;
using Microsoft.Data.Analysis;

namespace AccidentesMadrid.Services;

public interface IAccidentesServices 
{
    Task<IEnumerable<Accidentes>> GetAllAsync(int page = 1, int pageSize = 10, bool includeDeleted = false);

    // Le pasas los datos y te devuelve la lista de resultados tabulados
    IReadOnlyList<ResultadoConsulta> ConsultasLinq(IEnumerable<Accidentes> datos);
    
    // Si usas C# DataFrames (Microsoft.Data.Analysis)
    IReadOnlyList<ResultadoConsulta> ConsultasDataFrame(DataFrame df);
}