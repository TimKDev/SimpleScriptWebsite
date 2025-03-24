FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN apt-get update && apt-get install -y nodejs npm
COPY ["SimpleScriptWebSite/SimpleScriptWebSite.csproj", "SimpleScriptWebSite/"]
RUN dotnet restore "SimpleScriptWebSite/SimpleScriptWebSite.csproj"
COPY . .
WORKDIR "/src/SimpleScriptWebSite"
RUN dotnet build "SimpleScriptWebSite.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SimpleScriptWebSite.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleScriptWebSite.dll"]
