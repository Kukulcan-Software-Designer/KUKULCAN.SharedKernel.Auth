#!/usr/bin/env bash
set -euo pipefail

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
    1) provider="SqlServer"; port="1433" ;;
    2) provider="PostgresSql"; port="5432" ;;
    3) provider="MySql"; port="3306" ;;
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

mkdir -p "$(dirname "${ENV_FILE}")"
umask 077
cat > "${ENV_FILE}" <<EOF
export KUKULCAN_DATABASE_PROVIDER="${provider}"
export KUKULCAN_DATABASE_CONNECTION_STRING="${connection_string}"
EOF
chmod 600 "${ENV_FILE}"

source_line='[ -f "$HOME/.config/kukulcan/database.env" ] && . "$HOME/.config/kukulcan/database.env"'
touch "${SHELL_RC}"
if ! grep -Fqx "${source_line}" "${SHELL_RC}" 2>/dev/null; then
  printf '\n%s\n' "${source_line}" >> "${SHELL_RC}"
fi

export KUKULCAN_DATABASE_PROVIDER="${provider}"
export KUKULCAN_DATABASE_CONNECTION_STRING="${connection_string}"

printf '%s\n' "Las variables de entorno de base de datos han sido registradas."
printf '%s\n' "Provider: ${provider}"
printf '%s\n' "Host: localhost"
printf '%s\n' "Port: ${port}"
printf '%s\n' "La configuración persistirá para futuras sesiones de terminal."
