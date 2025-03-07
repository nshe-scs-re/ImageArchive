FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

USER root

WORKDIR /app

RUN update-ca-certificates

USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ./src/api/api.csproj ./api/

COPY ./Directory.Packages.props .

WORKDIR /src/api

RUN dotnet restore "./api.csproj"

WORKDIR /src

COPY ./src/api ./api

WORKDIR /src/api

RUN dotnet build "./api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "api.dll"]