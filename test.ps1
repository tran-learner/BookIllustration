$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot

if (-not (Test-Path -LiteralPath (Join-Path $projectRoot ".env"))) {
    throw "Missing .env. Copy .env.example to .env and set its values first."
}

dotnet test (Join-Path $projectRoot "backend/BookIllustration_Backend.Tests/BookIllustration_Backend.Tests.csproj")
exit $LASTEXITCODE
