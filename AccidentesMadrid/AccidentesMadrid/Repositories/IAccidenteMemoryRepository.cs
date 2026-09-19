using System.Collections;
using AccidentesMadrid.Models;

namespace AccidentesMadrid.Repositories;

public interface IAccidenteMemoryRepository : ICrudRepository<int, Accidentes> {

    Task<IEnumerable<Accidentes>> CargarArchivoCsvAsync();
}