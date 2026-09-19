using System.Globalization;
using AccidentesMadrid.Enum;
using AccidentesMadrid.Models;
using CsvHelper.Configuration;

namespace AccidentesMadrid.Mapper;

public class AccidenteMapper : ClassMap<Accidentes>
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    public AccidenteMapper()
    {
        Map(m => m.NumeroExpediente).Name("num_expediente");
        Map(m => m.Localizacion).Name("localizacion");
        Map(m => m.Distrito).Name("distrito");
        Map(m => m.EstadoMeteorologico).Name("estado_meteorológico");
        Map(m => m.TipoVehiculo).Name("tipo_vehiculo");
        Map(m => m.TipoPersona).Name("tipo_persona");
        Map(m => m.RangoEdad).Name("rango_edad");
        Map(m => m.Lesividad).Name("lesividad");

        Map(m => m.Fecha).Name("fecha").Convert(args =>
            DateOnly.TryParseExact(args.Row.GetField("fecha"), "dd/MM/yyyy", Es, DateTimeStyles.None, out var f)
                ? f
                : DateOnly.MinValue);

        Map(m => m.Hora).Name("hora").Convert(args =>
            TimeOnly.TryParseExact(args.Row.GetField("hora"), new[] { "H:mm:ss", "HH:mm:ss" }, Es, DateTimeStyles.None, out var h)
                ? h
                : TimeOnly.MinValue);

        Map(m => m.NumeroCalle).Name("numero").Convert(args =>
            int.TryParse(args.Row.GetField("numero"), NumberStyles.Integer, Es, out var n) ? n : null);

        Map(m => m.CodigoDistrito).Name("cod_distrito").Convert(args =>
            int.TryParse(args.Row.GetField("cod_distrito"), NumberStyles.Integer, Es, out var cd) ? cd : 0);

        Map(m => m.TipoAccidente).Name("tipo_accidente");

        Map(m => m.Sexo).Name("sexo").Convert(args =>
            args.Row.GetField("sexo")?.Trim().ToLowerInvariant() switch
            {
                "hombre" => Sexo.Hombre,
                "mujer" => Sexo.Mujer,
                _ => Sexo.NoAsignado
            });

        Map(m => m.CodigoAccidente).Name("cod_lesividad").Convert(args =>
            int.TryParse(args.Row.GetField("cod_lesividad"), NumberStyles.Integer, Es, out var lesividad) &&
            System.Enum.IsDefined(typeof(CodigoAccidente), lesividad)
                ? (CodigoAccidente)lesividad
                : CodigoAccidente.Desconocida);

        Map(m => m.CoordenadaXUtm).Name("coordenada_x_utm").Convert(args =>
            double.TryParse(args.Row.GetField("coordenada_x_utm"), NumberStyles.Float, Es, out var x) ? x : 0);

        Map(m => m.CoordenadaYUtm).Name("coordenada_y_utm").Convert(args =>
            double.TryParse(args.Row.GetField("coordenada_y_utm"), NumberStyles.Float, Es, out var y) ? y : 0);

        Map(m => m.PositivaAlcohol).Name("positiva_alcohol").Convert(args =>
            string.Equals(args.Row.GetField("positiva_alcohol")?.Trim(), "S", StringComparison.OrdinalIgnoreCase));

        Map(m => m.PositivaDroga).Name("positiva_droga").Convert(args =>
            string.Equals(args.Row.GetField("positiva_droga")?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
    }
}