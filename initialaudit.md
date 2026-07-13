# Initial Code Audit

Targeted review of the Vaultborn / AssetManager solution, carried out step by step.
Date started: 2026-07-13.

- **Step 1 — Security & repo hygiene: COMPLETE (findings below)**
- Step 2 — Infrastructure layer (services, data access): in progress
- Step 3 — Core layer (models, redundancy): pending
- Step 4 — Desktop code-behind (MainWindow etc.): pending

---

## Step 1: Security & repo hygiene

### 🔴 Critical — secrets committed to the repository

Secret **values are not repeated in this document**; see the referenced lines.
Because these are in git history, removing the lines is not enough — **the
credentials must be rotated** at their respective dashboards (MongoDB Atlas,
Autodesk APS, PayPal), especially if the repo is or was ever public.

1. **MongoDB Atlas connection string with username + password in plaintext**
   - `AssetManager.Infrastructure/Data/MongoConnection.cs:13` (older variant commented out on line 12)
   - Grants full read/write to the production database to anyone with repo access.

2. **Autodesk (Forge/APS) client secret hardcoded, duplicated in two files**
   - `AssetManager.Infrastructure/Services/TokenService.cs:18`
   - `AssetManager.Infrastructure/Services/AutodeskAPIService.cs:21`

3. **PayPal client secret hardcoded**
   - `AssetManager.Infrastructure/Services/PayPalService.cs:9`

4. **Postgres password in `AssetManager.Infrastructure/appsettings.json`**
   - Appears to be dead localhost/dev config (the app uses MongoDB), so low
     risk — but the file should be cleaned up or deleted.

**Remediation plan**
- [ ] Rotate MongoDB Atlas password
- [ ] Rotate Autodesk app client secret
- [ ] Rotate PayPal client secret
- [ ] Load secrets from environment variables / AWS Secrets Manager — note the
      project already contains a working helper (`AssetManager.Infrastructure/Services/AWSSecrets.cs`)
      that none of these services use
- [ ] Remove dead `appsettings.json` Postgres config

### 🟡 Repo hygiene

5. **Large binary files tracked in git** (most of the clone size):
   - `AssetManager.Infrastructure/Assets/hdris/scythian_tombs_puresky_4k.exr` — 73 MB
   - `Uploads/p3166.glb` — 30 MB test model
   - ~20 MB of 2 MB+ PNG background/button assets in `AssetManager.Desktop/Assets/`
   - Candidates for deletion or Git LFS.

6. **Build output was committed historically** (`bin/Debug`, `bin/Release`,
   DLLs, and copies of a `.env`). The historical `.env` content is harmless
   (a local `PYTHONPATH` only — no secrets), but history is bloated.

7. **.gitignore has redundant entries** (`*.dll` three times, `*.suo`/`*.user`
   twice) — cosmetic only.

8. **A full vendored copy of Python's `requests` library** (plus `urllib3`,
   `certifi`, `idna` — ~140 files) is committed under
   `AssetManager.Desktop/Resources/requests/`, including a nested duplicate
   `requests/requests/`. This should be a pip-installed dependency of the
   Fusion add-in, not committed source.

---

## Step 2: Infrastructure layer

_(pending)_

## Step 3: Core layer

_(pending)_

## Step 4: Desktop code-behind

_(pending)_
