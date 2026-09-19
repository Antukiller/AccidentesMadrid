using System.Security.Cryptography.X509Certificates;
using AccidentesMadrid.Enum;

namespace AccidentesMadrid.Models;

public class Accidentes {
    public int Id { get; init; }

    public string NumeroExpediente { get; init; } = string.Empty;

    public DateOnly Fecha { get; init; }

    public TimeOnly Hora { get; init; } = TimeOnly.FromDateTime(DateTime.UtcNow);
    
    public string Localizacion { get; init; }
    
    public int NumeroCalle { get; init; }
    
    public int CodigoDistrito { get; init; }
    
    public string Distrito { get; init; }
    
    public TipoAccidente Accidente { get; init; }
    
    public string EstadoMeteorlogico { get; init; }
    
    public string TipoVehiculo { get; init; }
    
    public string TipoPersona { get; init; }
    
    public string rangoEdad { get; init; }
    
    public Sexo Sexo { get; init; }
    
    public string lesividad { get; init; }
    
    public int CoordenadaYUtm { get; init; }
    
    public int CoordenadaXUtm { get; init; }
    
    public bool positivaAlcohol { get; init; }
    
    public bool positivaDroga { get; init; }
    
}