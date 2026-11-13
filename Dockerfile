# Multi-stage build: build tools never reach the runtime image.
# Optional: App Service runs the published app directly, but this lets you run it anywhere
# (or move to Azure Container Apps later).

FROM node:22-alpine AS assets
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY scripts ./scripts
COPY src/CpdBooking.Web/Styles ./src/CpdBooking.Web/Styles
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ ./src/
COPY --from=assets /src/src/CpdBooking.Web/wwwroot ./src/CpdBooking.Web/wwwroot
RUN dotnet publish src/CpdBooking.Web/CpdBooking.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Run as the non-root user that the .NET images provide.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CpdBooking.Web.dll"]
