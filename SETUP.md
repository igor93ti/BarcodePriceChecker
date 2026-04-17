# PriceChecker — Guia de Setup

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)
- Conta [Azure](https://azure.microsoft.com/free/) (gratuita)
- Conta [GitHub](https://github.com/)

---

## 1. Rodando localmente

```bash
# Clone o repositório
git clone https://github.com/igor93ti/BarcodePriceChecker.git
cd BarcodePriceChecker

# Rode a aplicação
dotnet run --project src/BarcodePriceChecker.Web

# Acesse em: http://localhost:5000
```

## 2. Rodando com Docker (App + Prometheus + Grafana)

```bash
docker compose up --build
```

| Serviço     | URL                         |
|-------------|------------------------------|
| App Blazor  | http://localhost:8080        |
| Prometheus  | http://localhost:9090        |
| Grafana     | http://localhost:3000        |

> Login Grafana: `admin` / `admin123` (mude em produção)

O dashboard **PriceChecker - Monitoramento** já vem pré-configurado.

---

## 3. GitHub — Criar repositório e configurar MCP

### 3.1 Repositório

Repositório já criado e configurado:
**https://github.com/igor93ti/BarcodePriceChecker**

```bash
git clone https://github.com/igor93ti/BarcodePriceChecker.git
```

### 3.2 Instalar GitHub MCP (Claude Code)

```bash
claude mcp add github -- npx @modelcontextprotocol/server-github
```

Configure no arquivo `~/.claude/settings.json`:
```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "ghp_SEU_TOKEN_AQUI"
      }
    }
  }
}
```

---

## 4. Azure — Configurar Deploy

### 4.1 Criar recursos no Azure (Free Tier)

```bash
# Login
az login

# Criar resource group
az group create --name rg-pricechecker --location brazilsouth

# Criar App Service Plan (Free F1)
az appservice plan create \
  --name plan-pricechecker \
  --resource-group rg-pricechecker \
  --sku F1 \
  --is-linux

# Criar Web App
az webapp create \
  --resource-group rg-pricechecker \
  --plan plan-pricechecker \
  --name pricechecker-app \
  --runtime "DOTNETCORE:8.0"
```

### 4.2 Criar Service Principal para GitHub Actions

```bash
az ad sp create-for-rbac \
  --name "pricechecker-github-actions" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-pricechecker \
  --sdk-auth
```

Copie o JSON gerado e adicione como **GitHub Secret**:
- Repositório → Settings → Secrets → `AZURE_CREDENTIALS`

### 4.3 Atualizar nome do App no CD

Edite `.github/workflows/cd.yml`:
```yaml
env:
  AZURE_WEBAPP_NAME: "pricechecker-app"  # ← seu nome aqui
```

---

## 5. CI/CD — Fluxo automático

| Evento                          | Pipeline   | Ação                               |
|---------------------------------|------------|------------------------------------|
| Push em qualquer branch         | CI         | Build + Testes                     |
| Pull Request para main/develop  | CI         | Build + Testes + Feedback no PR    |
| Merge / Push na main            | CD         | Build + Testes + Deploy no Azure   |
| Trigger manual                  | CD         | Deploy em ambiente escolhido       |

---

## 6. Monitoramento com Grafana Cloud (Azure + Produção)

Para produção no Azure, use o **Grafana Cloud** (tier gratuito):

1. Crie conta em https://grafana.com/
2. Crie um stack gratuito
3. Configure o Prometheus Remote Write no app via variáveis de ambiente:
   - `PROMETHEUS_REMOTE_WRITE_URL`
   - `PROMETHEUS_REMOTE_WRITE_USER`
   - `PROMETHEUS_REMOTE_WRITE_PASSWORD`

---

## 7. Estrutura do Projeto

```
BarcodePriceChecker/
├── src/
│   ├── Domain/          → Entidades: Product, PriceOffer, PriceComparison
│   ├── Application/     → Interfaces, DTOs, PriceComparisonService
│   ├── Infrastructure/  → APIs externas (Mercado Livre, Open Food Facts, Buscapé)
│   └── Web/             → Blazor Server (UI)
├── tests/               → Testes unitários com xUnit + Moq
├── docker/              → Configs Prometheus + Grafana
├── .github/workflows/   → CI (ci.yml) e CD (cd.yml)
├── Dockerfile           → Build multi-stage
└── docker-compose.yml   → Stack completa local
```

## 8. APIs utilizadas (todas gratuitas)

| API              | Uso                              | Auth      |
|------------------|----------------------------------|-----------|
| Open Food Facts  | Nome do produto por código EAN   | Não       |
| Mercado Livre    | Busca de preços e ofertas        | Não       |
| Buscapé          | Scraping de preços               | Não       |
