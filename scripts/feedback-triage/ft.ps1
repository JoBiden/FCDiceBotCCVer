# Feedback triage ledger CLI. Reads the bot's `Feedback` collection (read-only) and
# maintains a sibling `FeedbackTriage` collection that records what has been assessed,
# what the owner decided, and where the work ended up. Every verb prints JSON.
#
#   .\ft.ps1 info                                  # counts by status + how many are untriaged
#   .\ft.ps1 new                                   # submissions with no triage row yet (the work list)
#   .\ft.ps1 status                                # every triage row
#   .\ft.ps1 status -AwaitingOwner                 # rows waiting on an owner decision
#   .\ft.ps1 status -Open                          # everything not yet closed out
#   .\ft.ps1 status -Status approved,pr_open
#   .\ft.ps1 set -Id 6a69a8a25b3b20a4bbe5cd64 -Fields '{"status":"assessed","verdict":"bug"}' `
#            -Event assessed -Detail "dossier queue/2026-07-29-....md"
#   .\ft.ps1 seed -Rows '[{"feedbackId":"...","fields":{"status":"declined"},"detail":"why"}]'
#
# Statuses: assessed, needs_info (awaiting owner) | approved, in_progress, pr_open,
#           deferred (decided, in flight) | shipped, declined, duplicate, obsolete (closed).
#
# Requires mongosh on PATH or at the default install location. The `Feedback` documents
# themselves are never written to - see bin\ft.js for why.
#
# Connection overrides: -Conn mongodb://... -Database OtherDb -MongoSh C:\path\mongosh.exe
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("new", "status", "set", "seed", "info")]
    [string]$Mode,

    [string]$Id = "",
    [string]$Fields = "",
    [string]$Event = "",
    [string]$Detail = "",
    [string[]]$Status = @(),
    [string]$Rows = "",
    [switch]$AwaitingOwner,
    [switch]$Open,

    [string]$Conn = "mongodb://localhost:27017",
    # Named -Database, not the sibling scripts' -Db: this script takes [Parameter()]
    # attributes, so PowerShell adds the common parameters and -Db collides with the
    # alias of -Debug.
    [string]$Database = "ChateauDb",
    [string]$MongoSh = ""
)

$ErrorActionPreference = "Stop"
$here = $PSScriptRoot
$js = Join-Path $here "bin\ft.js"
if (-not (Test-Path $js)) { throw "Missing $js" }

if (-not $MongoSh) {
    $cmd = Get-Command mongosh -ErrorAction SilentlyContinue
    if ($cmd) {
        $MongoSh = $cmd.Source
    }
    elseif (Test-Path "$env:ProgramFiles\mongosh\mongosh.exe") {
        $MongoSh = "$env:ProgramFiles\mongosh\mongosh.exe"
    }
    else {
        throw "mongosh not found on PATH - install it or pass -MongoSh <path to mongosh.exe>."
    }
}

# Build the mode's payload. Passed as base64 in an env var so JSON never has to survive
# a trip through PowerShell and shell quoting.
#
# The -Fields / -Rows JSON is validated and then spliced in verbatim rather than
# round-tripped: Windows PowerShell's ConvertTo-Json re-serializes a nested array of
# ConvertFrom-Json objects as a {"value":[...],"Count":n} envelope. Only scalars go
# through ConvertTo-Json, which does escape them correctly.
function Get-JsonScalar([string]$Value) {
    return ($Value | ConvertTo-Json -Compress)
}

$payload = $null
switch ($Mode) {
    "status" {
        $parts = @()
        if ($Status.Count -gt 0) {
            $encoded = ($Status | ForEach-Object { Get-JsonScalar $_ }) -join ","
            $parts += '"status":[' + $encoded + ']'
        }
        if ($AwaitingOwner) { $parts += '"awaitingOwner":true' }
        if ($Open) { $parts += '"open":true' }
        if ($parts.Count -gt 0) { $payload = "{" + ($parts -join ",") + "}" }
    }
    "set" {
        if (-not $Id) { throw "set requires -Id <feedback _id hex>" }
        if (-not $Fields) { throw "set requires -Fields '<json object>'" }
        try { $Fields | ConvertFrom-Json | Out-Null } catch { throw "-Fields is not valid JSON: $($_.Exception.Message)" }
        # Parens around each element are load-bearing: PowerShell's comma binds tighter
        # than +, so `'a:' + $x, 'b:' + $y` builds one space-joined string, not two elements.
        $parts = @( ('"feedbackId":' + (Get-JsonScalar $Id)), ('"fields":' + $Fields) )
        if ($Event) { $parts += '"event":' + (Get-JsonScalar $Event) }
        if ($Detail) { $parts += '"detail":' + (Get-JsonScalar $Detail) }
        $payload = "{" + ($parts -join ",") + "}"
    }
    "seed" {
        if (-not $Rows) { throw "seed requires -Rows '<json array>'" }
        try { $Rows | ConvertFrom-Json | Out-Null } catch { throw "-Rows is not valid JSON: $($_.Exception.Message)" }
        $payload = '{"rows":' + $Rows + '}'
    }
}

$uri = $Conn.TrimEnd("/") + "/" + $Database

$env:FT_MODE = $Mode
if ($payload) {
    $env:FT_PAYLOAD = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload))
}
else {
    $env:FT_PAYLOAD = ""
}

try {
    & $MongoSh --quiet $uri $js
    if ($LASTEXITCODE -ne 0) { throw "mongosh exited with code $LASTEXITCODE (mode: $Mode)" }
}
finally {
    $env:FT_MODE = ""
    $env:FT_PAYLOAD = ""
}
