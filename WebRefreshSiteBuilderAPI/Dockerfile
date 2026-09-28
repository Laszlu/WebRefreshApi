FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebRefreshSiteBuilderAPI/WebRefreshSiteBuilderAPI.csproj", "WebRefreshSiteBuilderAPI/"]
RUN dotnet restore "WebRefreshSiteBuilderAPI/WebRefreshSiteBuilderAPI.csproj"
COPY . .
WORKDIR "/src/WebRefreshSiteBuilderAPI"
RUN dotnet build "./WebRefreshSiteBuilderAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebRefreshSiteBuilderAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
WORKDIR /app
COPY --from=publish /app/publish .

# The .NET package ships the Playwright driver but not a browser. Install the
# Chromium revision matching Microsoft.Playwright plus its Debian dependencies
# during the image build. A fixed path lets the non-root application user find
# the browser at runtime.
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN node_path="$(find .playwright/node -type f -name node -print -quit)" \
    && test -n "$node_path" \
    && "$node_path" .playwright/package/cli.js install --with-deps chromium

USER $APP_UID
ENTRYPOINT ["dotnet", "WebRefreshSiteBuilderAPI.dll"]
