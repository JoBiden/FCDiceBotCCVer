# Builds (if needed) and starts the Random Event Builder - a local web UI for authoring
# documents in the bot's ChateauDb.RandomEvents collection.
#
#   .\run.ps1                # build + start on http://localhost:8787 and open the browser
#   .\run.ps1 -Port 9000
#   .\run.ps1 -Db SomeOtherDb -NoBrowser
#   .\run.ps1 -DllSource "C:\path\to\FChatDicebot\bin\Debug"   # where the Mongo DLLs come from
#
# Requires the bot to have been built at least once (Debug) so the MongoDB driver DLLs
# exist; compiles server.cs with the .NET Framework C# compiler that ships with Windows.
param(
    [int]$Port = 8787,
    [string]$Db = "ChateauDb",
    [string]$Conn = "mongodb://localhost:27017",
    [string]$DllSource = "",
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$bin = Join-Path $here "bin"

# Locate the bot's built DLLs: same checkout first, then the main checkout when this
# script is being run from inside a .claude\worktrees\<name> worktree.
if (-not $DllSource) {
    $candidates = @(
        (Join-Path $here "..\..\FChatDicebot\bin\Debug"),
        (Join-Path $here "..\..\..\..\..\FChatDicebot\bin\Debug")
    )
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "MongoDB.Driver.dll")) { $DllSource = $c; break }
    }
    if (-not $DllSource) {
        throw "Could not find MongoDB.Driver.dll - build the FChatDicebot solution (Debug) first, or pass -DllSource."
    }
}

New-Item -ItemType Directory -Force $bin | Out-Null

# Copy runtime dependencies (cheap; skips unchanged files).
robocopy $DllSource $bin *.dll /XO /NJH /NJS /NDL /NC /NS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed copying DLLs from $DllSource" }

# The driver needs the bot's assembly binding redirects. Use the BUILD OUTPUT config
# (App.config + the redirects msbuild auto-generates), not the bare source App.config.
$appConfig = Join-Path $DllSource "FChatDicebot.exe.config"
if (-not (Test-Path $appConfig)) { $appConfig = Join-Path $DllSource "..\..\App.config" }
Copy-Item $appConfig (Join-Path $bin "RandomEventBuilder.exe.config") -Force

# Compile if the source is newer than the exe.
$exe = Join-Path $bin "RandomEventBuilder.exe"
$src = Join-Path $here "server.cs"
if (-not (Test-Path $exe) -or (Get-Item $src).LastWriteTime -gt (Get-Item $exe).LastWriteTime) {
    $csc = "$env:windir\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path $csc)) { $csc = "$env:windir\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
    Write-Host "Compiling server.cs ..."
    $refs = @("MongoDB.Driver.dll", "MongoDB.Driver.Core.dll", "MongoDB.Bson.dll") |
        ForEach-Object { "/r:" + (Join-Path $bin $_) }
    & $csc /nologo "/out:$exe" @refs $src
    if ($LASTEXITCODE -ne 0) { throw "csc failed" }
}

if (-not $NoBrowser) { Start-Process "http://localhost:$Port/" }
& $exe --port $Port --db $Db --conn $Conn --ui (Join-Path $here "ui.html")
