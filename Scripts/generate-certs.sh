
#!/bin/bash
set -euo pipefail

echo "🔧 Step 1: Creating directory structure..."
mkdir -p certs/client certs/server

echo "🔐 Step 2: Generating private key for the Custom CA (will prompt for passphrase)..."
openssl genrsa -aes256 -out certs/ca-key.pem 4096

echo "📝 Step 3: Generating self-signed CA certificate (valid for 365 days)..."
openssl req -new -x509 -days 365 -key certs/ca-key.pem -sha256 -out certs/ca.pem

echo "🔐 Step 4: Generating private key for the server (DinD)..."
openssl genrsa -out certs/server/server-key.pem 4096

echo "📄 Step 5: Creating server Certificate Signing Request (CSR) with CN=docker..."
openssl req -subj "/CN=docker" -sha256 -new -key certs/server/server-key.pem -out certs/server/server.csr

echo "🛠️  Step 6: Creating server extension configuration file..."
cat > certs/server/server-ext.cnf <<EOF
subjectAltName = DNS:docker,IP:127.0.0.1
extendedKeyUsage = serverAuth
EOF

echo "📜 Step 7: Generating signed server certificate using the Custom CA..."
openssl x509 -req -days 365 -sha256 -in certs/server/server.csr \
  -CA certs/ca.pem -CAkey certs/ca-key.pem -CAcreateserial \
  -out certs/server/server.pem -extfile certs/server/server-ext.cnf

echo "🔐 Step 8: Generating private key for the client..."
openssl genrsa -out certs/client/client-key.pem 4096

echo "📄 Step 9: Creating client Certificate Signing Request (CSR) with CN=client..."
openssl req -subj "/CN=client" -new -key certs/client/client-key.pem -out certs/client/client.csr

echo "🛠️  Step 10: Creating client extension configuration file..."
echo "extendedKeyUsage = clientAuth" > certs/client/client-ext.cnf

echo "📜 Step 11: Generating signed client certificate using the Custom CA..."
openssl x509 -req -days 365 -sha256 -in certs/client/client.csr \
  -CA certs/ca.pem -CAkey certs/ca-key.pem -CAcreateserial \
  -out certs/client/client.pem -extfile certs/client/client-ext.cnf

echo "🔐 Step 12: Setting correct permissions on client private key..."
chmod --reference=certs/client/client.pem certs/client/client-key.pem

echo "✅ All certificates and keys have been successfully generated!"
