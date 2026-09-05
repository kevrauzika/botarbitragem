# BotArbitragem

API para análise probabilística de odds de futebol. O objetivo é identificar possíveis apostas de valor usando probabilidades estimadas, odds de mercado e filtros de qualidade — sem promessa de retorno.

A implementação inicial usa .NET 8, PostgreSQL e Clean Architecture.

## Executar localmente

```bash
docker compose up -d
dotnet run --project src/BotArbitragem.Api
```

Swagger: `http://localhost:5000/swagger`

## Endpoints disponíveis

- `GET /health`
- `GET /api/matches?page=1&pageSize=50` (`pageSize` máximo de 100)
- `GET /api/matches/{id}?oddsLimit=200` (`oddsLimit` máximo de 1000)
- `POST /api/matches`
- `POST /api/matches/{id}/odds`
- `POST /api/analysis/expected-value`
- `POST /api/analysis/no-vig/1x2`
- `POST /api/analysis/value-bet`
- `GET /api/analysis/matches/{id}/value-bets`
- `POST /api/admin/ingestion/{sportKey}` (`X-Admin-Key` obrigatório)
- `POST /api/admin/publication/matches/{id}` (`X-Admin-Key` obrigatório)
- `POST /api/admin/telegram/test` (`X-Admin-Key` obrigatório)

O cálculo usa `EV = (probabilidade estimada × odd decimal) - 1`. EV positivo indica apenas valor matemático estimado, não garantia de resultado.

Uma oportunidade somente recebe `isQualified: true` quando passa por todos os filtros configuráveis de EV mínimo, edge mínimo, probabilidade e faixa de odds. Se nenhuma oportunidade passar, nenhuma pick deve ser publicada.

O endpoint de análise por partida calcula uma probabilidade justa de consenso usando as cotações 1X2 sem margem das outras casas. Por padrão, exige duas casas independentes como referência, ignora mercados incompletos e descarta cotações com mais de 30 minutos. A casa avaliada nunca participa do próprio consenso.

## Coleta real de odds

O primeiro adaptador utiliza a The Odds API v4 e importa partidas pré-jogo e cotações `h2h`/1X2. Configure os segredos apenas por variáveis de ambiente:

```bash
export OddsProvider__ApiKey="sua-chave"
export Administration__ApiKey="um-segredo-longo"
curl -X POST http://localhost:5000/api/admin/ingestion/soccer_brazil_campeonato -H "X-Admin-Key: um-segredo-longo"
```

A importação é idempotente para a mesma partida, casa, mercado, seleção e instante de captura. A chave do provedor nunca deve ser adicionada ao repositório.

## Alertas no Telegram

Adicione o bot ao grupo e configure `Telegram__BotToken` e `Telegram__ChatId` por variáveis de ambiente. Se o grupo usa tópicos, configure também `Telegram__MessageThreadId`. Com `Monitoring__Enabled=true`, a API coleta as ligas permitidas, analisa partidas futuras e envia somente oportunidades ainda não publicadas. O monitoramento fica desativado por padrão e os tokens nunca são registrados nos logs HTTP.

Para validar a conexão antes de ativar o monitoramento:

```bash
curl -X POST http://localhost:5000/api/admin/telegram/test -H "X-Admin-Key: um-segredo-longo"
```

## Deploy em producao

O `Dockerfile` da raiz publica a API em .NET 8 na porta `8080`. Provedores que injetam a variavel `PORT` tambem sao suportados automaticamente.

Configure os seguintes segredos no provedor:

```text
ConnectionStrings__Postgres=<conexao PostgreSQL do provedor>
Administration__ApiKey=<segredo longo e aleatorio>
OddsProvider__ApiKey=<chave da The Odds API>
Telegram__Enabled=true
Telegram__BotToken=<token do bot>
Telegram__ChatId=<id do grupo>
Automation__IngestionEnabled=true
Automation__ScanEnabled=true
Monitoring__Enabled=true
```

Use `/health` como health check. As migrations sao aplicadas automaticamente ao iniciar a API.
