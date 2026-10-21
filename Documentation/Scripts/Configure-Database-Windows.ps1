$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $repoRoot

$envFile = Join-Path $HOME '.config/kukulcan/database.env'


function Show-Progress([int]$Percent, [string]$Message) {
    Write-Host ('[{0,3}%] {1}' -f $Percent, $Message)
}

function Read-MaskedPassword {
    $password = [System.Text.StringBuilder]::new()
    Write-Host -NoNewline 'Password: '

    while ($true) {
        $key = [Console]::ReadKey($true)

        if ($key.Key -eq [ConsoleKey]::Enter) {
            break
        }

        if ($key.Key -eq [ConsoleKey]::Backspace) {
            if ($password.Length -gt 0) {
                [void]$password.Remove($password.Length - 1, 1)
                Write-Host -NoNewline ([char]8 + ' ' + [char]8)
            }
            continue
        }

        if (-not [char]::IsControl($key.KeyChar)) {
            [void]$password.Append($key.KeyChar)
            Write-Host -NoNewline '*'
        }
    }

    Write-Host
    return $password.ToString()
}

function Escape-ConnectionValue([string]$Value) {
    if ($null -eq $Value) {
        return ''
    }

    return $Value.Replace('\', '\\').Replace('"', '""')
}

while ($true) {
    Write-Host 'Seleccione el gestor de base de datos:'
    Write-Host '1. SQL Server'
    Write-Host '2. PostgreSQL'
    Write-Host '3. MySQL'
    Write-Host '0. Salir sin registrar las variables'
    $option = Read-Host 'Opción'

    switch ($option) {
        '1' { $provider = 'SqlServer'; $port = 1433; $migrationProject = 'Source/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.csproj'; break }
        '2' { $provider = 'PostgresSql'; $port = 5432; $migrationProject = 'Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj'; break }
        '3' { $provider = 'MySql'; $port = 3306; $migrationProject = 'Source/KUKULCAN.SharedKernel.Auth.Migrations.MySQL/KUKULCAN.SharedKernel.Auth.Migrations.MySQL.csproj'; break }
        '0' {
            Write-Host 'Salir sin registrar las variables'
            exit 0
        }
        default {
            Write-Host 'Opción no válida. Inténtelo de nuevo.'
            continue
        }
    }

    break
}

$database = Read-Host 'Database name'
$user = Read-Host 'User'
$password = Read-MaskedPassword
Show-Progress 10 'Database connection details collected'

$databaseEscaped = Escape-ConnectionValue $database
$userEscaped = Escape-ConnectionValue $user
$passwordEscaped = Escape-ConnectionValue $password

switch ($provider) {
    'SqlServer' {
        $connectionString = "Server=localhost,$port;Database=$databaseEscaped;User Id=$userEscaped;Password=$passwordEscaped;TrustServerCertificate=True"
    }
    'PostgresSql' {
        $connectionString = "Host=localhost;Port=$port;Database=$databaseEscaped;Username=$userEscaped;Password=$passwordEscaped"
    }
    'MySql' {
        $connectionString = "Server=localhost;Port=$port;Database=$databaseEscaped;User=$userEscaped;Password=$passwordEscaped"
    }
}

$directory = Split-Path -Parent $envFile
Show-Progress 20 'Saving database connection settings'
New-Item -ItemType Directory -Path $directory -Force | Out-Null

@"
KUKULCAN__DATABASE__PROVIDER=$provider
KUKULCAN__DATABASE__CONNECTIONSTRING=$connectionString
"@ | Set-Content -Path $envFile -Encoding utf8NoBOM

[Environment]::SetEnvironmentVariable('KUKULCAN__DATABASE__PROVIDER', $provider, 'User')
[Environment]::SetEnvironmentVariable('KUKULCAN__DATABASE__CONNECTIONSTRING', $connectionString, 'User')

$env:KUKULCAN__DATABASE__PROVIDER = $provider
$env:KUKULCAN__DATABASE__CONNECTIONSTRING = $connectionString

Write-Host 'Las variables de entorno de base de datos han sido registradas.'
Write-Host "Provider: $provider"
Write-Host 'Host: localhost'
Write-Host "Port: $port"
Write-Host 'La configuración persistirá para futuras sesiones de PowerShell.'

Show-Progress 30 '.NET SDK and migration project validation'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK not found.' }
if (-not (Test-Path $migrationProject)) { throw "Migration project not found: $migrationProject" }
Show-Progress 40 'Discovering pending EF Core migrations'
$migrationLines = @(& dotnet ef migrations list --project $migrationProject --startup-project $migrationProject --context AuthDbContext 2>&1)
if ($LASTEXITCODE -ne 0) {
    $migrationLines | ForEach-Object { Write-Host $_ }
    throw 'Could not list EF Core migrations.'
}
$migrationLines | ForEach-Object { Write-Host $_ }
$pendingCount = @($migrationLines | Where-Object { "$_" -match '\\(Pending\\)' }).Count

Show-Progress 50 'Applying EF Core migrations to the selected database'
$appliedCount = 0
& dotnet ef database update --project $migrationProject --startup-project $migrationProject --context AuthDbContext 2>&1 | ForEach-Object {
    $line = "$_"
    Write-Host $line
    if ($line -match 'Applying migration') {
        $appliedCount++
        if ($pendingCount -gt 0) {
            $percent = [Math]::Min(90, 50 + [Math]::Floor($appliedCount * 40 / $pendingCount))
            Show-Progress $percent "Applied migration $appliedCount of $pendingCount"
        }
    }
}
if ($LASTEXITCODE -ne 0) { throw 'EF Core database update failed.' }
Show-Progress 100 'Database setup completed'
