# EsperancaSolidaria.Worker

Worker Service responsável por consumir eventos de doação do RabbitMQ e atualizar o valor arrecadado das campanhas na plataforma **Conexão Solidária**.

## Responsabilidades

- Consumir eventos `DonationReceivedEvent` da fila RabbitMQ
- Atualizar o campo `AmountRaised` da campanha no banco de dados
- Garantir processamento atômico e seguro de doações simultâneas

## Tecnologias

- .NET 10 Worker Service
- RabbitMQ.Client (consumo de fila)
- Dapper (update atômico no SQL Server)
- SQL Server

## Por que Dapper em vez de EF Core?

O update do `AmountRaised` é feito com SQL direto:

```sql
UPDATE Campaigns 
SET AmountRaised = AmountRaised + @Amount 
WHERE Id = @CampaignId AND Active = 1
```

Isso garante atomicidade — múltiplas doações simultâneas não causam race condition, pois o incremento é feito diretamente no banco sem carregar a entidade em memória.

## Estrutura do Projeto

```
src/
├── EsperancaSolidaria.Worker
│   ├── DonationWorker.cs          # BackgroundService principal
│   ├── Events/
│   │   └── DonationReceivedEvent.cs
│   └── Program.cs
└── EsperancaSolidaria.Worker.Infrastructure
    ├── Repositories/
    │   ├── ICampaignWorkerRepository.cs
    │   └── CampaignWorkerRepository.cs
    └── DependencyInjectionExtension.cs
```

## Fluxo de Processamento

```
RabbitMQ (fila: DonationReceivedEvent)
  → DonationWorker.ReceivedAsync
  → Deserializa DonationReceivedEvent { CampaignId, Amount }
  → CampaignWorkerRepository.UpdateAmountRaised()
  → UPDATE Campaigns SET AmountRaised += @Amount
  → BasicAck (sucesso) ou BasicNack (falha → requeue)
```

## Garantias de Confiabilidade

- `autoAck: false` — mensagem só é confirmada após processamento bem-sucedido
- `BasicNack` com requeue — em caso de falha a mensagem volta para a fila
- `IServiceScopeFactory` — repositório scoped criado por mensagem, evitando problemas de DbContext compartilhado
- Fila `durable: true` — mensagens sobrevivem a restart do RabbitMQ

## Como Rodar Localmente

### Pré-requisitos

- .NET 10 SDK
- SQL Server com banco `EsperancaSolidariaCampaign` criado
- RabbitMQ rodando na porta 5672
- Campanhas API já executada ao menos uma vez (para criar as tabelas via migration)

### Configuração

1. Clone o repositório:
```bash
git clone https://github.com/MatheusRoberto-Git/EsperancaSolidaria.Worker.git
cd EsperancaSolidaria.Worker
```

2. Configure o `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Connection": "Data Source=localhost\\SQLEXPRESS;Initial Catalog=EsperancaSolidariaCampaign;User Id=seu_usuario;Password=sua_senha;TrustServerCertificate=True;"
  },
  "Settings": {
    "RabbitMq": {
      "HostName": "localhost",
      "UserName": "guest",
      "Password": "guest"
    }
  }
}
```

3. Suba o RabbitMQ com Docker (se ainda não estiver rodando):
```bash
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

4. Execute o Worker:
```bash
dotnet run --project src/EsperancaSolidaria.Worker
```

### Rodar com Docker

```bash
docker build -t worker:latest .
docker run \
  -e ConnectionStrings__Connection="Server=host.docker.internal\\SQLEXPRESS;..." \
  -e Settings__RabbitMq__HostName="localhost" \
  worker:latest
```

### Rodar com Docker Compose

Na raiz da pasta `Hackathon FIAP`:
```bash
docker-compose up worker
```

## Kubernetes

```bash
kubectl apply -f k8s/
kubectl get pods -l app=worker
```

Para verificar os logs do Worker em tempo real:
```bash
kubectl logs -l app=worker -f
```

## Monitorar a Fila no RabbitMQ

Acesse o RabbitMQ Management UI:
```
http://localhost:15672
```
Login: `guest` / `guest`

A fila `DonationReceivedEvent` ficará visível com contagem de mensagens pendentes e processadas.

## Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__Connection` | Connection string do banco EsperancaSolidariaCampaign |
| `Settings__RabbitMq__HostName` | Host do RabbitMQ |
| `Settings__RabbitMq__UserName` | Usuário do RabbitMQ |
| `Settings__RabbitMq__Password` | Senha do RabbitMQ |