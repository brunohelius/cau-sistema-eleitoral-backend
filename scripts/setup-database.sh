#!/bin/bash

# Setup do Database para Sistema Eleitoral CAU
# Requisitos: PostgreSQL instalado e rodando

set -e

echo "🚀 Configurando Database Sistema Eleitoral CAU..."

# Configurações do banco
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_USER=${DB_USER:-postgres}
DB_PASSWORD=${DB_PASSWORD:-postgres}
DB_NAME_DEV=${DB_NAME_DEV:-sistema_eleitoral_dev}
DB_NAME_PROD=${DB_NAME_PROD:-sistema_eleitoral}

# Função para executar SQL
execute_sql() {
    local database=$1
    local sql_file=$2
    echo "📝 Executando $sql_file no database $database..."
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $database -f $sql_file
}

# Função para criar database
create_database() {
    local db_name=$1
    echo "🗃️  Criando database $db_name..."
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c "CREATE DATABASE $db_name;" postgres || true
}

# Verificar se PostgreSQL está rodando
echo "🔍 Verificando conexão com PostgreSQL..."
if ! PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c '\l' postgres > /dev/null 2>&1; then
    echo "❌ Erro: Não foi possível conectar ao PostgreSQL"
    echo "   Verifique se o PostgreSQL está rodando e as credenciais estão corretas"
    echo "   Host: $DB_HOST, Port: $DB_PORT, User: $DB_USER"
    exit 1
fi

echo "✅ Conexão com PostgreSQL OK!"

# Criar databases
create_database $DB_NAME_DEV
create_database $DB_NAME_PROD

# Executar migration inicial no database de desenvolvimento
MIGRATION_FILE="../src/SistemaEleitoral.Infrastructure/Migrations/20250806_InitialCreate.sql"
if [ -f "$MIGRATION_FILE" ]; then
    execute_sql $DB_NAME_DEV $MIGRATION_FILE
    echo "✅ Migration inicial executada no database de desenvolvimento!"
else
    echo "⚠️  Arquivo de migration não encontrado: $MIGRATION_FILE"
fi

# Testar conexões
echo "🧪 Testando conexões com os databases..."

echo "📊 Testando $DB_NAME_DEV..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME_DEV -c "SELECT COUNT(*) as total_tables FROM information_schema.tables WHERE table_schema = 'public';"

echo "📊 Testando $DB_NAME_PROD..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME_PROD -c "SELECT version();" > /dev/null 2>&1

echo ""
echo "🎉 Setup do database concluído com sucesso!"
echo ""
echo "📋 Informações dos databases:"
echo "   • Desenvolvimento: $DB_NAME_DEV"
echo "   • Produção: $DB_NAME_PROD"
echo "   • Host: $DB_HOST:$DB_PORT"
echo "   • Usuário: $DB_USER"
echo ""
echo "🔗 Connection Strings:"
echo "   • Dev:  Host=$DB_HOST;Database=$DB_NAME_DEV;Username=$DB_USER;Password=$DB_PASSWORD;Port=$DB_PORT;"
echo "   • Prod: Host=$DB_HOST;Database=$DB_NAME_PROD;Username=$DB_USER;Password=$DB_PASSWORD;Port=$DB_PORT;"
echo ""
echo "▶️  Para testar a API:"
echo "   cd ../src/SistemaEleitoral.Api"
echo "   dotnet run"