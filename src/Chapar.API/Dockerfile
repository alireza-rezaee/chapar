FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG TARGETARCH
ARG APP_VERSION=1.0.0
ARG APP_INFO_VERSION=1.0.0

COPY --link ./*.csproj .
RUN dotnet restore \
    --os linux \
    ${TARGETARCH:+--arch $TARGETARCH}

COPY --link . ./

RUN dotnet publish . \
    --no-restore \
    --configuration Release \
    --os linux \
    ${TARGETARCH:+--arch $TARGETARCH} \
    --output /app/publish \
    -p:Version="${APP_VERSION}" \
    -p:InformationalVersion="${APP_INFO_VERSION}" \
    -p:UseAppHost=false \
    -p:StripSymbols=true \
    -p:DebugType=None \
    -p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Chapar.API.dll"]
