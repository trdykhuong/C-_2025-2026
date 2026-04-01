# Azure Deployment Guide

## 1. Prerequisites
- Azure account (https://portal.azure.com)
- Azure CLI installed: `winget install Microsoft.AzureCLI`
- GitHub repository with the code

---

## 2. Create Azure Resources

```bash
# Login
az login

# Create Resource Group
az group create --name interacthub-rg --location southeastasia

# Create SQL Server
az sql server create \
  --name interacthub-sql \
  --resource-group interacthub-rg \
  --location southeastasia \
  --admin-user sqladmin \
  --admin-password "YourStrongPassword123!"

# Create SQL Database
az sql db create \
  --resource-group interacthub-rg \
  --server interacthub-sql \
  --name InteractHubDb \
  --service-objective S0

# Allow Azure services to access SQL
az sql server firewall-rule create \
  --resource-group interacthub-rg \
  --server interacthub-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create App Service Plan
az appservice plan create \
  --name interacthub-plan \
  --resource-group interacthub-rg \
  --sku B1 \
  --is-linux

# Create Backend Web App
az webapp create \
  --name interacthub-api \
  --resource-group interacthub-rg \
  --plan interacthub-plan \
  --runtime "DOTNETCORE:8.0"

# Create Storage Account for images
az storage account create \
  --name interacthubstorage \
  --resource-group interacthub-rg \
  --location southeastasia \
  --sku Standard_LRS

# Create Blob Container
az storage container create \
  --name interacthub \
  --account-name interacthubstorage \
  --public-access blob

# Create Static Web App for frontend
az staticwebapp create \
  --name interacthub-frontend \
  --resource-group interacthub-rg \
  --location eastasia \
  --source https://github.com/YOUR_USERNAME/YOUR_REPO \
  --branch main \
  --app-location /frontend \
  --output-location dist
```

---

## 3. Configure App Settings (Backend)

```bash
# Get SQL connection string
SQL_CONN="Server=tcp:interacthub-sql.database.windows.net,1433;Database=InteractHubDb;User ID=sqladmin;Password=YourStrongPassword123!;Encrypt=True;"

# Get Blob connection string
BLOB_CONN=$(az storage account show-connection-string \
  --name interacthubstorage \
  --resource-group interacthub-rg \
  --query connectionString -o tsv)

# Set environment variables on App Service
az webapp config appsettings set \
  --name interacthub-api \
  --resource-group interacthub-rg \
  --settings \
    "ConnectionStrings__DefaultConnection=$SQL_CONN" \
    "Jwt__Key=YourProductionJwtSecretKeyMin32Characters!!" \
    "Jwt__Issuer=InteractHub" \
    "Jwt__Audience=InteractHubClient" \
    "Jwt__ExpiresHours=24" \
    "Frontend__Url=https://interacthub-frontend.azurestaticapps.net" \
    "Azure__BlobStorage__ConnectionString=$BLOB_CONN" \
    "Azure__BlobStorage__ContainerName=interacthub"
```

---

## 4. GitHub Actions Secrets

Add these secrets in GitHub → Settings → Secrets and Variables → Actions:

| Secret | How to get |
|---|---|
| `AZURE_BACKEND_APP_NAME` | `interacthub-api` |
| `AZURE_BACKEND_PUBLISH_PROFILE` | Azure Portal → App Service → Get publish profile → copy XML content |
| `AZURE_STATIC_WEB_APPS_TOKEN` | Azure Portal → Static Web App → Manage deployment token |
| `AZURE_BACKEND_URL` | `https://interacthub-api.azurewebsites.net` |

---

## 5. Deploy

Push to `main` branch — GitHub Actions will automatically:
1. Build & test backend
2. Build frontend  
3. Deploy backend to Azure App Service
4. Deploy frontend to Azure Static Web Apps

---

## 6. Verify Deployment

- **API Swagger:** `https://interacthub-api.azurewebsites.net/swagger`
- **Frontend:** `https://interacthub-frontend.azurestaticapps.net`

---

## 7. Application Insights (Optional Monitoring)

```bash
az monitor app-insights component create \
  --app interacthub-insights \
  --location southeastasia \
  --resource-group interacthub-rg \
  --application-type web

# Link to App Service
az webapp config appsettings set \
  --name interacthub-api \
  --resource-group interacthub-rg \
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$(az monitor app-insights component show \
    --app interacthub-insights \
    --resource-group interacthub-rg \
    --query instrumentationKey -o tsv)"
```
