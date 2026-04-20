FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EsperancaSolidaria.Worker/EsperancaSolidaria.Worker.csproj", "EsperancaSolidaria.Worker/"]
COPY ["src/EsperancaSolidaria.Worker.Infrastructure/EsperancaSolidaria.Worker.Infrastructure.csproj", "EsperancaSolidaria.Worker.Infrastructure/"]

RUN dotnet restore "EsperancaSolidaria.Worker/EsperancaSolidaria.Worker.csproj"

COPY src/ .

RUN dotnet publish "EsperancaSolidaria.Worker/EsperancaSolidaria.Worker.csproj" \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EsperancaSolidaria.Worker.dll"]