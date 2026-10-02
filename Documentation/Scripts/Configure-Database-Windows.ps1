$ErrorActionPreference = 'Stop'

$envFile = Join-Path $HOME '.config/kukulcan/database.env'

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
        '1' { $provider = 'SqlServer'; $port = 1433; break }
        '2' { $provider = 'PostgresSql'; $port = 5432; break }
        '3' { $provider = 'MySql'; $port = 3306; break }
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
New-Item -ItemType Directory -Path $directory -Force | Out-Null

@"
KUKULCAN_DATABASE_PROVIDER=$provider
KUKULCAN_DATABASE_CONNECTION_STRING=$connectionString
"@ | Set-Content -Path $envFile -Encoding utf8NoBOM

[Environment]::SetEnvironmentVariable('KUKULCAN_DATABASE_PROVIDER', $provider, 'User')
[Environment]::SetEnvironmentVariable('KUKULCAN_DATABASE_CONNECTION_STRING', $connectionString, 'User')

$env:KUKULCAN_DATABASE_PROVIDER = $provider
$env:KUKULCAN_DATABASE_CONNECTION_STRING = $connectionString

Write-Host 'Las variables de entorno de base de datos han sido registradas.'
Write-Host "Provider: $provider"
Write-Host 'Host: localhost'
Write-Host "Port: $port"
Write-Host 'La configuración persistirá para futuras sesiones de PowerShell.'
