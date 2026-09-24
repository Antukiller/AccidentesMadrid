# Práctica 5 · Accidentes de Madrid

Análisis de los **accidentes de tráfico en Madrid (2024-2026)** comparando dos técnicas de consulta:

1. **LINQ / PLINQ** → consultas sobre objetos en memoria (gestión clásica de colecciones en .NET).
2. **Microsoft.Data.Analysis (DataFrames)** → consultas sobre una tabla columnar en memoria (estilo Python/pandas).

El objetivo de la práctica es **ver con datos reales cuándo conviene cada técnica**.

---

## 1. ¿Qué problema resuelve este proyecto?

Tenemos **130.864 registros** de accidentes repartidos en 3 ficheros CSV del Ayuntamiento de Madrid
(uno por cada año: 2024, 2025 y 2026). Cada registro tiene campos como:

- `fecha` y `hora` del accidente
- `distrito` y `cod_distrito`
- `tipo_accidente`, `estado_meteorológico`, `tipo_vehículo`, `tipo_persona`
- `rango_edad`, `sexo`
- `lesividad` (gravedad de las lesiones)
- `positiva_alcohol` y `positiva_droga` (S/N)

Y queremos responder **30 preguntas** sobre esos datos (las consultas del apartado 5),
haciendo cada una **de dos formas distintas** para comparar su rendimiento.

---

## 2. Estructura del proyecto

```
AccidentesMadrid/                         <- raíz del repositorio (aquí está este README)
└── AccidentesMadrid/                     <- solución
    ├── AccidentesMadrid.slnx
    └── AccidentesMadrid/                 <- proyecto
        ├── AccidentesMadrid.csproj
        ├── Program.cs                    <- arranque + tabla comparativa de tiempos
        ├── Dto/ResultadoConsultado.cs    <- DTO que "empaqueta" cada resultado
        ├── Enum/                         <- enumerados (Sexo, CodigoAccidente)
        ├── Mapper/AccidenteMapper.cs     <- mapea CSV -> objeto (CsvHelper)
        ├── Models/Accidentes.cs          <- modelo (una fila del CSV)
        ├── Repositories/AccidentesMemoryRepository.cs <- lee y combina los CSV
        ├── Services/AccidentesServices.cs <- LAS 60 CONSULTAS (30 LINQ + 30 DataFrame)
        ├── data/                         <- los 3 ficheros CSV
        ├── Dockerfile
        ├── docker-compose.yml
        └── .dockerignore
```

> Importante: el proyecto es **una aplicación de consola**. Se ejecuta, imprime resultados y termina.

---

## 3. Cómo leer los datos: capa de repositorio y mapeo

Los datos vienen en texto plano (CSV). Antes de poder consultarlos hay que:

1. **Leer el fichero** (con codificación UTF-8, separador `;`).
2. **Convertir cada fila a un objeto** `Accidentes` de C#.

Esto se hace en dos clases:

### `Mapper/AccidenteMapper.cs`

Usa la librería **CsvHelper** con un *ClassMap*: le dice a CsvHelper **qué columna del CSV
corresponde a qué propiedad del objeto** y **cómo convertir el texto**.

Ejemplos de conversiones "trampa":
- La fecha viene como `dd/MM/yyyy` (formato europeo) → `DateOnly.TryParseExact`.
- Las coordenadas usan **coma decimal** (ej. `441234,56`) → cultura `es-ES`.
- `positiva_alcohol` viene como `S` / `N` → se convierte a `bool` (`S` → `true`).
- `cod_lesividad` es un número → se convierte al enumerado `CodigoAccidente`.

```csharp
Map(m => m.PositivaAlcohol).Name("positiva_alcohol").Convert(args =>
    string.Equals(args.Row.GetField("positiva_alcohol")?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
```

### `Repositories/AccidentesMemoryRepository.cs`

- Busca la carpeta `data/` (subiendo directorios desde el que se ejecuta el programa).
- Lee **todos** los `*.csv` que encuentre, **en paralelo** (`Task.Run` por fichero).
- Combina los 3 ficheros en una lista única y les asigna un `Id` secuencial.

```csharp
var tareasLectura = ficheros.Select(fichero => Task.Run(() => LeerFichero(fichero))).ToArray();
var resultadosPorFichero = await Task.WhenAll(tareasLectura);
```

Resultado: **una lista de 130.864 objetos `Accidentes`** lista para consultar.

### `Models/Accidentes.cs`

Representa una fila. **No incluye propiedades calculadas** (año, mes, día de la semana...),
porque esas se derivan en las consultas a partir de `Fecha` (`a.Fecha.Year`, `a.Fecha.DayOfWeek`).

```csharp
public DateOnly Fecha { get; init; }
public TimeOnly Hora { get; init; }
public string Distrito { get; init; } = string.Empty;
public bool PositivaAlcohol { get; init; }
// ...
```

---

## 4. Las dos técnicas: LINQ vs DataFrame

### 4.1 LINQ (Language Integrated Query)

Es el lenguaje de consultas **integrado en C#**. Trabaja sobre colecciones de objetos
(`IEnumerable<Accidentes>`) usando métodos como `Where`, `GroupBy`, `OrderByDescending`, `Take`.

- Todo se ejecuta en **un solo hilo** (salvo que uses `AsParallel()`, que entonces es **PLINQ**)
  y opera sobre los objetos directamente.

```csharp
var topDistritos = lista
    .GroupBy(a => a.Distrito)              // agrupa por distrito
    .OrderByDescending(g => g.Count())     // ordena por número de accidentes descendente
    .Take(5)                               // se queda con los 5 primeros
    .Select(g => $"{g.Key} ({g.Count()})") // formatea el texto del resultado
    .ToList();
```

### 4.2 DataFrame (Microsoft.Data.Analysis)

Es una librería que aporta la estructura **tabla** (filas × columnas típica de pandas).
En vez de objetos, cada columna es un array nativo (columna de ints, de strings, de bools...).
**Las operaciones se hacen sobre columnas enteras a la vez.**

Para no leer el CSV dos veces, construimos el DataFrame **a partir de los objetos ya mapeados**:

```csharp
public static DataFrame ConstruirDataFrame(List<Accidentes> datos) => new(
    new PrimitiveDataFrameColumn<int>("anio", datos.Select(a => a.Fecha.Year)),
    new StringDataFrameColumn("distrito", datos.Select(a => a.Distrito)),
    new PrimitiveDataFrameColumn<bool>("alcohol", datos.Select(a => a.PositivaAlcohol)),
    new PrimitiveDataFrameColumn<double>("uno", datos.Select(_ => 1.0)),  // truco para contar
    // ... más columnas
);
```

### 4.3 El "truco" de la columna `uno`

`GroupBy(x).Count()` **no funciona como uno espera** en `Microsoft.Data.Analysis`: no crea una
columna `count`, sino que **replica el mismo valor en todas las columnas del resultado**. La forma
correcta de contar es:

```csharp
// Agrupar por una clave y SUMA la columna "uno" (que vale 1 siempre) -> obtienes el conteo
private static DataFrame Agrupar(DataFrame df, string columna) => df.GroupBy(columna).Sum("uno");
```

Por eso el DataFrame tiene una columna `"uno"` rellena de `1.0`.

### 4.4 Filtros booleanos

En LINQ usarías `Where(a => a.PositivaAlcohol)`. En DataFrame se usa una **máscara booleana**:

```csharp
var peatonesMask = (PrimitiveDataFrameColumn<bool>)(
    df["tipo_persona"].ElementwiseEquals("Peatón") |
    df["tipo_persona"].ElementwiseEquals("Peatón (atropello sc)"));
var peatones = df.Filter(peatonesMask);
```

Detalle técnico: el operador `|` entre dos columnas devuelve un `DataFrameColumn` genérico,
así que hay que **castearlo** a `PrimitiveDataFrameColumn<bool>` para poder pasarlo a `Filter`.

---

## 5. Las 30 consultas

Cada consulta se implementa **dos veces**: una con LINQ/PLINQ y otra con DataFrame.
El programa las ejecuta y mide el tiempo de cada una.

| # | Descripción | Cómo se hace |
|---|-------------|--------------|
| 1 | Total de accidentes | `lista.Count` / `df.Rows.Count` |
| 2 | Accidentes por distrito (top 5) | `GroupBy(Distrito)` + `Count()` |
| 3 | Accidentes por tipo | `GroupBy(TipoAccidente)` |
| 4 | Accidentes por estado meteorológico | `GroupBy(EstadoMeteorologico)` |
| 5 | Accidentes por sexo | `GroupBy(Sexo)` |
| 6 | Accidentes por rango de edad | `GroupBy(RangoEdad)` |
| 7 | Positivos en alcohol | `GroupBy(PositivaAlcohol)` |
| 8 | Positivos en drogas | `GroupBy(PositivaDroga)` |
| 9 | Accidentes por día de la semana | `GroupBy(Fecha.DayOfWeek)` |
| 10 | Accidentes por mes | `GroupBy(Fecha.Month)` |
| 11 | Hora con más accidentes | `GroupBy(Hora.Hour)` + `OrderByDescending` + `First` |
| 12 | Lesiones más frecuentes | `GroupBy(Lesividad)` |
| 13 | Tipo de vehículo más implicado | `GroupBy(TipoVehiculo)` |
| 14 | Accidentes con peatones | `Where(TipoPersona.StartsWith("Peatón"))` |
| 15 | Proporción hombre/mujer | contar `Sexo.Hombre` / `Sexo.Mujer` |
| 16 | Distritos con más peatones | `Where(EsPeaton)` + `GroupBy(Distrito)` |
| 17 | Fin de semana vs entre semana | `Where(EsFinDeSemana)` |
| 18 | Media de accidentes por día | `Count()` / nº de fechas distintas |
| 19 | Accidentes con alcohol + droga | `Where(alcohol && droga)` |
| 20 | Rangos de edad más vulnerables (peatones) | `Where(EsPeaton)` + `GroupBy(RangoEdad)` |
| 21 | Distritos con más positivos en alcohol | `Where(alcohol)` + `GroupBy(Distrito)` |
| 22 | Accidentes por código de distrito | `GroupBy(CodigoDistrito)` |
| 23 | Accidentes por año | `GroupBy(Fecha.Year)` |
| 24 | Evolución mensual por año | `GroupBy(año + mes)` (clave compuesta) |
| 25 | Distrito con más accidentes por año | `GroupBy(año)` + top distrito (**PLINQ**) |
| 26 | Tendencia de alcohol por año | `Where(alcohol)` + `GroupBy(año)` |
| 27 | Fin de semana vs entre semana por año | `GroupBy(año)` + contar FDS |
| 28 | Hora pico por año | `GroupBy(año)` + top hora (**PLINQ**) |
| 29 | Lesión más frecuente por año | `GroupBy(año)` + top lesividad (**PLINQ**) |
| 30 | Evolución de peatones por año | `Where(EsPeaton)` + `GroupBy(año)` |

**Nota sobre PLINQ:** las consultas 25, 28 y 29 usan `AsParallel()` en el lado LINQ: parten el
trabajo en varios hilos. Es especialmente útil cuando hay un `GroupBy` dentro de otro `GroupBy`
(mucho trabajo que repartir).

### La clave compuesta (consultas 24-30)

LINQ permite agrupar por varias claves con una tupla:
```csharp
.GroupBy(a => (Anio: a.Fecha.Year, Mes: a.Fecha.Month))
```

Pero **DataFrame.GroupBy solo acepta UNA columna**. La solución del proyecto:
**crear la clave combinada ya concatenada** al construir el DataFrame:

```csharp
new StringDataFrameColumn("anio_mes",     datos.Select(a => $"{a.Fecha.Year}-{a.Fecha.Month:00}")),
new StringDataFrameColumn("anio_distrito", datos.Select(a => $"{a.Fecha.Year}|{a.Distrito}")),
```

Así: `GroupBy("anio_mes")` agrupa por año-mes en una sola pasada.

---

## 6. Cómo se mide el tiempo (Program.cs)

1. **Calentamiento**: se ejecutan ambas técnicas una vez antes de medir (la primera ejecución
   paga la compilación JIT del .NET y no es representativa).
2. **Rondas**: cada técnica se ejecuta **5 veces** y se muestra la **media** por consulta. Así se
   elimina el "ruido" de la medición.
3. **Separación de fases**: se mide toda la tanda LINQ, luego toda la tanda DataFrame, forzando
   `GC.Collect()` entre medias. Sin esto, el recolector de basura de una fase contaminaría la
   medición de la otra.
4. **Resultado**: se agrupa por número de consulta y se imprime la tabla comparativa con el
   **tiempo medio** y el **ganador** de cada consulta (en verde en consola).

Cada resultado se guarda en un **DTO** que "empaqueta" número, descripción, valor y tiempo:

```csharp
public record ResultadoConsulta(int Numero, string Descripcion, string Valor, TimeSpan Tiempo);
```

---

## 7. Resultados obtenidos (ejecución real de referencia)

```
Entorno   : .NET 10.0.12 | 16 núcleos lógicos | X64
Lectura de ficheros  : 130.864 accidentes en 855 ms
Construcción DataFrame: 417 ms
```

| # | Consulta | LINQ (ms) | DF (ms) | Ganador |
|---|----------|----------:|--------:|:-------:|
| 1 | Total de accidentes | 0,00 | 0,00 | LINQ |
| 2 | Accidentes por distrito (top 5) | 20,26 | 11,50 | **DF** |
| 3 | Tipo de accidente | 21,25 | 11,77 | **DF** |
| 4 | Estado meteorológico | 20,10 | 8,49 | **DF** |
| 5 | Sexo | 8,96 | 2,28 | **DF** |
| 6 | Rango de edad | 22,25 | 12,90 | **DF** |
| 7 | Positivos en alcohol | 7,51 | 1,47 | **DF** |
| 8 | Positivos en drogas | 7,37 | 1,03 | **DF** |
| 9 | Día de la semana | 10,22 | 2,09 | **DF** |
| 10 | Mes | 15,42 | 1,30 | **DF** |
| 11 | Hora con más accidentes | 15,24 | 1,31 | **DF** |
| 12 | Lesiones más frecuentes | 20,25 | 8,64 | **DF** |
| 13 | Tipo de vehículo más implicado | 20,83 | 9,26 | **DF** |
| 14 | Accidentes con peatones | 9,85 | 20,21 | LINQ |
| 15 | Proporción hombre/mujer | 11,53 | 129,35 | LINQ |
| 16 | Distritos con más peatones | 13,24 | 20,17 | LINQ |
| 17 | Fin de semana vs entre semana | 5,87 | 31,63 | LINQ |
| 18 | Media de accidentes por día | 13,03 | 2,78 | **DF** |
| 19 | Accidentes con alcohol + droga | 6,05 | 13,24 | LINQ |
| 20 | Rangos de edad vulnerables (peatones) | 12,54 | 20,80 | LINQ |
| 21 | Distritos con más positivos en alcohol | 6,60 | 18,88 | LINQ |
| 22 | Accidentes por código de distrito | 14,77 | 6,50 | **DF** |
| 23 | Accidentes por año | 14,70 | 1,04 | **DF** |
| 24 | Evolución mensual por año | 15,14 | 2,06 | **DF** |
| 25 | Distrito con más accidentes por año | 15,95 | 136,88 | LINQ\* |
| 26 | Tendencia de alcohol por año | 5,50 | 18,53 | LINQ |
| 27 | Fin de semana vs entre semana por año | 34,34 | 139,15 | LINQ |
| 28 | Hora pico por año | 16,69 | 125,19 | LINQ\* |
| 29 | Lesión más frecuente por año | 14,77 | 117,47 | LINQ\* |
| 30 | Evolución de peatones por año | 11,02 | 19,50 | LINQ |
| | **TOTAL** | **411,24** | **895,43** | LINQ 14 · DF 16 |

\* Queries 25, 28 y 29: lado LINQ con `AsParallel()` (PLINQ).

---

## 8. Lectura de resultados y análisis (¡lo importante!)

### 8.1 Qué significan los números

- Los resultados de ambas técnicas **son idénticos** (mismos datos, mismas operaciones),
  solo cambia el **tiempo** de cada consulta.
- La **construcción del DataFrame** (417 ms) no se cuenta como "consulta": es un coste de
  preparación. Si solo fueras a hacer UNA consulta, no merecería la pena; como hacemos 30, se amortiza.

### 8.2 Patrón 1: consultas de agrupación simple → gana DataFrame

Consultas **2-13, 18, 22, 23 y 24** (agrupar por una columna y contar): **DataFrame es más rápido**
(por ejemplo, la 23 «Accidentes por año» pasa de 14,70 ms a 1,04 ms, **~14× más rápido**).

**¿Por qué?** En LINQ, agrupar implica crear multitud de objetos `IGrouping` intermedios y
desboxes de los valores. DataFrame, en cambio, hace la agrupación sobre **arrays nativos**
(columnas contiguas en memoria), mucho más amigables con el hardware (caché de la CPU).

### 8.3 Patrón 2: consultas con filtro + agrupación → gana LINQ

Consultas que primero **filtran** y luego agrupan (14-17, 19-21, 30) o que mezclan varias
condiciones (15): **LINQ es más rápido** (en la 15 «Proporción hombre/mujer», la diferencia es de
**11×** a favor de LINQ).

**¿Por qué?** `df.Filter(...)` **crea un DataFrame NUEVO** con una copia de las filas que cumplen
la condición. Son operaciones más caras en DataFrame (asignación de memoria + copia) que recorrer
objetos con un `Where`.

### 8.4 Patrón 3: consultas "por cada año" → gana LINQ/PLINQ claramente

Consultas **25-29**: para cada año, filtrar y volver a agrupar. En DataFrame esto implica hacer
`Filter` + `GroupBy` **por cada año** (3 veces), lo que multiplica el coste:
por ejemplo la 27 pasa de **34 ms (LINQ) a 139 ms (DataFrame)**.

**¿Por qué?** La repetición del patrón "filtrar → agrupar → quedarse con el máximo" golpea el
punto débil de DataFrame (los `Filter` que copian) varias veces. Y se agrava porque los
`filtrar + ordenar + Head(1)` en DataFrame hacen mucha más maquinaria interna que el
`GroupBy + First()` de LINQ.

> **Dato del mundo real (¡que no está buscado!):** la consulta 29 da como "lesión más frecuente"
> el valor **vacío** (`""`), porque el campo `lesividad` está sin rellenar en muchos registros
> (58.142 filas). Es un ejemplo perfecto de **calidad de los datos**: antes de analizar hay que
> limpiarlos.

### 8.5 Resumen para el README / conclusión

| Escenario | Técnica recomendada | Motivo |
|-----------|:-------------------:|--------|
| Agrupar y contar por una columna | **DataFrame** | Arrays nativos, hasta ~14× más rápido |
| Filtrar + agrupar | **LINQ** | El `Filter` de DataFrame copia las filas |
| Varias consultas por año | **LINQ / PLINQ** | Evita repetir filtros costosos |
| Muy poco volumen de datos | LINQ | No compensa construir el DataFrame |
| Muchas consultas diferentes | **DataFrame** | El coste de construir se amortiza |

**Conclusión global del proyecto:** con 130.864 registros y 30 consultas, el tiempo total es de
**LINQ 411 ms vs DataFrame 895 ms**. DataFrame **no gana por diseño**, gana solo en su punto
fuerte (agrupaciones masivas por columnas); LINQ/PLINQ es más versátil y gana cuando hay filtros
o trabajo repetido por año. La lectura del CSV es lo que más tarda de todo (855 ms), muy por
encima de cualquier consulta.

---

## 9. Cómo ejecutar el proyecto

Requisitos: **.NET 10 SDK**.

```bash
cd AccidentesMadrid/AccidentesMadrid/AccidentesMadrid
dotnet run
```

---

## 10. Docker

El proyecto es una **aplicación de consola**: se ejecuta, imprime el análisis y termina.
No escucha en ningún puerto, por eso el `docker-compose.yml` **no tiene `ports`** ni variables
de entorno web.

### `Dockerfile` (multi-etapa)

- **Etapa 1 (build)**: imagen SDK, restaura paquetes y publica en Release.
- **Etapa 2 (runtime)**: imagen `runtime` (no `aspnet`, no es web), copia el binario publicado
  **y la carpeta `data/`** con los CSV y ejecuta el programa.

Los ficheros Docker están **dentro de la carpeta del proyecto** (donde está el `.csproj`),
así que los comandos se ejecutan desde ahí:

```bash
cd AccidentesMadrid/AccidentesMadrid/AccidentesMadrid   # carpeta con el Dockerfile
docker build -t accidentesmadrid .
docker run --rm accidentesmadrid
```

### `docker-compose.yml`

```yaml
services:
  accidentesmadrid:
    build:
      context: .
      dockerfile: Dockerfile
```

```bash
cd AccidentesMadrid/AccidentesMadrid/AccidentesMadrid
docker compose up --build
```

### `.dockerignore`

Excluye `bin/`, `obj/`, `.git/`, `.vs/` para que el contexto de construcción sea limpio
y el `COPY . .` no arrastre código compilado ni basura.

---

## 11. Conclusión personal de aprendizaje

En esta práctica he aprendido que **no existe una técnica universalmente mejor**:

- **LINQ/PLINQ** es muy expresivo y cómodo, pero al agrupar crea muchos objetos intermedios.
- **DataFrame** está optimizado para operaciones por columnas (como pandas o R), pero paga caro
  cada filtro/copia y necesita columnas "preparadas" (claves compuestas concatenadas, columna `uno`,
  máscaras booleanas casteadas) que complican el código.

Saber **medir** (media de varias ejecuciones, calentamiento JIT, aislar el GC) es tan importante
como saber programar la consulta.