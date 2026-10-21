#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"

ENV_FILE="${HOME}/.config/kukulcan/database.env"
SHELL_RC="${HOME}/.zshrc"

mask_password() {
  local password=""
  local char=""
  while IFS= read -r -s -n 1 char; do
    [[ "${char}" == $'\n' || "${char}" == $'\r' ]] && break
    if [[ "${char}" == $'\177' || "${char}" == $'\b' ]]; then
      if [[ -n "${password}" ]]; then
        password="${password%?}"
        printf '\b \b'
      fi
    else
      password+="${char}"
      printf '*'
    fi
  done
  printf '\n'
  printf '%s' "${password}"
}


show_progress() {
  printf '[%3d%%] %s\n' "$1" "$2"
}

escape_value() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\"\"}"
  printf '%s' "${value}"
}

while true; do
  printf '%s\n' "Seleccione el gestor de base de datos:"
  printf '%s\n' "1. SQL Server"
  printf '%s\n' "2. PostgreSQL"
  printf '%s\n' "3. MySQL"
  printf '%s\n' "0. Salir sin registrar las variables"
  read -r -p "Opción: " option

  case "${option}" in
    1) provider="SqlServer"; port="1433"; migration_project="Source/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer/KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.csproj" ;;
    2) provider="PostgresSql"; port="5432"; migration_project="Source/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL/KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.csproj" ;;
    3) provider="MySql"; port="3306"; migration_project="Source/KUKULCAN.SharedKernel.Auth.Migrations.MySQL/KUKULCAN.SharedKernel.Auth.Migrations.MySQL.csproj" ;;
    0)
      printf '%s\n' "Salir sin registrar las variables"
      exit 0
      ;;
    *)
      printf '%s\n\n' "Opción no válida. Inténtelo de nuevo."
      continue
      ;;
  esac
  break
done

read -r -p "Database name: " database
read -r -p "User: " user
printf '%s' "Password: "
password="$(mask_password)"

show_progress 10 "Database connection details collected"

database_escaped="$(escape_value "${database}")"
user_escaped="$(escape_value "${user}")"
password_escaped="$(escape_value "${password}")"

case "${provider}" in
  SqlServer)
    connection_string="Server=localhost,${port};Database=${database_escaped};User Id=${user_escaped};Password=${password_escaped};TrustServerCertificate=True"
    ;;
  PostgresSql)
    connection_string="Host=localhost;Port=${port};Database=${database_escaped};Username=${user_escaped};Password=${password_escaped}"
    ;;
  MySql)
    connection_string="Server=localhost;Port=${port};Database=${database_escaped};User=${user_escaped};Password=${password_escaped}"
    ;;
esac

show_progress 20 "Saving database connection settings"
mkdir -p "$(dirname "${ENV_FILE}")"
umask 077
cat > "${ENV_FILE}" <<EOF
export KUKULCAN__DATABASE__PROVIDER="${provider}"
export KUKULCAN__DATABASE__CONNECTIONSTRING="${connection_string}"
EOF
chmod 600 "${ENV_FILE}"

source_line='[ -f "$HOME/.config/kukulcan/database.env" ] && . "$HOME/.config/kukulcan/database.env"'
touch "${SHELL_RC}"
if ! grep -Fqx "${source_line}" "${SHELL_RC}" 2>/dev/null; then
  printf '\n%s\n' "${source_line}" >> "${SHELL_RC}"
fi

export KUKULCAN__DATABASE__PROVIDER="${provider}"
export KUKULCAN__DATABASE__CONNECTIONSTRING="${connection_string}"

printf '%s\n' "Las variables de entorno de base de datos han sido registradas."
printf '%s\n' "Provider: ${provider}"
printf '%s\n' "Host: localhost"
printf '%s\n' "Port: ${port}"
printf '%s\n' "La configuración persistirá para futuras sesiones de terminal."
show_progress 30 "Validating the .NET SDK and migration project"
command -v dotnet >/dev/null 2>&1 || { printf '%s\n' "Error: .NET SDK not found." >&2; exit 1; }
[[ -f "${migration_project}" ]] || { printf 'Error: migration project not found: %s\n' "${migration_project}" >&2; exit 1; }
show_progress 40 "Discovering pending EF Core migrations"
migration_output="$(dotnet ef migrations list --project "${migration_project}" --startup-project "${migration_project}" --context AuthDbContext 2>&1)"
printf '%s\n' "${migration_output}"
pending_count="$(printf '%s\n' "${migration_output}" | grep -Ec '\\(Pending\\)' || true)"
show_progress 50 "Applying EF Core migrations to the selected database"
applied_count=0
dotnet ef database update --project "${migration_project}" --startup-project "${migration_project}" --context AuthDbContext 2>&1 |
  while IFS= read -r line; do
    printf '%s\n' "${line}"
    if [[ "${line}" == *"Applying migration"* ]]; then
      applied_count=$((applied_count + 1))
      if (( pending_count > 0 )); then
        percent=$((50 + applied_count * 40 / pending_count))
        (( percent > 90 )) && percent=90
        show_progress "${percent}" "Applied migration ${applied_count} of ${pending_count}"
      fi
    fi
  done
show_progress 100 "Database setup completed"
