# Initial Code Audit

Targeted review of the Vaultborn / AssetManager solution, carried out step by step.
Date started: 2026-07-13.

- **Step 1 — Security & repo hygiene: COMPLETE (findings below)**
- **Step 2 — Infrastructure layer (services, data access): COMPLETE (findings below)**
- **Step 3 — Core layer (models, redundancy): COMPLETE (findings below)**
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

~5,100 lines across 35 files. Reviewed all services with focus on the largest
(`DataManagement.cs`, 1,654 lines). Recurring themes: copy-paste duplication,
resource misuse, swallow-and-return-null error handling, and UI concerns
leaking into the data layer.

### 🔴 Additional security issues found in this layer

1. **Client secret printed to console** —
   `Services/AutodeskAPIService.cs:36-37` logs both the client ID and client
   secret in plaintext on every construction.
2. **Full access token logged** — `Services/TokenService.cs:174`: the comment
   says "first 10 chars" but the interpolation prints the **entire** bearer
   token. Same file logs the full auth response body (line 152) and the full
   refresh token (line 244).
3. **Refresh token persisted to user environment variables** —
   `TokenService.cs:245` writes the OAuth refresh token into the Windows user
   environment (i.e. the registry), where it survives indefinitely and is
   readable by any process running as the user.

### 🟠 Correctness / reliability

4. **Shared static `HttpClient` mutated per call** — `DataManagement.cs` calls
   `client.DefaultRequestHeaders.Clear()` then sets the auth header on a
   static client inside instance methods. Two concurrent calls can swap each
   other's headers mid-flight. Headers belong on `HttpRequestMessage`.
5. **66 occurrences of `new HttpClient()` / `new MongoConnection()`**
   scattered across 15 files. Each `new MongoConnection()` builds a new
   `MongoClient` (13 times in `UserService.cs` alone — one per method call);
   MongoClient is documented to be a singleton. Per-call `HttpClient` risks
   socket exhaustion. Both should be single shared instances (DI or static).
6. **Static credential fields mutated by an instance constructor** —
   `TokenService(string clientId, string clientSecret)` assigns to `static`
   fields, so constructing one instance silently changes credentials for every
   other user of the class. `AutodeskApiService` has both hardcoded
   `ClientId/ClientSecret` *and* injected `_ClientId/_ClientSecret`; methods
   like `GetItemUrn` use the hardcoded ones, so injection does nothing.
7. **`TokenManager` is a global mutable static token bag** with no expiry
   tracking — nothing knows when a token is stale until an API call 401s
   (and then the error is swallowed, see below).
8. **Catch-all → `Console.WriteLine` → `return null` in nearly every method.**
   Errors never reach the UI or a logger (a WPF app has no console), and every
   caller must remember to null-check. One shared retry/error strategy or
   thrown exceptions would remove hundreds of lines.
9. **PayPal order approval URL picked by array index** —
   `PayPalService.cs:75` uses `links[1]` instead of finding the link with
   `rel == "approve"`; the API does not guarantee link order. `CreateOrder`
   also never checks the HTTP status. Sandbox endpoints are hardcoded.
10. **`UserService.GetUserPic` mutates during a read**: fetching a profile
    picture can randomly reassign the user's picture and write it to the DB;
    the approved-pictures list is hardcoded S3 URLs, and the fallback is the
    placeholder `https://your-bucket.s3.amazonaws.com/fallback.png`.

### 🟡 Design / redundancy

11. **`DataManagement.cs` is a 1,654-line God class** mixing Autodesk REST
    calls and MongoDB access, with heavy copy-paste:
    - `GetPersonalHub()` vs `GetPersonalHubDetails()` — identical logic, one
      returns an ID, the other a tuple.
    - Two `GetLatestItemThumbnail` overloads plus `GetVersionThumbnail` plus
      `FetchThumbnailUrl` — four variants of the same fetch.
    - **Six** overlapping version-metadata methods (`GetVersionsForItemAsync`,
      `GetItemVersions`, `GetItemVersionMetadata`,
      `GetItemVersionsWithExtraMetadata`, `GetModelVersionMetadata`,
      `GetVersionMetadata`) returning ever-larger tuples of the same data.
    - The ~15-line auth-header + GET + parse-JSON preamble is duplicated in
      every method instead of one `GetJsonAsync(url)` helper.
12. **N+1 query patterns** — `UserService.GetAllListedModels` runs three
    sequential DB/API lookups per listed model (purchased check, project ID,
    seller name), each opening a fresh Mongo connection. Same in
    `GetAllListedDecks`.
13. **WPF leaking into the service layer** — `GetAllListedModels` returns
    `Dictionary<string, string>` containing `"BuyVisibility": "Collapsed"`.
    The data layer is emitting WPF `Visibility` values instead of a bool on a
    typed model.
14. **Dead code and clutter**: large commented-out method blocks
    (`TokenService.cs:265-294`), duplicate `using System.Text;`, dead Postgres
    `appsettings.json`, an empty `NewFolder\` item in the csproj, and a stray
    `AssetManager.Desktop_azcjhdjh_wpftmp.csproj` build artifact committed to
    the repo.

**Highest-value refactors (in order):**
1. One shared `MongoClient`/`MongoConnection` and one shared `HttpClient`
   (or `IHttpClientFactory`) via dependency injection.
2. Collapse `DataManagement.cs` around a single `GetJsonAsync` helper and one
   version-metadata method; delete the five redundant variants.
3. Replace return-null error handling with exceptions or a `Result` type, and
   route logging through a logger instead of `Console.WriteLine`.
4. Strip all token/secret logging (ties into Step 1 rotation work).

## Step 3: Core layer

### 🟠 `AssetManager.Core` is an empty project

1. **The Core project contains zero source files** — only a `.csproj`. The
   classes it once held (`FusionManager`, `HelloWorld`, `Class1`) were deleted
   over time. Both Desktop and Infrastructure still reference it, so today it
   contributes nothing except its package references:
   - **`python` 3.13.2** — this NuGet package embeds an entire CPython
     distribution into the build output of every project that references Core.
   - **`pythonnet` 3.0.5** — conflicts with the `3.1.0-preview` version
     referenced by Desktop and Infrastructure (two different pythonnet
     versions in one solution).
   - **`Microsoft.AspNetCore.App` 2.2.8** — an out-of-support ASP.NET Core
     2.2 package pinned inside a net8.0 library; this reference style has been
     invalid since .NET Core 3.0 and drags in dozens of obsolete assemblies.
   - WebView2, IdentityModel.OidcClient, RestSharp — all duplicated in the
     other two projects anyway.

   **Fix:** either delete the project (and the two dangling references) or —
   better — make it a real Core: move the domain models out of
   Infrastructure into it, with no NuGet dependencies at all.

### 🟡 Domain-model issues (models live in Infrastructure/Models)

2. **The layering is inverted.** For a Core/Infrastructure split, domain
   models (`Model`, `ModelData`, `User`, `Comment`, …) should live in Core;
   instead they sit in Infrastructure next to the Mongo code, and Core is
   empty.
3. **`Model` vs `ModelData` are two overlapping representations** of the same
   concept in different collections (`Models` and `ModelData`), with
   inconsistent conventions: `Model` uses ObjectId string IDs and `DateTime`;
   `ModelData` uses underscore-prefixed Bson field names and stores
   **dates as strings** (`CreatedDate`, `ModifiedDate`), making date queries
   and sorting unreliable.
4. `ModelData.FileSize` is an `int` (caps at ~2 GB and mismatches the `long`
   used for file sizes elsewhere); `Thumbnail_Base64` stores image bytes
   inline in the document, pushing toward Mongo's 16 MB document limit and
   bloating every query that fetches model lists.
5. `Model.CreatedAt` defaults to `DateTime.Now` (local time) rather than
   `DateTime.UtcNow`.

### 🟡 Stray top-level directories

6. **`Executable/` contains only committed build output** — an old
   `Executable.exe`/`.dll`/`.pdb` with no corresponding source project in the
   solution. Dead weight; delete from the repo.
7. **`Uploads/` is test debris** — `test.txt`, `uploadTest.txt.txt`, and the
   30 MB `p3166.glb` noted in Step 1.

## Step 4: Desktop code-behind

_(pending)_
