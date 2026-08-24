$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot

if (-not (Test-Path -LiteralPath (Join-Path $projectRoot ".env"))) {
    throw "Missing .env. Copy .env.example to .env and set its values first."
}

$frontendDirectory = Join-Path $projectRoot "frontend"
$backendDirectory = Join-Path $projectRoot "backend"

if (-not (Test-Path -LiteralPath (Join-Path $frontendDirectory "node_modules"))) {
    Push-Location $frontendDirectory
    try {
        pnpm install
    }
    finally {
        Pop-Location
    }
}

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$backendDirectory'; dotnet run --launch-profile http"
)

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$frontendDirectory'; pnpm dev"
)
