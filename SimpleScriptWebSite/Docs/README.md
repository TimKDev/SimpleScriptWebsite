## Debug
Start backend with `docker compose up` or using Rider. 

The frontend is not automatically reloaded when changes. Use `npm run dev` for running the frontend. For API calls to work CORS must be enabled in the api in dev mode.

## Release
Rebuild images such that frontend is updated: `docker compose build --no-cache`

Start full application with `docker compose up`. No CORS needed anymore.

## Docker in Docker Setup
General Setup:
Asp.net Application is running inside a docker container and needs to be able to create and manage docker containers dynamically in order to provide a sandbox environment to execute the console application in an isolated environment. For safety purposes this application should not directy interact with my host docker demon, because this application would need root access or at least docker group access, which could be dangerous. 

Instead I am using a second container, which provides another docker demon that the asp.net application can use. This Docker in Docker Container (DinD) is then used to create new Containers in this isolated Docker network. The Asp.net Application does not need root permissions for this.

### TLS 
Currently it is possible to communicate which the Docker demon using TCP without TLS, but in future versions, this will not be supported. Therefore I needed to implement TLS communication between the Asp.net docker container and the DinD container. Docker wants to use a certificate for the DinD Container (Server) and also a Client Certificate for the Asp.net Container (Client). 

The following steps were necessary to archive this:
1. Use `openssl` to create a private Key for a Custom CA. Then use this private Key to create a Certificate for this custom CA.
2. Create a private key and certificate for the Server (DinD Container) and the Client (Asp.net Container). Both certificates are issued by the Custom CA. 
3. The certificates are moved into the containers using volumns. 
4. The DinD Container is configured to use these certificates using a daemon.json file, which is also provided by a docker volumn. 
5. The CA certificate needed to be added to the trusted certificate store of the asp.net application container. This is done by providing the certificate to the container (using COPY in the Dockerfile) and then using the command (`update-ca-certificates`). This added the Custom certificate to the file `etc/ssl/certs/ca-certificates.crt`. This file contains all certificates which the Debian/Ubuntu Host trust by default. 
6. The Connection to the DinD Container needes to be modified in the C# Code in order to use the Client certificate. 