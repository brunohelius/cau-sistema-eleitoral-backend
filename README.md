# 🗳️ CAU Sistema Eleitoral - Backend

API REST desenvolvida em .NET 8 com Clean Architecture para o Sistema Eleitoral do CAU (Conselho de Arquitetura e Urbanismo).

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**:

```
SistemaEleitoral/
├── src/
│   ├── SistemaEleitoral.Api/           # Camada de Apresentação (Controllers, Middleware)
│   ├── SistemaEleitoral.Application/   # Casos de Uso e Lógica de Aplicação
│   ├── SistemaEleitoral.Domain/        # Entidades e Regras de Negócio
│   └── SistemaEleitoral.Infrastructure/ # Implementações (DB, Email, etc)
├── tests/
│   ├── SistemaEleitoral.UnitTests/
│   └── SistemaEleitoral.IntegrationTests/
└── docs/
```

## ✨ Funcionalidades

### Módulos Implementados
- ✅ **Autenticação e Autorização** - JWT com refresh tokens
- ✅ **Gestão de Eleições** - CRUD completo com workflow
- ✅ **Sistema de Chapas** - Registro e validação
- ✅ **Votação Online** - Com auditoria completa
- ✅ **Apuração** - Em tempo real com WebSockets
- ✅ **Denúncias** - Sistema completo com workflow
- ✅ **Recursos** - Gestão de recursos eleitorais
- ✅ **Comunicação** - Email e notificações
- ✅ **Relatórios** - Geração de PDFs e Excel
- ✅ **Auditoria** - Log completo de todas as ações

## 🛠️ Tecnologias

- **.NET 8** - Framework principal
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados principal
- **Redis** - Cache e sessões
- **SignalR** - WebSocket para real-time
- **JWT** - Autenticação
- **FluentValidation** - Validações
- **AutoMapper** - Mapeamento de objetos
- **MediatR** - Padrão mediator para CQRS
- **Serilog** - Logging estruturado
- **Docker** - Containerização

## 🚀 Instalação

### Pré-requisitos
- .NET 8 SDK
- PostgreSQL 14+
- Redis (opcional)
- Docker (opcional)

### Setup Local

1. Clone o repositório:
```bash
git clone https://github.com/brunozexter/cau-sistema-eleitoral-backend.git
cd cau-sistema-eleitoral-backend
```

2. Configure o banco de dados:
```bash
# Crie o banco de dados
createdb sistema_eleitoral

# Configure a connection string em appsettings.json
```

3. Execute as migrations:
```bash
cd src/SistemaEleitoral.Api
dotnet ef database update
```

4. Execute o projeto:
```bash
dotnet run
```

A API estará disponível em: https://localhost:5001

### Setup com Docker

```bash
docker-compose up -d
```

## 📚 Documentação da API

A documentação Swagger está disponível em: https://localhost:5001/swagger

### Principais Endpoints

#### Autenticação
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/logout` - Logout

#### Eleições
- `GET /api/eleicoes` - Listar eleições
- `POST /api/eleicoes` - Criar eleição
- `PUT /api/eleicoes/{id}` - Atualizar eleição
- `DELETE /api/eleicoes/{id}` - Excluir eleição

#### Votação
- `POST /api/votacao/votar` - Registrar voto
- `GET /api/votacao/comprovante/{id}` - Obter comprovante

#### Apuração
- `GET /api/apuracao/{eleicaoId}` - Resultado parcial
- `GET /api/apuracao/{eleicaoId}/final` - Resultado final

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Com coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📊 Métricas e Monitoramento

- Health Check: `/health`
- Métricas: `/metrics`
- Logs estruturados com Serilog

## 🔒 Segurança

- Autenticação JWT com refresh tokens
- Rate limiting
- CORS configurado
- SQL Injection prevention
- XSS protection
- HTTPS enforced
- Auditoria completa

## 🤝 Contribuindo

1. Fork o projeto
2. Crie sua feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 Licença

Este projeto está sob licença proprietária do CAU.

## 👨‍💻 Autor

**Bruno Souza**
- GitHub: [@brunozexter](https://github.com/brunozexter)

## 🙏 Agradecimentos

- Equipe CAU pelo suporte
- Comunidade .NET
- Contributors do projeto