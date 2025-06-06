FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
# Add the custom CA certificate to the trust store:
USER root
COPY ["certs/ca.pem", "/usr/local/share/ca-certificates/my-custom-docker-ca.crt"]
RUN update-ca-certificates
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_PROFILE=Production
WORKDIR /src
RUN apt-get update && \
    apt-get install -y --no-install-recommends nodejs npm && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
COPY ["SimpleScriptWebSite/SimpleScriptWebSite.csproj", "SimpleScriptWebSite/"]
RUN dotnet restore "SimpleScriptWebSite/SimpleScriptWebSite.csproj"
COPY . .
WORKDIR "/src/SimpleScriptWebSite"
RUN dotnet build "SimpleScriptWebSite.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:BUILD_PROFILE=$BUILD_PROFILE

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG BUILD_PROFILE=Production
RUN dotnet publish "SimpleScriptWebSite.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:BUILD_PROFILE=$BUILD_PROFILE

FROM base AS final
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release && \
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null && \
    apt-get update && \
    apt-get install -y docker-ce-cli

COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SimpleScriptWebSite.dll"]
