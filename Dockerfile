# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copia arquivos de projeto para aproveitar cache de camadas
COPY BarcodePriceChecker.sln .
COPY src/BarcodePriceChecker.Domain/BarcodePriceChecker.Domain.csproj                   src/BarcodePriceChecker.Domain/
COPY src/BarcodePriceChecker.Application/BarcodePriceChecker.Application.csproj         src/BarcodePriceChecker.Application/
COPY src/BarcodePriceChecker.Infrastructure/BarcodePriceChecker.Infrastructure.csproj   src/BarcodePriceChecker.Infrastructure/
COPY src/BarcodePriceChecker.Web/BarcodePriceChecker.Web.csproj                         src/BarcodePriceChecker.Web/
COPY tests/BarcodePriceChecker.Tests/BarcodePriceChecker.Tests.csproj                   tests/BarcodePriceChecker.Tests/

RUN dotnet restore

# Copia todo o código fonte
COPY . .

# Roda os testes
RUN dotnet test --no-restore --verbosity minimal

# Publica a aplicação
RUN dotnet publish src/BarcodePriceChecker.Web/BarcodePriceChecker.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Usuário não-root por segurança
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BarcodePriceChecker.Web.dll"]
