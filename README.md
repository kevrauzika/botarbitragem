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

O cálculo usa `EV = (probabilidade estimada × odd decimal) - 1`. EV positivo indica apenas valor matemático estimado, não garantia de resultado.

Uma oportunidade somente recebe `isQualified: true` quando passa por todos os filtros configuráveis de EV mínimo, edge mínimo, probabilidade e faixa de odds. Se nenhuma oportunidade passar, nenhuma pick deve ser publicada.
