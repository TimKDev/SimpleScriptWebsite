## Debug

Start backend with `docker compose up` or using Rider.

The frontend is not automatically reloaded when changes. Use `npm run dev` for running the frontend. For API calls to
work CORS must be enabled in the api in dev mode.

## Release

Rebuild images such that frontend is updated: `docker compose build --no-cache`

Start full application with `docker compose up`. No CORS needed anymore.

## Docker in Docker Setup

General Setup:
Asp.net Application is running inside a docker container and needs to be able to create and manage docker containers
dynamically in order to provide a sandbox environment to execute the console application in an isolated environment. For
safety purposes this application should not directy interact with my host docker demon, because this application would
need root access or at least docker group access, which could be dangerous.

Instead I am using a second container, which provides another docker demon that the asp.net application can use. This
Docker in Docker Container (DinD) is then used to create new Containers in this isolated Docker network. The Asp.net
Application does not need root permissions for this.

### TLS

Currently it is possible to communicate which the Docker demon using TCP without TLS, but in future versions, this will
not be supported. Therefore I needed to implement TLS communication between the Asp.net docker container and the DinD
container. Docker wants to use a certificate for the DinD Container (Server) and also a Client Certificate for the
Asp.net Container (Client).

The following steps were necessary to archive this:

1. Use `openssl` to create a private Key for a Custom CA. Then use this private Key to create a Certificate for this
   custom CA.
2. Create a private key and certificate for the Server (DinD Container) and the Client (Asp.net Container). Both
   certificates are issued by the Custom CA.
3. The certificates are moved into the containers using volumns.
4. The DinD Container is configured to use these certificates using a daemon.json file, which is also provided by a
   docker volumn.
5. The CA certificate needed to be added to the trusted certificate store of the asp.net application container. This is
   done by providing the certificate to the container (using COPY in the Dockerfile) and then using the command (
   `update-ca-certificates`). This added the Custom certificate to the file `etc/ssl/certs/ca-certificates.crt`. This
   file contains all certificates which the Debian/Ubuntu Host trust by default.
6. The Connection to the DinD Container needes to be modified in the C# Code in order to use the Client certificate.

### Certificate generation with `openssl`
The following steps can be executed by running the script `Scripts/generate-certs.sh`. As Common Name an arbitrary string needs to be prompted, but "." is not allowed. 
1. `mkdir -p certs/client certs/server`
2. `openssl genrsa -aes256 -out certs/ca-key.pem 4096`: Create private Key for Custom CA.
3. `openssl req -new -x509 -days 365 -key certs/ca-key.pem -sha256 -out certs/ca.pem`: Create public certificate for
   Custom CA. Common Name is irrelevant.
4. `openssl genrsa -out certs/server/server-key.pem 4096`: Create private Key for Server (DinD).
5. `openssl req -subj "/CN=docker" -sha256 -new -key certs/server/server-key.pem -out certs/server/server.csr`: Creates
   a Certificate Signing Request (CSR). This is a standard to provide a CA with information how a certificate should be
   created. In this case it defines the Common Name as `docker` which must match DNS name of the DinD Container in the
   docker-compose network.
6. Create a Extension file `certs/server/server-ext.cnf` with the content: ===========

```
subjectAltName = DNS:docker,IP:127.0.0.1
extendedKeyUsage = serverAuth 
```

7.
`openssl x509 -req -days 365 -sha256 -in certs/server/server.csr -CA certs/ca.pem -CAkey certs/ca-key.pem -CAcreateserial -out certs/server/server.pem -extfile certs/server/server-ext.cnf`
uses the Custom CA to create a certificate with the information defined in the CSR.

8. `openssl genrsa -out certs/client/client-key.pem 4096`: Create private Key for Client.
9. `openssl req -subj "/CN=client" -new -key certs/client/client-key.pem -out certs/client/client.csr`: CSR for Common
   Name for Client, but the Common Name is not important.
10. Create a Client Extension File `certs/client/client-ext.cnf` with the content: `extendedKeyUsage = clientAuth`. This
    defines that
    this a client certificate.
11.

`openssl x509 -req -days 365 -sha256 -in certs/client/client.csr -CA certs/ca.pem -CAkey certs/ca-key.pem -CAcreateserial -out certs/client/client.pem -extfile certs/client/client-ext.cnf`:
Create the client certificate.

12. `chmod --reference=client.pem client-key.pem`: Copy correct permission to key.
    
