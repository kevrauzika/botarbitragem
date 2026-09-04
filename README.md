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
- `GET /api/opportunities?status=active&kind=arbitrage`
- `GET /api/opportunities/{id}`
- `POST /api/admin/opportunities/scan` (`X-Admin-Key` obrigatório)
- `GET /api/bets`
- `GET /api/bets/performance`
- `POST /api/bets` (`X-Admin-Key` obrigatório)
- `POST /api/bets/{id}/settle` (`X-Admin-Key` obrigatório)
- `POST /api/bets/{id}/void` (`X-Admin-Key` obrigatório)

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

## Monitoramento automático

O worker interno pode coletar as ligas configuradas, analisar as odds recentes e persistir dois tipos de oportunidade:

- `value-bet`: usa a mediana das probabilidades sem margem das casas como referência e aplica a política de EV/edge;
- `arbitrage`: combina a melhor odd de cada resultado, exige retorno teórico mínimo e calcula a porcentagem da stake para cada perna.

Por segurança, a coleta automática começa desabilitada até que a chave do provedor seja configurada. Para ativar:

```bash
export Automation__IngestionEnabled="true"
export Automation__ScanEnabled="true"
export Automation__IntervalSeconds="300"
```

Somente odds dentro de `OpportunityPolicy:MaximumOddsAgeMinutes` são consideradas. Oportunidades que deixam de aparecer são marcadas como `expired`.

## Alertas pelo Telegram

O envio é opcional e não impede a análise quando estiver desabilitado:

```bash
export Telegram__Enabled="true"
export Telegram__BotToken="token-do-bot"
export Telegram__ChatId="id-do-chat"
```

Alertas repetidos respeitam `OpportunityPolicy:AlertCooldownMinutes`. Nunca versionar o token real.

## Registro e desempenho

Uma oportunidade pode ser registrada como `paper` (simulação) ou `actual` (apenas acompanhamento de uma aposta feita fora do sistema). O sistema não envia apostas para bookmakers. As odds e stakes de cada perna são congeladas no registro para preservar o histórico.

Depois da partida, informe o retorno efetivamente recebido no endpoint `settle`, ou marque como `void`. O endpoint `GET /api/bets/performance` calcula exposição pendente, resultado e ROI separadamente por moeda.
