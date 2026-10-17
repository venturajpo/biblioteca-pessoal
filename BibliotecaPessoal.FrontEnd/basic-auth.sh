#!/bin/sh
# Liga a autenticacao HTTP basica quando BASIC_AUTH_USER e BASIC_AUTH_PASSWORD estao definidos.
set -eu

auth_conf=/etc/nginx/basic-auth.conf
htpasswd=/etc/nginx/.htpasswd
user="${BASIC_AUTH_USER:-}"
password="${BASIC_AUTH_PASSWORD:-}"

if [ -z "$user" ] && [ -z "$password" ]; then
    : > "$auth_conf"
    echo "basic-auth: desligada"
    exit 0
fi

# So metade definida: falha em vez de subir o site aberto.
if [ -z "$user" ] || [ -z "$password" ]; then
    echo "basic-auth: defina BASIC_AUTH_USER e BASIC_AUTH_PASSWORD juntos" >&2
    exit 1
fi

printf '%s:%s\n' "$user" "$(cryptpw -m sha512 "$password")" > "$htpasswd"
chown root:nginx "$htpasswd"
chmod 640 "$htpasswd"

printf 'auth_basic "Biblioteca Pessoal";\nauth_basic_user_file %s;\n' "$htpasswd" > "$auth_conf"
echo "basic-auth: ligada para o usuario $user"
