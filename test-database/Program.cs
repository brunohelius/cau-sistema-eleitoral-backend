using Npgsql;

// Simple database connection test
var connectionString = "Host=localhost;Database=sistema_eleitoral_dev;Username=brunosouza;Port=5432;";

Console.WriteLine("🧪 Teste de Conexão com Database");
Console.WriteLine("================================");

try
{
    // Test raw connection
    Console.WriteLine("1. Testando conexão direta...");
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Conexão PostgreSQL OK!");
    
    // Test query
    Console.WriteLine("\n2. Testando consulta básica...");
    using var command = new NpgsqlCommand("SELECT version();", connection);
    var version = await command.ExecuteScalarAsync();
    Console.WriteLine($"✅ PostgreSQL Version: {version}");
    
    // Test database structure
    Console.WriteLine("\n3. Testando estrutura do database...");
    var tableQuery = @"
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        ORDER BY table_name";
        
    using var tableCommand = new NpgsqlCommand(tableQuery, connection);
    using var reader = await tableCommand.ExecuteReaderAsync();
    
    var tables = new List<string>();
    while (await reader.ReadAsync())
    {
        tables.Add(reader.GetString(0));
    }
    
    Console.WriteLine($"✅ Total de tabelas: {tables.Count}");
    Console.WriteLine("📋 Tabelas encontradas:");
    foreach (var table in tables)
    {
        Console.WriteLine($"   - {table}");
    }
    
    // Test basic table data
    reader.Close();
    Console.WriteLine("\n4. Testando dados básicos...");
    
    var countQuery = "SELECT COUNT(*) FROM ufs";
    using var countCommand = new NpgsqlCommand(countQuery, connection);
    var ufCount = await countCommand.ExecuteScalarAsync();
    Console.WriteLine($"✅ UFs cadastrados: {ufCount}");
    
    var permissionQuery = "SELECT COUNT(*) FROM permissoes";
    using var permissionCommand = new NpgsqlCommand(permissionQuery, connection);
    var permissionCount = await permissionCommand.ExecuteScalarAsync();
    Console.WriteLine($"✅ Permissões cadastradas: {permissionCount}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro: {ex.Message}");
    Console.WriteLine($"Detalhes: {ex}");
    Environment.Exit(1);
}

Console.WriteLine("\n🎉 Teste de conexão concluído com sucesso!");
Console.WriteLine("\n📊 Próximos passos:");
Console.WriteLine("   1. Database funcionando perfeitamente ✅");
Console.WriteLine("   2. Estrutura básica criada ✅");  
Console.WriteLine("   3. Dados iniciais inseridos ✅");
Console.WriteLine("   4. Pronto para Entity Framework ✅");
Console.WriteLine("\n▶️  Pode prosseguir com a configuração da API!");