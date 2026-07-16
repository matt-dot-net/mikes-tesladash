FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TeslaDash/TeslaDash.csproj TeslaDash/
RUN dotnet restore TeslaDash/TeslaDash.csproj
COPY . .
RUN dotnet publish TeslaDash/TeslaDash.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Tesla__TokenPath=/data/tesla-tokens.protected \
    Tesla__DataProtectionPath=/data/keys
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TeslaDash.dll"]
