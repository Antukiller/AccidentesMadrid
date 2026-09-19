namespace AccidentesMadrid.Repositories;

public interface ICrudRepository<TKey, TEntity> where TEntity : class {
    // Devuelve una colección de entidades de forma asíncrona
    Task<IEnumerable<TEntity>> GetAllAsync(int pagina, int tamanhoPagina, bool isDeleteInclude);
}