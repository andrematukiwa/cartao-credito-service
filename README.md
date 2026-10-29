# Cartão de Crédito

Microsserviço que **consome o evento `PropostaGerada`** e **emite os cartões de crédito** (0, 1 ou 2) conforme a faixa de score aprovada na proposta. É a última etapa do fluxo de concessão de crédito:

```
Cadastro de Clientes ──ClienteCadastrado──▶ Proposta de Crédito ──PropostaGerada──▶ Cartão de Crédito
```

![Arquitetura da solução](docs/arquitetura.png)

O [fluxo detalhado](docs/fluxo-detalhado.png) mostra as decisões de cada serviço: validações, idempotência, retry e DLQ. Para editar os diagramas, abra a fonte no draw.io: [docs/fluxograma.drawio](docs/fluxograma.drawio).

## Stack

| | |
|---|---|
| Plataforma | .NET 10 (ASP.NET Core Web API + consumer MassTransit no mesmo processo) |
| Banco | PostgreSQL 17 + EF Core 10 (migrations, `snake_case`) |
| Mensageria | RabbitMQ 4.1 + MassTransit 8: retry exponencial, dead-letter queue e Inbox |
| Testes | xUnit |
| Documentação da API | OpenAPI + Swagger UI |

## Como executar

### Fluxo completo (recomendado)

O RabbitMQ precisa ser o mesmo para os três serviços. Com os repositórios clonados **lado a lado na mesma pasta**, rode a partir do `cadastro-clientes-service`:

```bash
docker compose -f docker-compose.fluxo-completo.yml up -d --build
```

| Recurso | Endereço |
|---|---|
| Swagger do Cartão | http://localhost:8083/swagger |
| Swagger do Cadastro | http://localhost:8081/swagger |
| Swagger da Proposta | http://localhost:8082/swagger |
| Painel do RabbitMQ | http://localhost:15672 (guest / guest) |

Cadastre um cliente pelo Swagger do Cadastro e, com o `id` devolvido, consulte `GET http://localhost:8083/api/v1/cartoes/{clienteId}`.

### Só este serviço

```bash
docker compose up -d --build
```

Sobe a API (porta 8083), o PostgreSQL e um RabbitMQ próprio. Útil para desenvolver isoladamente; sem os outros serviços, as mensagens podem ser publicadas pelo painel do RabbitMQ.

Para rodar a API fora do Docker (porta 5140):

```bash
docker compose up -d cartao-postgres rabbitmq
dotnet run --project src/CartaoCredito.Api
```

### Testes

```bash
dotnet test
```

Os testes de unidade cobrem a emissão (1 ou 2 cartões, limite por cartão, validade, número mascarado), a proposta negada, a idempotência e cada tipo de mensagem inválida que vai direto para a DLQ. Repositório, unit of work e gerador de número são substituídos por mocks (NSubstitute), e o relógio por `FakeTimeProvider`.

## Endpoint

| Método | Rota | Descrição | Respostas |
|---|---|---|---|
| `GET` | `/api/v1/cartoes/{clienteId}` | Lista os cartões do cliente | `200` |

Devolve uma **lista vazia** quando a proposta foi negada ou ainda está em processamento: os cartões são emitidos de forma assíncrona após a proposta (consistência eventual).

```json
[
  {
    "id": "64ab9b8a-c17b-4d4b-9fbb-45d4467fd5f2",
    "propostaId": "871685cc-e21b-4383-b40a-0afbf8a85c6b",
    "nomeTitular": "Maria Silva",
    "numero": "**** **** **** 9989",
    "validade": "10/2031",
    "limite": 5000.00,
    "status": "Ativo",
    "emitidoEm": "2026-10-29T15:03:23.094459Z"
  },
  {
    "id": "a5bc7014-12fe-4ae4-991b-e3915ab7b356",
    "propostaId": "871685cc-e21b-4383-b40a-0afbf8a85c6b",
    "nomeTitular": "Maria Silva",
    "numero": "**** **** **** 5230",
    "validade": "10/2031",
    "limite": 5000.00,
    "status": "Ativo",
    "emitidoEm": "2026-10-29T15:03:23.094459Z"
  }
]
```

## Regras de emissão

| Proposta | Cartões emitidos |
|---|---|
| Negada | nenhum |
| Aprovada, score 101–500 | 1 cartão de R$ 1.000,00 |
| Aprovada, score 501–1000 | 2 cartões de R$ 5.000,00 cada (R$ 10.000,00 no total) |

A decisão de crédito (status, quantidade e limite) é da **Proposta**. Este serviço só executa a emissão, sem repetir a regra de score.

Cada cartão sai com:
- **Número mascarado** (`**** **** **** 1234`): só os 4 dígitos finais são gerados e armazenados (veja *Decisões técnicas*).
- **Validade** no último dia do mês, 5 anos após a emissão (`MM/AAAA`).
- **Status** `Ativo`.

## Mensageria

| | Evento | Exchange | Fila |
|---|---|---|---|
| Consome | `PropostaGerada` | `proposta-gerada` | `cartao-proposta-gerada` |

## Resiliência

| Mecanismo | O que resolve |
|---|---|
| **Retry exponencial** (5 tentativas, de 1s até 30s) | falhas transitórias, como o banco fora do ar por alguns segundos |
| **Dead-letter queue** (`cartao-proposta-gerada_error`) | esgotadas as tentativas, a mensagem não se perde: fica na DLQ com o motivo da falha nos headers, pronta para reprocessamento |
| **Mensagem inválida sem retry** | dados que nunca vão funcionar (ex.: 3 cartões, status desconhecido) vão direto para a DLQ, sem gastar tentativas |
| **Inbox** | a mesma mensagem entregue duas vezes é processada uma vez só |
| **Idempotência de negócio** + índice único em `(proposta_id, sequencial)` | a mesma proposta nunca gera cartões duas vezes, mesmo que o evento seja republicado |

Todos os cenários foram verificados com o fluxo rodando: o banco derrubado durante o processamento (o cartão foi emitido após as retentativas), mensagens inválidas (foram para a DLQ na primeira tentativa) e o evento republicado (ignorado, sem cartões duplicados).

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|---|---|---|
| `ConnectionStrings__CartaoDb` | Conexão com o PostgreSQL | `Host=cartao-postgres;Port=5432;Database=cartao;Username=postgres;Password=postgres` |
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Port` | Porta AMQP (padrão `5672`) | `5672` |
| `RabbitMq__VirtualHost` | Virtual host (padrão `/`) | `/` |
| `RabbitMq__Username` | Usuário | `guest` |
| `RabbitMq__Password` | Senha | `guest` |
| `Database__ApplyMigrationsOnStartup` | Aplica as migrations ao subir (ligado em `Development`) | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Development` habilita Swagger e migrations automáticas | `Development` |

A aplicação **não sobe** se a connection string ou as credenciais do RabbitMQ estiverem ausentes (fail-fast).

## Estrutura

```
src/
├── CartaoCredito.Api              → Controller, tratamento de erros, Program.cs
├── CartaoCredito.Application      → Caso de uso (CartaoService), DTOs, contrato do evento
├── CartaoCredito.Domain           → Entidade Cartao (emissão, validade), gerador de número
└── CartaoCredito.Infrastructure   → EF Core, repositório, consumer + retry/DLQ, migrations
tests/
└── CartaoCredito.UnitTests        → xUnit, espelhando a estrutura de src/
```

## Decisões técnicas

**O número completo do cartão não é armazenado (PCI-DSS).** Em produção, o número (PAN) é gerado e tokenizado pela processadora; o sistema guarda só o token e os 4 dígitos finais. Aqui são gerados e guardados apenas os dígitos finais, com `RandomNumberGenerator` (criptográfico, não previsível). O gerador é uma interface (`IGeradorNumeroCartao`), o que permite fixar o valor nos testes.

**Emissão é responsabilidade do Cartão; a decisão é da Proposta.** O serviço confia na quantidade e no limite informados no evento e não recalcula score. Assim, uma mudança na regra de crédito acontece num lugar só.

**Idempotência por proposta.** O índice único `(proposta_id, sequencial)` impede um "terceiro cartão" ou a duplicação dos dois, mesmo com duas entregas concorrentes da mesma mensagem.

**Lista vazia em vez de 404.** `GET /cartoes/{clienteId}` é uma listagem: um cliente sem cartões (negado ou ainda em processamento) é uma resposta válida, não um erro.

**Status como texto.** No banco o status é `"Ativo"`: legível em consultas e sem depender da ordem do enum.

**Contratos sem biblioteca compartilhada.** O evento consumido é uma cópia local identificada pela URN `proposta-gerada`, o que permite deploys independentes.

**Sem evento de saída.** O desafio termina na emissão; um `CartaoEmitido` (para notificar o cliente, por exemplo) seria o próximo passo natural, mas ficou de fora para não criar um evento sem consumidor.

**MassTransit 8.x.** A partir da versão 9 o MassTransit passou a exigir licença comercial; a 8.x segue open source (Apache 2.0).
