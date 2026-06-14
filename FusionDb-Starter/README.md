# FusionDb Starter

Initial zero-cost local foundation for FusionDb.

## Requirements

- .NET 10 SDK
- Docker Desktop or Podman with Docker Compose compatibility

## 1. Start PostgreSQL and pgvector

```powershell
cd deploy
docker compose up -d
docker compose ps
```

## 2. Run the API

Open another terminal from the repository root:

```powershell
dotnet restore src/FusionDb.Api/FusionDb.Api.csproj
dotnet run --project src/FusionDb.Api/FusionDb.Api.csproj
```

## 3. Verify

Open:

- http://localhost:5080/
- http://localhost:5080/health/database
- http://localhost:5080/health/vector

Expected database result:

```json
{
  "status": "healthy",
  "database": "fusiondb",
  "postgresVersion": "...",
  "vectorEnabled": true
}
```

## Reset local database

This deletes the local Docker database volume:

```powershell
cd deploy
docker compose down -v
docker compose up -d
```
