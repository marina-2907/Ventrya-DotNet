# Ventrya — API de Avaliações e Competências

**Global Solution 2026/2 — FIAP**
**Disciplina:** Advanced Business Development with .NET
**Tecnologia principal:** ASP.NET Core Minimal API (.NET 10)

## 1. Sobre o projeto

A Ventrya é uma plataforma de desenvolvimento profissional que busca apoiar pessoas com pouca ou nenhuma experiência no mercado de trabalho.

A proposta combina desafios práticos, avaliação de competências e preparação profissional.

O módulo desenvolvido em .NET é responsável pelo gerenciamento das avaliações realizadas nos desafios práticos.

## 2. Tecnologias utilizadas

- C# e ASP.NET Core Minimal API (.NET 10)
- Entity Framework Core e SQLite
- Autenticação JWT e autorização por perfil
- Rate Limiting
- Idempotência com persistência no banco
- OpenAPI e Swagger UI
- Health Checks
- xUnit e testes de integração

## 3. Funcionalidades implementadas

A API disponibiliza operações de cadastro, consulta, atualização e exclusão de avaliações.

Possui autenticação JWT com dois perfis de demonstração: **Avaliador** e **Participante**.

O Avaliador pode consultar e gerenciar avaliações. O Participante pode consultar avaliações, mas não pode cadastrá-las, atualizá-las ou excluí-las.

A aplicação também oferece proteção contra requisições duplicadas, limitação de chamadas, monitoramento de saúde e testes automatizados.

## 4. Endpoints

| Método | Rota | Funcionalidade |
|---|---|---|
| GET | `/` | Informações da API |
| POST | `/api/auth/login` | Autenticação JWT |
| GET | `/api/avaliacoes` | Listar avaliações |
| GET | `/api/avaliacoes/{id}` | Buscar avaliação |
| POST | `/api/avaliacoes` | Cadastrar avaliação |
| PUT | `/api/avaliacoes/{id}` | Atualizar avaliação |
| DELETE | `/api/avaliacoes/{id}` | Excluir avaliação |
| GET | `/health` | Verificar saúde da aplicação |
| GET | `/openapi/v1.json` | Documento OpenAPI em desenvolvimento |
| GET | `/swagger` | Swagger UI em desenvolvimento |

As rotas de avaliações exigem autenticação. POST, PUT e DELETE exigem o perfil Avaliador.

O POST também exige o cabeçalho `Idempotency-Key`.

## 5. Como executar

**Pré-requisitos:** .NET 10 SDK, Git e ferramenta `dotnet-ef` versão 10.

Clone o repositório e acesse sua pasta:

```bash
git clone URL_DO_REPOSITORIO
cd Ventrya-DotNet
```

Restaure as dependências:

```bash
dotnet restore Ventrya.slnx
```

Configure os segredos locais:

```bash
dotnet user-secrets init --project Ventrya.Api
```

No Windows PowerShell, gere uma chave JWT:

```powershell
$bytes = New-Object byte[] 48
$gerador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$gerador.GetBytes($bytes)
$gerador.Dispose()
$chave = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:Key" "$chave" --project Ventrya.Api
```

Configure as senhas de demonstração:

```powershell
$senhaAvaliador = Read-Host "Senha do avaliador"
dotnet user-secrets set "DemoAuth:Password" "$senhaAvaliador" --project Ventrya.Api

$senhaParticipante = Read-Host "Senha do participante"
dotnet user-secrets set "DemoAuth:ParticipantePassword" "$senhaParticipante" --project Ventrya.Api
```

As senhas e a chave JWT não devem ser publicadas no GitHub.

Crie ou atualize o banco de dados:

```bash
dotnet ef database update --project Ventrya.Api
```

Execute a aplicação:

```bash
dotnet run --project Ventrya.Api
```

A porta utilizada durante o desenvolvimento foi `5131`, podendo variar conforme a configuração local.

## 6. Testes automatizados

Execute:

```bash
dotnet test Ventrya.slnx
```

Na última execução validada, foram registrados **13 testes aprovados e nenhuma falha**.

Os testes contemplam validações de dados, autenticação, autorização e idempotência, incluindo testes de integração com SQLite em memória.

## 7. Segurança e confiabilidade

**JWT:** autentica usuários por meio de tokens com assinatura e prazo de validade.

**Autorização:** restringe operações de gerenciamento de avaliações ao perfil Avaliador.

**Idempotência:** identifica requisições POST repetidas pela chave `Idempotency-Key`, evitando duplicações no fluxo implementado.

**Rate Limiting:** limita as requisições às rotas de avaliações a cinco chamadas por janela de 30 segundos, respondendo com HTTP 429 quando o limite é ultrapassado.

**Health Checks:** disponibiliza `/health` para monitorar a aplicação e a conectividade com o banco.

**Validações:** verifica os dados recebidos nas requisições de cadastro e atualização.

## 8. Organização do projeto

```text
Ventrya-DotNet/
├── Ventrya.Api/
│   ├── Data/
│   ├── DTOs/
│   ├── Endpoints/
│   ├── Migrations/
│   ├── Models/
│   ├── Program.cs
│   └── Ventrya.Api.csproj
├── Ventrya.Tests/
│   ├── AvaliacaoRequestTests.cs
│   ├── IntegracaoApiTests.cs
│   └── Ventrya.Tests.csproj
├── Ventrya.slnx
└── README.md
```

## 9. Limitações do MVP acadêmico

A autenticação utiliza dois usuários de demonstração, configurados localmente, e não um cadastro completo de usuários.

Os identificadores dos desafios e participantes representam registros de outros módulos da plataforma e não possuem relacionamento implementado com tabelas locais nesta API.

O controle de concorrência da idempotência foi desenvolvido para uma instância da aplicação. Uma implantação distribuída exigiria mecanismos adicionais.

## Integrantes da equipe

**FIAP — Análise e Desenvolvimento de Sistemas**

| Integrante | RM |
|---|---|
| Marina Tamagnini Magalhães | 561786 |
| Felipe Maglio Filho | 563512 |
| João Pedro Bitencourt Goldoni | 564339 |
| Mateus Granja dos Santos | 564930 |
| Vitória Valentina Maglio | 563509 |

**Projeto:** Ventrya
**Entrega:** Global Solution 2026/2
**Disciplina:** Advanced Business Development with .NET

## 10. Objetivo da entrega

Demonstrar uma API RESTful funcional, organizada e testável, contemplando persistência, autenticação, autorização, idempotência, limitação de requisições, monitoramento e testes automatizados, conforme os requisitos da disciplina .NET da Global Solution 2026/2.