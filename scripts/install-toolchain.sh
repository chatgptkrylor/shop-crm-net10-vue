#!/usr/bin/env bash
set -euo pipefail

echo "=== .NET 10 SDK ==="
if command -v dotnet >/dev/null 2>&1 && dotnet --version | grep -q '^10\.'; then
  echo "dotnet 10.x already installed: $(dotnet --version)"
else
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/opencode/dotnet-install.sh
  chmod +x /tmp/opencode/dotnet-install.sh
  /tmp/opencode/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
  grep -q 'export PATH="$HOME/.dotnet:$PATH"' "$HOME/.bashrc" || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.bashrc"
  echo "Installed dotnet: $(dotnet --version)"
fi

echo "=== SQL Server 2022 Developer ==="
if systemctl is-active --quiet mssql-server; then
  echo "mssql-server already running"
else
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
  echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022/ jammy main" | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
  sudo apt-get update
  sudo apt-get install -y mssql-server

  # SQL Server 2022 needs OpenLDAP 2.5 libs (Ubuntu 24.04 ships 2.6)
  if [ ! -f /usr/lib/x86_64-linux-gnu/liblber-2.5.so.0 ]; then
    curl -fsSL "http://archive.ubuntu.com/ubuntu/pool/main/o/openldap/libldap-2.5-0_2.5.16+dfsg-0ubuntu0.22.04.2_amd64.deb" -o /tmp/opencode/libldap25.deb
    cd /tmp/opencode && dpkg-deb -x libldap25.deb libldap25
    sudo cp libldap25/usr/lib/x86_64-linux-gnu/liblber-2.5.so.0 /usr/lib/x86_64-linux-gnu/
    sudo cp libldap25/usr/lib/x86_64-linux-gnu/liblber-2.5.so.0.1.11 /usr/lib/x86_64-linux-gnu/
    sudo cp libldap25/usr/lib/x86_64-linux-gnu/libldap-2.5.so.0 /usr/lib/x86_64-linux-gnu/
    sudo cp libldap25/usr/lib/x86_64-linux-gnu/libldap-2.5.so.0.1.11 /usr/lib/x86_64-linux-gnu/
    sudo ldconfig
    cd -
  fi

  echo "Run: sudo MSSQL_SA_PASSWORD=<password> MSSQL_PID=Developer ACCEPT_EULA=Y /opt/mssql/bin/mssql-conf setup accept-eula"
  echo "Then re-run this script."
  exit 1
fi

echo "=== mssql-tools (sqlcmd) ==="
if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/22.04/prod jammy main" | sudo tee /etc/apt/sources.list.d/mssql-tools.list
  sudo apt-get update
  echo "mssql-tools18 mssql-tools18/accept_eula boolean true" | sudo debconf-set-selections
  sudo ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
  echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
  export PATH="$PATH:/opt/mssql-tools18/bin"
fi

echo "=== dotnet dev-certs ==="
export PATH="$HOME/.dotnet:$PATH"
dotnet dev-certs https --trust 2>/dev/null || true

echo "=== Toolchain ready ==="
echo "dotnet: $(dotnet --version)"
echo "sqlcmd: $(command -v sqlcmd || echo 'not on PATH - source ~/.bashrc')"