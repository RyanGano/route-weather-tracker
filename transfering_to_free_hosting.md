# Migration to Free Hosting: Container Apps → Static Web App + App Service

## Overview

**Goal**: Reduce Azure costs from ~$20+/month toward $0/month  
**Current**: Frontend + Backend running as Azure Container Apps (billed per vCPU/memory)  
**Target**:

- **Frontend** → Azure Static Web App (Free tier, $0/month) — serves pre-built React/Vite files
- **Backend** → Azure App Service (F1 Free tier, $0/month) — serves the .NET 10 API

**Resource Group**: `rg-route-weather-tracker-service.AppHost-free`  
**Key Vault**: `weather-service-kv` — **no changes needed here**  
**Domain**: `www.whentodrive.com` — DNS at Porkbun will be updated during Phase 5

> **Cold starts**: App Service F1 has no "Always On", so after 20 min idle the app sleeps.
> The first request will take 10–30s to warm up. Subsequent requests are fast.
> The 5-minute caching in the weather API means warm-up hits are rare. Acceptable trade-off for $0/month.

---

## Steps

### Phase 1: Code Changes

- [x] Agent Done: Create `route-weather-tracker-app/public/staticwebapp.config.json` — SPA routing config so React Router deep-links work when served by Static Web App
- [x] Agent Done: Add `UseForwardedHeaders()` to `route-weather-tracker-service/Program.cs` — prevents HTTPS redirect loop on App Service (App Service proxies to your app over HTTP internally; without this, `UseHttpsRedirection()` loops)
- [x] Agent Done: Replace `.github/workflows/azure-dev.yml` — new workflow builds .NET 10 backend and deploys to App Service, builds React frontend with `VITE_API_URL` injected, and deploys to Static Web App
- [x] Agent Done: Commit and push all changes to `main`

> ⚠️ **Expected**: The new workflow will immediately trigger and **fail** — infrastructure and
> secrets don't exist yet. This is harmless. You will manually re-trigger after Phase 3.

---

### Phase 2: Azure Infrastructure

- [x] User Done: Create **App Service Plan**
  - Type: Linux, Free (F1)
  - Resource Group: `rg-route-weather-tracker-service.AppHost-free`
  - Region: **West US** (match existing Key Vault)
  - → Fill in `App_Service_Plan_Name` in **Things to Supply**

- [x] User Done: Create **App Service (Web App)**
  - Runtime stack: **.NET 10** (Linux)
  - App Service Plan: the one you just created
  - Resource Group: `rg-route-weather-tracker-service.AppHost-free`
  - → Fill in `App_Service_Name` and `App_Service_URL` in **Things to Supply**

- [x] User Done: Create **Static Web App**
  - Plan type: **Free**
  - Source / Deployment source: **Other** (NOT GitHub — we use our own workflow)
  - Resource Group: `rg-route-weather-tracker-service.AppHost-free`
  - → Fill in `Static_Web_App_Name` and `Static_Web_App_Default_URL` in **Things to Supply**

- [x] User Done: Enable **System-Assigned Managed Identity** on the App Service
  - App Service → Identity → System assigned → Status: **On** → Save
  - → Fill in `Managed_Identity_Object_ID` in **Things to Supply**

- [x] User Done: Grant Managed Identity **"Key Vault Secrets User"** on `weather-service-kv`
  - Key Vault `weather-service-kv` → Access control (IAM) → + Add role assignment
  - Role: **Key Vault Secrets User** | Assign to: the App Service's managed identity

- [x] User Done: Configure **App Service Application Settings**
  - Add setting: `KeyVaultUri` = `https://weather-service-kv.vault.azure.net/`
  - Add setting: `ASPNETCORE_ENVIRONMENT` = `Production`
  - General settings → **HTTPS Only**: **On**
  - Save (app will restart)

---

### Phase 3: GitHub Configuration

- [x] User Done: Add GitHub **secret** `AZURE_STATIC_WEB_APPS_API_TOKEN`
  - Static Web App → Overview → **Manage deployment token** → copy
  - GitHub → Settings → Secrets and variables → Actions → **New repository secret**
  - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`

- [x] User Done: Add GitHub **repo variable** `AZURE_APP_SERVICE_NAME`
  - Value: your `App_Service_Name` from below (e.g., `route-weather-api`)

- [x] User Done: Add GitHub **repo variable** `VITE_API_URL`
  - Value: your `App_Service_URL` from below (e.g., `https://route-weather-api.azurewebsites.net`)

---

### Phase 4: Deploy and Verify

- [x] User Done: Manually trigger the GitHub Actions workflow
  - GitHub → Actions → **"Deploy Route Weather Tracker"** → **Run workflow** → Run workflow
  - Both jobs (`deploy-backend` and `deploy-frontend`) must turn green ✅

- [x] User Done: Test the backend directly
  - Open `App_Service_URL/api/endpoints` in browser
  - Should return a JSON array of cities/route endpoints

- [ ] User: Test the frontend at the SWA default URL
  - Open `Static_Web_App_Default_URL` in browser
  - The route-picker app should load; try a route lookup end-to-end

---

### Phase 5: Custom Domain DNS Cutover

- [ ] User: Add custom domain `www.whentodrive.com` to the Static Web App
  - Static Web App → **Custom domains** → **+ Add**
  - Enter `www.whentodrive.com` → Next
  - Validation type: **CNAME**
  - SWA shows you a CNAME target — fill in `SWA_CNAME_Target` below

- [ ] User: Update the `www` CNAME record at Porkbun
  - Porkbun → DNS for `whentodrive.com`
  - Edit existing `www` CNAME → change value to `SWA_CNAME_Target`
  - Set TTL to `300` (5 min for fast propagation) → Save
  - Go back to Azure SWA → Custom domains → click **Validate** on `www.whentodrive.com`

- [ ] User: Wait for SWA to provision the SSL certificate
  - Custom domains list shows: Pending → Provisioning certificate → **Ready**
  - Takes 10–30 min after DNS propagates

- [ ] User: Verify `https://www.whentodrive.com` loads correctly
  - Test a real route lookup end-to-end to confirm API connectivity

---

### Phase 6: Cleanup — Stop the Billing

> Only do this **after** `https://www.whentodrive.com` is confirmed working on the new stack.

- [ ] User: Delete **`frontend`** Container App
- [ ] User: Delete **`api`** Container App
- [ ] User: Delete **`cae-xkbsw6reaj2vs`** Container Apps Environment
- [ ] User: Delete **`acrxkbsw6reaj2vs`** Container Registry
- [ ] User: Delete **`law-xkbsw6reaj2vs`** Log Analytics workspace
- [ ] User: Delete **`mi-xkbsw6reaj2vs`** Managed Identity (the old user-assigned one; the App Service has its own system-assigned identity now)
- [ ] User: Remove stale GitHub repo variables `AZURE_ENV_NAME` and `AZURE_LOCATION` (no longer used)

---

## How To

### How To: Create App Service Plan (F1 Free, Linux)

1. Azure Portal → **Create a resource** → search **"App Service Plan"** → Create
2. **Subscription**: your subscription
3. **Resource Group**: `rg-route-weather-tracker-service.AppHost-free`
4. **Name**: e.g., `weather-api-plan-free`
5. **Operating System**: Linux
6. **Region**: West US
7. **Pricing tier**: Click **"Explore pricing plans"** → scroll to **Dev / Test** tab → select **F1 (Free)** → Select
8. **Review + Create** → **Create**

---

### How To: Create App Service (Web App, .NET 10, Linux)

1. Azure Portal → **Create a resource** → **Web App**
2. **Resource Group**: `rg-route-weather-tracker-service.AppHost-free`
3. **Name**: choose a globally unique name (e.g., `route-weather-api`) → becomes `<name>.azurewebsites.net`
4. **Publish**: Code
5. **Runtime stack**: **.NET 10** (if not yet listed, use .NET 9 as a fallback and open an issue)
6. **Operating System**: Linux
7. **Region**: West US
8. **App Service Plan**: select the plan from the previous step
9. Skip Deployment, Networking, Monitoring tabs
10. **Review + Create** → **Create**

Once created, the **Overview** tab shows the default URL → copy as `App_Service_URL`.

---

### How To: Create Static Web App (Free Tier)

1. Azure Portal → **Create a resource** → search **"Static Web App"** → Create
2. **Resource Group**: `rg-route-weather-tracker-service.AppHost-free`
3. **Name**: e.g., `route-weather-frontend`
4. **Plan type**: **Free**
5. **Deployment source**: **Other**
6. **Region**: West US 2 (closest to West US)
7. **Review + Create** → **Create**
8. Go to the resource → **Overview** → copy the **URL** (e.g., `https://proud-mushroom-01234.2.azurestaticapps.net`) → save as `Static_Web_App_Default_URL`
9. Click **Manage deployment token** → copy → save for the next step

---

### How To: Enable System-Assigned Managed Identity on App Service

1. Open your App Service resource
2. Left sidebar → **Identity** (under Settings)
3. **System assigned** tab → Status toggle: **On** → **Save** → confirm **Yes**
4. After saving, copy the **Object (principal) ID** → save as `Managed_Identity_Object_ID`

---

### How To: Grant Managed Identity "Key Vault Secrets User" on the Key Vault

1. Azure Portal → open `weather-service-kv`
2. Left sidebar → **Access control (IAM)**
3. **+ Add** → **Add role assignment**
4. **Role** tab → search `Key Vault Secrets User` → select → **Next**
5. **Members** tab → **Assign access to**: Managed identity → **+ Select members**
   - Managed identity type: **App Service**
   - Select your App Service from the list → **Select**
6. **Review + assign** → **Review + assign**

---

### How To: Configure App Service Application Settings

1. Open your App Service resource
2. Left sidebar → **Environment variables** (newer portal) or **Configuration → Application settings**
3. **+ Add application setting** for each:
   - `KeyVaultUri` → `https://weather-service-kv.vault.azure.net/`
   - `ASPNETCORE_ENVIRONMENT` → `Production`
4. Click **Apply** / **Save**
5. Switch to **General settings** tab → **HTTPS Only** → **On** → **Save**
6. The app restarts automatically

---

### How To: Add GitHub Secret and Variables

**Secret** (AZURE_STATIC_WEB_APPS_API_TOKEN):

1. GitHub repo → **Settings** → **Secrets and variables** → **Actions**
2. **Secrets** tab → **New repository secret**
3. Name: `AZURE_STATIC_WEB_APPS_API_TOKEN` | Value: deployment token from SWA → **Add secret**

**Variables** (AZURE_APP_SERVICE_NAME, VITE_API_URL):

1. Same page → **Variables** tab → **New repository variable**
2. Repeat for each:
   - `AZURE_APP_SERVICE_NAME` = your App Service name (not the full URL, just the name)
   - `VITE_API_URL` = full App Service URL with `https://` (e.g., `https://route-weather-api.azurewebsites.net`)

---

### How To: Add Custom Domain to Static Web App + Validate

1. Static Web App → left sidebar → **Custom domains** → **+ Add**
2. Enter: `www.whentodrive.com` → **Next**
3. Validation type: **CNAME record** (for `www` subdomain)
4. Azure shows: "Add a CNAME record with value `<swa-hostname>.azurestaticapps.net`"
5. Copy that hostname → fill in `SWA_CNAME_Target` below
6. **Do not click Validate yet** — first go update DNS at Porkbun (next section)

---

### How To: Update DNS at Porkbun (www CNAME)

1. Log in to [porkbun.com](https://porkbun.com) → **Domain Management** → **whentodrive.com** → **DNS**
2. Find the existing `CNAME` record for `www` (currently pointing to old Container App URL)
3. Click the **edit** (pencil) icon
4. Change the **Answer** field to `SWA_CNAME_Target` (just the hostname, no `https://`)
5. Set **TTL**: `300` (5 minutes)
6. **Save**
7. Return to Azure SWA → Custom domains → click **Validate** next to `www.whentodrive.com`
8. Azure will retry DNS until it resolves; SSL provisions automatically once validated

---

### How To: Delete Old Container App Resources (Phase 6)

1. Azure Portal → **Resource Groups** → `rg-route-weather-tracker-service.AppHost-free`
2. Check the checkbox next to each resource listed in Phase 6 above
3. **Delete** (top toolbar) → type the resource group name to confirm
   - **Or** delete them individually to be safer

**Order to delete** (dependencies matter):

1. `frontend` Container App
2. `api` Container App
3. `cae-xkbsw6reaj2vs` Container Apps Environment (only after both apps are gone)
4. `acrxkbsw6reaj2vs` Container Registry
5. `law-xkbsw6reaj2vs` Log Analytics workspace
6. `mi-xkbsw6reaj2vs` Managed Identity

---

## Things to Supply

Resource_Group_Name: rg-route-weather-tracker-service.AppHost-free
Key_Vault_Name: weather-service-kv
Key_Vault_URI: https://weather-service-kv.vault.azure.net/

App_Service_Plan_Name: whentodrive-app
App_Service_Name: whentodrive-api
App_Service_URL: https://whentodrive-api-anepgwheb4d0cman.westus3-01.azurewebsites.net

Static_Web_App_Name: whentodrive-webapp
Static_Web_App_Default_URL: https://green-sand-04ed4421e.6.azurestaticapps.net

Managed_Identity_Object_ID: 5e06153c-12cb-4aee-a340-ecdd688658cf

SWA_CNAME_Target: [add here — shown during custom domain setup in portal]
