FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY Flit.Identity.sln ./
COPY src/Flit.Identity.Api/Flit.Identity.Api.csproj src/Flit.Identity.Api/
COPY src/Flit.Identity.Auth/Flit.Identity.Auth.csproj src/Flit.Identity.Auth/
COPY src/Flit.Identity.Users/Flit.Identity.Users.csproj src/Flit.Identity.Users/
COPY src/Flit.Identity.Rbac/Flit.Identity.Rbac.csproj src/Flit.Identity.Rbac/
COPY src/Flit.Identity.Notifications/Flit.Identity.Notifications.csproj src/Flit.Identity.Notifications/
COPY src/Flit.Identity.Infrastructure/Flit.Identity.Infrastructure.csproj src/Flit.Identity.Infrastructure/
COPY src/Flit.Identity.Shared/Flit.Identity.Shared.csproj src/Flit.Identity.Shared/
COPY src/Flit.Companies.Shared/Flit.Companies.Shared.csproj src/Flit.Companies.Shared/
COPY src/Flit.Companies.Infrastructure/Flit.Companies.Infrastructure.csproj src/Flit.Companies.Infrastructure/
COPY src/Flit.Companies.Admin/Flit.Companies.Admin.csproj src/Flit.Companies.Admin/
COPY src/Flit.Companies.Runt/Flit.Companies.Runt.csproj src/Flit.Companies.Runt/
COPY src/Flit.OT.Shared/Flit.OT.Shared.csproj src/Flit.OT.Shared/
COPY src/Flit.OT.Infrastructure/Flit.OT.Infrastructure.csproj src/Flit.OT.Infrastructure/
COPY src/Flit.OT.Admin/Flit.OT.Admin.csproj src/Flit.OT.Admin/
RUN dotnet restore src/Flit.Identity.Api/Flit.Identity.Api.csproj
COPY src/ src/
RUN dotnet publish src/Flit.Identity.Api/Flit.Identity.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache curl
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build /app/publish .
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
  CMD curl -f http://127.0.0.1:80/api/health || exit 1
CMD ["dotnet", "Flit.Identity.Api.dll"]
