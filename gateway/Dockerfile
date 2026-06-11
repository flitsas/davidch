FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY Flit.Gateway/Flit.Gateway.csproj Flit.Gateway/
RUN dotnet restore Flit.Gateway/Flit.Gateway.csproj
COPY Flit.Gateway/ Flit.Gateway/
RUN dotnet publish Flit.Gateway/Flit.Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache curl
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
  CMD curl -f http://127.0.0.1:8080/health || exit 1
CMD ["dotnet", "Flit.Gateway.dll"]
