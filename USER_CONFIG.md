# Configuração de Usuário - Sistema Eleitoral CAU

## 👤 Usuário Administrador Padrão

### Informações do Usuário
- **Username**: `brunohelius`
- **Email**: `brunohelius@gmail.com`
- **Role**: `Administrator`
- **Status**: `Ativo`
- **Data de Criação**: Sistema migrado
- **Último Acesso**: Sistema em desenvolvimento

### Permissões
- ✅ Administração completa do sistema
- ✅ Gestão de eleições
- ✅ Aprovação de chapas
- ✅ Sistema judicial (denúncias/impugnações)
- ✅ Configurações do sistema
- ✅ Relatórios e auditoria

### Configurações JWT
```json
{
  "sub": "brunohelius",
  "email": "brunohelius@gmail.com",
  "role": "Administrator",
  "permissions": ["admin", "election_manager", "judge", "system_config"]
}
```

### Dados para Seed/Migration
```csharp
// Para uso em DbContext Seed
new Usuario
{
    UserName = "brunohelius",
    Email = "brunohelius@gmail.com",
    EmailConfirmed = true,
    Nome = "Bruno Helius",
    Ativo = true,
    IsAdmin = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
}
```

## 🔐 Configurações de Segurança

### JWT Token Settings
- **Issuer**: `SistemaEleitoralCAU`
- **Audience**: `CAU-Users`
- **Expiration**: `24 hours`
- **Secret**: Configurar em appsettings.json

### Políticas de Autorização
```csharp
// Políticas definidas no sistema
"Administrator" => Acesso total
"ElectionManager" => Gestão de eleições
"Judge" => Sistema judicial
"User" => Acesso básico
```

---
*Documentação atualizada automaticamente*  
*Projeto: Sistema Eleitoral CAU v2.0*