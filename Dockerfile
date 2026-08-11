# ---------- Etapa 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo el csproj primero: si no cambian las dependencias,
# Docker reutiliza la capa de restore y el build es mucho mas rapido.
COPY RanchoMqttApi.csproj ./
RUN dotnet restore RanchoMqttApi.csproj

# Ahora si, el resto del codigo
COPY . .
RUN dotnet publish RanchoMqttApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Etapa 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=America/Matamoros

# Usuario sin privilegios (buena practica: si alguien escapa de la app, no es root)
RUN adduser --disabled-password --gecos "" --uid 1001 appuser \
 && mkdir -p /app/Logs \
 && chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish ./

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "RanchoMqttApi.dll"]
