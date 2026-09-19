using AccidentesMadrid.Enum;

namespace AccidentesMadrid.Models;

public class Accidentes {
    public int Id { get; internal set; }

    public string NumeroExpediente { get; init; } = string.Empty;

    public DateOnly Fecha { get; init; }

    public TimeOnly Hora { get; init; }

    public string Localizacion { get; init; } = string.Empty;

    public int? NumeroCalle { get; init; }

    public int CodigoDistrito { get; init; }

    public string Distrito { get; init; } = string.Empty;

    public string TipoAccidente { get; init; } = string.Empty;

    public string EstadoMeteorologico { get; init; } = string.Empty;

    public string TipoVehiculo { get; init; } = string.Empty;

    public string TipoPersona { get; init; } = string.Empty;

    public string RangoEdad { get; init; } = string.Empty;

    public Sexo Sexo { get; init; }

    public CodigoAccidente CodigoAccidente { get; init; }

    public string Lesividad { get; init; } = string.Empty;

    public double CoordenadaXUtm { get; init; }

    public double CoordenadaYUtm { get; init; }

    public bool PositivaAlcohol { get; init; }

    public bool PositivaDroga { get; init; }

    public int Anio => Fecha.Year;

    public int Mes => Fecha.Month;

    public int Dia => Fecha.Day;

    public DayOfWeek DiaSemana => Fecha.DayOfWeek;

    public bool EsFinDeSemana => DiaSemana is DayOfWeek.Saturday or DayOfWeek.Sunday;
}