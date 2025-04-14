FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

USER root

ARG AUTH0_DOMAIN
ENV Auth0__Domain=$AUTH0_DOMAIN

ARG AUTH0_CLIENTID
ENV Auth0__ClientId=$AUTH0_CLIENTID

WORKDIR /app

RUN update-ca-certificates

#USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ./src/frontend/frontend.csproj ./frontend/

COPY ./Directory.Packages.props .

WORKDIR /src/frontend

RUN dotnet restore "./frontend.csproj"

WORKDIR /src

COPY ./src/frontend ./frontend

WORKDIR /src/frontend

RUN dotnet build "./frontend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./frontend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "frontend.dll"]