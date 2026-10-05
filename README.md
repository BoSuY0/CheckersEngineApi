# CheckersEngineApi

A REST Web API that takes an English/American 8x8 checkers position in PDN and returns the best move. The engine
is KingsRow English 1.20 (x64) with the Chinook 2-8 piece win/loss/draw endgame databases, the "practical Windows
path" named in the specification. The API is a normal ASP.NET Core application hosted in IIS. A small web page
in `wwwroot` exists only to check that the service and the engine work together.

## Architecture

```
src/Checkers.Domain               checkers model: squares, pieces, Position, PDN FEN, move notation, legal moves
src/Checkers.Application          -> Domain     use cases (suggest, validate, health), strength levels, LRU cache, ports
src/Checkers.Engine.Protocol      (no deps)     JSON-lines messages between the API and a worker host
src/Checkers.Engine               -> Application, Domain, Protocol   worker pool and processes, implements the ports
src/Checkers.Engine.KingsRowHost  -> Protocol   win-x64 exe that loads Kingsrow64.dll and egdb64.dll
src/Checkers.Api                  -> Application, Engine             controllers, composition root, IIS, test page
tests/Checkers.{Domain,Application,Engine,Api}.Tests                 xUnit v3
```

Dependencies point inward: Api -> Engine -> Application -> Domain. The Application owns the ports
(`IEngineWorkerPool`, `IEngineSession` = `IEngineAdapter` + `ITablebase`, and `EngineFailureException`), and the
Engine project implements them. The controllers use only the Application; `Program.cs` is the only API code that
references the Engine project. Checkers rules exist only in the Domain.

KingsRow and egdb are native x64 Windows DLLs that keep their state in process globals, so each worker is a
separate long-lived process (`Checkers.Engine.KingsRowHost.exe`). The API starts `Engine:Workers` of them at
application start, warms them up, routes requests round-robin with an async lock per worker, and restarts a
worker only when its host fails or does not stop after a timeout. A host start is not bound to the request that
waits for it: a request that times out while a worker starts leaves the start running for the next request. The host speaks one JSON object per line on
stdin/stdout (`setPosition`, `search`, `probe`, `stop`).

### Flow of `POST /v1/move/suggest`

1. Parse and normalize the PDN position (422 on failure, or when the side to move has no legal move). Return a
   cached answer if one exists.
2. Acquire one worker for the whole request.
3. With 8 pieces or fewer: play out pending captures, probe all quiet positions in the database in one batch,
   and keep the moves with the best win/draw/loss value. A 10 ms KingsRow search picks among them; if that search
   fails, the first of them is returned with depth and nodes 0. If the database cannot decide, continue with step 4.
4. Otherwise: KingsRow searches with the level's limits. The first legal move among the best move and its PV is
   played; if none is legal, the response is 500.
5. The result is cached by canonical PDN and effective search limits (LRU, 15 minute TTL).

`softTimeMs` is enforced inside the host (KingsRow exact time). `hardTimeMs` is a `CancellationToken` in the
controller; on expiry the response is 504 and the worker's search is stopped.

## Endpoints

### `POST /v1/move/suggest`

```http
POST /v1/move/suggest
Content-Type: application/json

{
  "gameId": "checkers-8x8",
  "state": { "notation": "PDN", "position": "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16" },
  "level": "strong",
  "limits": { "maxDepth": 18, "softTimeMs": 500, "hardTimeMs": 1200 }
}
```

```json
{
  "engine": "chinook",
  "bestMove": "14x23",
  "pv": ["14x23", "27x18", "16x23", "25-21", "12-16", "28-24", "16-19", "24x15", "10x19", "22-17", "19-24"],
  "scoreOrWDL": 142,
  "depth": 19,
  "nodes": 559222,
  "positionKey": "pdn:B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16",
  "info": { "tablebaseHit": false, "timeMs": 56 }
}
```

- `level` is `weak`, `medium` or `strong`, or absent. `limits` and each of its fields are optional.
- `scoreOrWDL` is from the side to move's point of view: the KingsRow evaluation (about 100 per man, |v| >= 2000
  is a known win or loss), or 1 / 0 / -1 (win / draw / loss) when the answer came from the database.
- Errors (RFC 7807 ProblemDetails): 400 for a malformed request (wrong `gameId`, `notation` other than `PDN`,
  unknown `level`, non-positive limit), 422 for invalid PDN or no legal move, 504 when `hardTimeMs` elapses, 500
  when the engine fails.

| level | move time | depth cap |
|---|---|---|
| weak | 100 ms | 8 |
| medium | 250 ms | 12 |
| strong | 500 ms | 18 |
| none | `Limits:DefaultSoftTimeMs` | none |

Request limits are upper bounds on the level's budget. Without `hardTimeMs`, `Limits:DefaultHardTimeMs` applies.

### `POST /v1/move/validate`

```json
{ "position": "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16", "move": "14x23" }
```
returns `{ "legal": true }`. Captures may be written short (`6x24`) or with the full path (`6x15x24`). A malformed
position or move gives 422.

### `GET /healthz`

Returns `{ "ok": true, "workers": 2 }` when every configured worker is ready, otherwise `ok: false` and the
number of ready workers. The status code is always 200.

### `GET /`

The test board: a PDN input and a level, a Suggest button that draws the returned position and highlights the
best move, and a Validate button for a move.

### Request log

Every `POST /v1/move/suggest` request that reaches the controller writes one JSON line through the built-in JSON
console formatter, whatever its outcome, for example:

```json
{"EventId":1,"LogLevel":"Information","Category":"Checkers.Api.Controllers.MoveController","Message":"Suggest request 0HNP3100SLQ81:00000001 took 33 ms (depth 14, nodes 39561, tablebaseHit True)","State":{"requestId":"0HNP3100SLQ81:00000001","timeMs":33,"depth":14,"nodes":39561,"tablebaseHit":true,"{OriginalFormat}":"Suggest request {requestId} took {timeMs} ms (depth {depth}, nodes {nodes}, tablebaseHit {tablebaseHit})"}}
```

`depth`, `nodes` and `tablebaseHit` are `null` when the request ended without a suggestion (422, 500 or 504).
Under IIS the lines go to the stdout log file (see the deployment section).

## Configuration

`src/Checkers.Api/appsettings.json`:

```json
{
  "Engine": {
    "Type": "chinook",
    "Path": "C:\\kingsrow",
    "Workers": 2,
    "Databases": "D:\\tb\\chinook",
    "HostPath": "KingsRowHost/Checkers.Engine.KingsRowHost.exe",
    "Launcher": ""
  },
  "Cache": { "Capacity": 20000, "TtlMinutes": 15 },
  "Limits": { "DefaultSoftTimeMs": 300, "DefaultHardTimeMs": 1200 }
}
```

| Key | Meaning |
|---|---|
| `Engine:Type` | Engine name echoed in every response |
| `Engine:Path` | KingsRow install directory (Windows path) holding `egdb64.dll` and `engines\Kingsrow64.dll` |
| `Engine:Workers` | Number of worker processes (1-64) |
| `Engine:Databases` | Chinook database directory (Windows path, at most 200 characters: egdb64.dll overruns a buffer on long paths) |
| `Engine:HostPath` | Worker host executable; relative paths are resolved against the application directory. The build and publish put it in `KingsRowHost/` |
| `Engine:Launcher` | Program that runs the host, `wine` on Linux; empty on Windows |
| `Cache:Capacity`, `Cache:TtlMinutes` | LRU size and absolute time to live |
| `Limits:DefaultSoftTimeMs` | Search time when the request has no level |
| `Limits:DefaultHardTimeMs` | Deadline when the request has no `hardTimeMs` |

Every option is validated at startup. Any key can be overridden with an environment variable such as
`Engine__Databases`.

## Obtaining KingsRow and the Chinook databases

- KingsRow English 1.20 x64: `KingsrowSetup64.1.20.exe` from https://edgilbert.org/EnglishCheckers/KingsRowEnglish.htm.
  Install it to a short path such as `C:\kingsrow`; `Engine:Path` points to that directory.
- Chinook databases: https://webdocs.cs.ualberta.ca/~chinook/DataBases/ - `DB6.zip` (2-6 pieces), `DB7.0.zip` to
  `DB7.4.zip` (7 pieces, 4 v 3) and `DB8.00.zip` to `DB8.44.zip` (8 pieces, 4 v 4, about 5.6 GB unpacked). Unpack
  all zips into one directory and point `Engine:Databases` to it. A smaller set works too (DB6 alone, or DB6 + DB7)
  as long as each piece count present is complete; the startup log reports the piece count ("with 7-piece
  databases"). Without any database the service still runs, but every request uses an engine search.

## Build and test

Requires the .NET 10 SDK.

```sh
dotnet build CheckersEngineApi.slnx
dotnet test --solution CheckersEngineApi.slnx
```

`global.json` selects the Microsoft.Testing.Platform runner that xUnit v3 needs with `dotnet test` on .NET 10.

`Checkers.Engine.Tests` also compiles the host's board, status-line and bitboard conversions (the files without
native calls) so that they are tested on any OS.

Two end-to-end tests in `Checkers.Engine.Tests` run the real host and are skipped unless these are set:
`CHECKERS_KINGSROW_HOST` (host exe path), `CHECKERS_KINGSROW_PATH` and `CHECKERS_KINGSROW_DATABASES` (Windows
paths). Off Windows the host runs through `wine` in the prefix given by `WINEPREFIX`.

## Running locally on Linux with Wine

1. Create a 64-bit Wine prefix and install KingsRow into it, e.g. to `C:\kingsrow`:
   `WINEPREFIX=$PWD/.engine-cache/wineprefix wine KingsrowSetup64.1.20.exe`.
2. Unpack the Chinook databases, e.g. into `.engine-cache/chinook-db`.
3. Point the service at the prefix and the databases (Wine maps `Z:` to `/`), then run it:

   ```sh
   export WINEPREFIX=$PWD/.engine-cache/wineprefix
   export Engine__Databases="Z:$(pwd | tr / '\\')\\.engine-cache\\chinook-db"
   dotnet run --project src/Checkers.Api
   ```

   The launch profile sets `ASPNETCORE_ENVIRONMENT=Development` and `WINEDEBUG=-all`, and serves
   http://localhost:5080. `appsettings.Development.json` sets `Engine:Launcher` to `wine`.

The build publishes the win-x64 host and copies it to `bin/<config>/net10.0/KingsRowHost/`.

## Deploying to IIS on Windows Server

1. Install the ASP.NET Core 10 Hosting Bundle (it contains the ASP.NET Core Module V2 and the runtime).
2. Install KingsRow (e.g. `C:\kingsrow`) and unpack the databases (e.g. `D:\tb\chinook`).
3. Publish: `dotnet publish src/Checkers.Api -c Release -o <site folder>`. The output contains `web.config`
   (in-process hosting) and `KingsRowHost\Checkers.Engine.KingsRowHost.exe` (self-contained, no extra runtime).
4. Set `Engine:Path` and `Engine:Databases` in `appsettings.json` (or as environment variables in `web.config`).
5. Application pool:
   - .NET CLR version: No Managed Code; pipeline mode Integrated.
   - Start Mode: `AlwaysRunning`, and on the site "Preload Enabled" = true, so the workers start and warm up when
     the pool starts instead of on the first request (requires the IIS Application Initialization feature).
   - Idle Time-out: 0, so the pool and its warm workers are not shut down.
   - Disable Overlapped Recycle: true, so two generations of workers never run at once (each worker holds its
     hash table and database buffers).
   - Load User Profile: true. KingsRow keeps its settings in `HKCU` and writes a log under the user's Documents.
6. Folder permissions for the pool identity (`IIS AppPool\<pool name>`): Read & Execute on the site folder, the
   KingsRow directory and the database directory, and Modify on the site's `logs` folder.
7. Request log: under in-process hosting the JSON console lines go to the process's stdout, which the ASP.NET Core
   Module writes to `logs\stdout_<timestamp>_<process id>.log` (`stdoutLogEnabled="true"` in `web.config`). The
   module starts a new file for every process start and neither rotates nor deletes them, so remove old files as
   part of normal maintenance.
8. Check `GET /healthz`: `{ "ok": true, "workers": 2 }`.

## Assumptions

1. **Chinook = KingsRow + Chinook databases.** There is no standalone Chinook search engine for Windows; the
   specification names KingsRow with the Chinook 2-8 piece databases as the practical path. `engine` echoes
   `Engine:Type` ("chinook").
2. **`Engine:Path` is the KingsRow install directory**, not a `chinook.exe`. KingsRow is a DLL, so it runs inside
   our own worker host (`Engine:HostPath`). `Engine:Launcher` exists only to run that host under Wine.
3. **The sample response is not a valid answer.** `22-18x11-7` is not PDN, it is a White move in a Black-to-move
   position, and the pv does not follow from it. Only its shape is kept; `bestMove` and `pv` are real legal moves.
   Captures in the response use their full path (`6x15x24`).
4. **PV** is the legal prefix of the engine's PV replayed from the position. If it does not start with the chosen
   move, PV = `[bestMove]`.
5. **"Try next PV move"**: candidates are the engine's best move followed by its PV moves; the first one legal in
   the requested position is played. If none is, the response is 500.
6. **scoreOrWDL** is from the side to move's point of view (see above).
7. **"Perfect move" with a WLD-only database.** The probe determines the moves that keep the best WLD value; a
   10 ms KingsRow search with the same databases picks among them, and its move is checked against that set. A WLD
   table has no distance information, so this keeps winning play progressing. The probe has already answered the
   request, so when that search fails the first proven move is returned (PV = the move, depth and nodes 0).
8. **The tablebase step applies to every level.** Flow step 2 is level independent; "strong: probe tablebases
   first" restates it.
9. **8 pieces or fewer but outside database coverage.** Chinook's 7- and 8-piece sets cover only 4 v 3 and 4 v 4,
   and fewer pieces may be installed. Then, or when capture resolution exceeds its bound, the request falls back to
   a normal engine search with the engine's `tablebaseHit`.
10. **Pending captures** are played out (at most 8 plies) before probing, because the database has no valid value
    while either side has a capture.
11. **maxDepth with a time-based engine.** KingsRow has no depth limit; the host stops the search once the reported
    nominal depth reaches the cap. This is best effort: depth advances by 2 and is reported only from time to time.
    Time is the enforced budget. "depth X to Y or movetime Z" becomes move time Z plus depth cap Y.
12. **Level vs limits.** The level defines the budget; request limits are upper bounds. Without a level the budget
    is `Limits:DefaultSoftTimeMs` with no depth cap. When `softTimeMs` >= `hardTimeMs` the result is a 504.
13. **Strong uses 500 ms** (the low end of 500-600 ms) so the total stays under the 600 ms acceptance limit.
14. **"No randomness"**: the opening book (random by default) is off and each worker searches with one thread, for
    all levels. A time-based search can still vary between runs; the cache keeps answers stable for its TTL.
15. **Cache key** = canonical PDN + effective search limits (move time, depth cap); `hardTimeMs` only decides
    whether an answer arrives. Only successful suggestions are cached. The TTL is absolute.
16. **PDN input is a PDN FEN position** (`B:W18,19,K22:B1-3,5`: either list first, ranges and `K` kings allowed,
    upper-case letters). Normalization = canonical ordering. Validation: squares 1-32, no duplicates, at most 12
    pieces per side, no man on its own crowning row.
17. **Validate** accepts short and full capture notation; `legal` is true when a legal move matches.
18. **A position with no legal move** cannot get a suggestion: 422.
19. **400 vs 422**: request-shape errors use ASP.NET's standard 400 validation response (including a numeric
    `level`); 422 is reserved for PDN content, as the specification states.
20. **Timeouts**: `hardTimeMs` covers waiting for a worker and the search. On expiry the response is 504, the
    search is stopped, the worker is restarted if it does not become idle within 250 ms, and nothing is cached.
21. **Health**: `ok` = every configured worker is ready; `workers` = the number ready; always 200, as the
    specification defines only the body. A worker that fails to start fails application start.
22. **JSON log per request**: one line per suggest request, the requests that run the engine and carry depth,
    nodes and tablebaseHit; those three are `null` when the request ended without a suggestion. Requests rejected
    by model validation (400), `validate` and `healthz` are not logged. `requestId` is ASP.NET's `TraceIdentifier`.
23. **Nodes** are derived from KingsRow's kN/s and the elapsed time; KingsRow does not report a node count.
24. **Engine `tablebaseHit`** is inferred from KingsRow's root result: its database draw list, or a database
    draw/win/loss score in a position within the loaded piece count. KingsRow has no explicit flag.
25. **IIS**: in-process hosting, settings as in the deployment section. The request log is kept through the ASP.NET
    Core Module's stdout log, the built-in way to keep console output under IIS.
26. **The test page** draws from the canonical `positionKey` the API returns and contains no checkers rules.

## Known limitations

- A king capture that ends on its own start square cannot be read back from KingsRow (it reports such a move as
  "xxxx" and the board does not change), so that rare case returns 500.
- The depth cap is approximate (assumption 11), and on a repeated position the warm hash table lets KingsRow
  report deep results within a few milliseconds.
- `nodes` of the very first search in a worker includes KingsRow's database initialisation time; warm-up absorbs it.
- The test board renders only what the API returns; it does not play games.
- Under Wine, the database path must stay short (MAX_PATH); use a short directory or an 8.3 path.
