# Route Weather Tracker

Shows current conditions, webcams, and weather forecasts for mountain passes along the route from **Stanwood, WA** to **Kalispell, MT**.

**Passes covered:**

- Snoqualmie Pass (I-90, WA)
- Fourth of July Pass (I-90, ID)
- Lookout Pass (I-90, MT/ID)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and [Yarn](https://yarnpkg.com/)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (for local Key Vault access)

## Required Secrets

The following secrets must exist in the `route-weather-tracker-kv` Azure Key Vault at `https://route-weather-tracker-kv.vault.azure.net/`:

| Secret Name         | How to obtain                                           |
| ------------------- | ------------------------------------------------------- |
| `WsdotApiKey`       | Request a free key at https://wsdot.wa.gov/traffic/api/ |
| `OpenWeatherApiKey` | Sign up free at https://openweathermap.org/             |

## Local Development Setup

1. Sign in to Azure so `DefaultAzureCredential` can resolve Key Vault secrets:

   ```bash
   az login
   ```

   Your account needs the **Key Vault Secrets User** role on `route-weather-tracker-kv`.

2. Restore dependencies:

   ```bash
   dotnet restore
   cd route-weather-tracker-app && yarn install && cd ..
   ```

3. Run everything (API + frontend + Aspire dashboard):
   ```bash
   dotnet run --project route-weather-tracker-service.AppHost
   ```

The Aspire dashboard opens at `http://localhost:18888`. The frontend is served by Vite on the port shown in the dashboard.

## Project Structure

```
route-weather-tracker/
  route-weather-tracker-app/                     # Vite/React/TypeScript frontend
  route-weather-tracker-service/                 # .NET 10 Web API
  route-weather-tracker-service.AppHost/         # Aspire orchestrator
  route-weather-tracker-service.ServiceDefaults/ # Shared observability
  route-weather-tracker-service.Tests/           # xUnit backend tests
  route-weather-tracker.sln
```

## Running Tests

```bash
dotnet test
```
