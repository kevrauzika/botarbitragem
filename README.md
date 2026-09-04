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
- `GET /api/matches`
- `GET /api/matches/{id}`
- `POST /api/matches`
- `POST /api/matches/{id}/odds`
- `POST /api/analysis/expected-value`
- `POST /api/analysis/no-vig/1x2`
- `POST /api/analysis/value-bet`
- `POST /api/admin/ingestion/{sportKey}` (`X-Admin-Key` obrigatório)

O cálculo usa `EV = (probabilidade estimada × odd decimal) - 1`. EV positivo indica apenas valor matemático estimado, não garantia de resultado.

Uma oportunidade somente recebe `isQualified: true` quando passa por todos os filtros configuráveis de EV mínimo, edge mínimo, probabilidade e faixa de odds. Se nenhuma oportunidade passar, nenhuma pick deve ser publicada.

## Coleta real de odds

O primeiro adaptador utiliza a The Odds API v4 e importa partidas pré-jogo e cotações `h2h`/1X2. Configure os segredos apenas por variáveis de ambiente:

```bash
export OddsProvider__ApiKey="sua-chave"
export Administration__ApiKey="um-segredo-longo"
curl -X POST http://localhost:5000/api/admin/ingestion/soccer_brazil_campeonato -H "X-Admin-Key: um-segredo-longo"
```

A importação é idempotente para a mesma partida, casa, mercado, seleção e instante de captura. A chave do provedor nunca deve ser adicionada ao repositório.
