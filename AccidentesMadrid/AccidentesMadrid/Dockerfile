# ============================================================
# Dockerfile multi-etapa: Práctica Accidentes Madrid
# ============================================================

# ETAPA 1: Build + Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar SOLO el proyecto (NUNCA bin/ ni obj/ -> .dockerignore)
COPY AccidentesMadrid.csproj ./
RUN dotnet restore

# Copiar código fuente y datos
COPY . .

# Publicar
RUN dotnet publish AccidentesMadrid.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ETAPA 2: Runtime (imagen final ligera)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copiar solo el binario publicado
COPY --from=build /app/publish .

# Datos necesarios para las consultas
COPY data/ ./data/

# Punto de entrada (aplicación de consola: ejecuta y termina)
ENTRYPOINT ["dotnet", "AccidentesMadrid.dll"]