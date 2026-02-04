# ISIN Kategorisierung (.NET 8 + Vue)

Diese Version des Projekts wurde auf **.NET 8**, **Vue 3** und **Entity Framework Core** migriert. Sie bietet weiterhin den gleichen Workflow:

- Excel-Upload
- ISIN-Validierung
- Finnhub-Kategorisierung
- Excel-Export (1 Sheet oder pro Kategorie)

## Projektstruktur

```
server/   ASP.NET Core API (.NET 8, EF Core, SQLite)
client/   Vue 3 Frontend (Vite)
```

## Backend Setup (server)

### 1) Konfiguration

Lege eine `server/appsettings.Development.json` an oder nutze Umgebungsvariablen:

```json
{
  "Finnhub": {
    "BaseUrl": "https://finnhub.io/api/v1",
    "Secret": "DEIN_FINNHUB_SECRET"
  },
  "Upload": {
    "MaxUploadMb": 10
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=categoriser.db"
  }
}
```

### 2) Starten

```bash
cd server
DOTNET_ENVIRONMENT=Development dotnet run
```

Die API läuft standardmäßig auf `http://localhost:5000`.

## Frontend Setup (client)

```bash
cd client
npm install
npm run dev
```

Vite proxyt `/api` auf `http://localhost:5000`.

## API Endpoints

- `POST /api/upload` (multipart/form-data, `file`)
- `POST /api/check` (JSON, `jobId`)
- `GET /api/download?jobId=...&mode=singleSheet|sheetsByCategory`

## Hinweise

- Für persistente Speicherung wird SQLite verwendet (EF Core). Der Datenbank-File wird automatisch erzeugt.
- Die Finnhub-API benötigt einen Secret-Token in den Einstellungen.
