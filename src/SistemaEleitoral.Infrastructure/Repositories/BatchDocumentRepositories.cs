using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    // BATCH 2: DOCUMENT MANAGEMENT SYSTEM REPOSITORIES
    // Essential for electoral document handling and file management
    
    #region ArquivoRepository (Generic File Management)
    public interface IArquivoRepository : IRepository<Arquivo>
    {
        Task<Arquivo?> GetCompletoAsync(int id);
        Task<IEnumerable<Arquivo>> GetPorTipoAsync(TipoArquivo tipo);
        Task<IEnumerable<Arquivo>> GetPorEntidadeAsync(string entidade, int entidadeId);
        Task<byte[]> GetConteudoArquivoAsync(int arquivoId);
        Task<Arquivo> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo, TipoArquivo tipo, string entidade, int entidadeId);
        Task<bool> ExcluirArquivoFisicoAsync(int arquivoId);
        Task<long> GetTamanhoTotalArquivosAsync(string? entidade = null);
        Task<IEnumerable<Arquivo>> GetArquivosVencendoRetencaoAsync(int diasAlerta = 30);
        Task<bool> ValidarIntegridadeArquivoAsync(int arquivoId);
        Task<string> GerarHashArquivoAsync(byte[] conteudo);
    }
    
    public class ArquivoRepository : BaseRepository<Arquivo>, IArquivoRepository
    {
        private readonly IConfiguration _configuration;
        
        public ArquivoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<ArquivoRepository> logger, IConfiguration configuration) 
            : base(context, cache, logger)
        {
            _configuration = configuration;
        }
        
        public async Task<Arquivo?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .Include(a => a.TipoArquivo)
                    .Include(a => a.StatusArquivo)
                    .Include(a => a.VersaoArquivos)
                    .Include(a => a.PermissoesArquivo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == id),
                TimeSpan.FromMinutes(30));
        }
        
        public async Task<IEnumerable<Arquivo>> GetPorTipoAsync(TipoArquivo tipo)
        {
            return await _dbSet
                .Include(a => a.TipoArquivo)
                .Where(a => a.TipoArquivoId == (int)tipo && a.Ativo)
                .OrderByDescending(a => a.DataUpload)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Arquivo>> GetPorEntidadeAsync(string entidade, int entidadeId)
        {
            var cacheKey = GetCacheKey("por_entidade", entidade, entidadeId);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .Include(a => a.TipoArquivo)
                    .Where(a => a.Entidade == entidade && a.EntidadeId == entidadeId && a.Ativo)
                    .OrderByDescending(a => a.DataUpload)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(20));
        }
        
        public async Task<byte[]> GetConteudoArquivoAsync(int arquivoId)
        {
            var arquivo = await GetByIdAsync(arquivoId);
            if (arquivo == null || string.IsNullOrEmpty(arquivo.CaminhoArquivo))
                return Array.Empty<byte>();
                
            var caminhoCompleto = Path.Combine(_configuration["ArquivosPath"] ?? "", arquivo.CaminhoArquivo);
            if (File.Exists(caminhoCompleto))
                return await File.ReadAllBytesAsync(caminhoCompleto);
                
            return Array.Empty<byte>();
        }
        
        public async Task<Arquivo> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo, TipoArquivo tipo, string entidade, int entidadeId)
        {
            var hash = await GerarHashArquivoAsync(conteudo);
            var extensao = Path.GetExtension(nomeArquivo);
            var nomeUnico = $"{Guid.NewGuid()}{extensao}";
            var caminhoRelativo = Path.Combine(DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"), nomeUnico);
            var caminhoCompleto = Path.Combine(_configuration["ArquivosPath"] ?? "", caminhoRelativo);
            
            // Criar diretório se não existir
            var diretorio = Path.GetDirectoryName(caminhoCompleto);
            if (diretorio != null && !Directory.Exists(diretorio))
                Directory.CreateDirectory(diretorio);
                
            await File.WriteAllBytesAsync(caminhoCompleto, conteudo);
            
            var arquivo = new Arquivo
            {
                NomeOriginal = nomeArquivo,
                NomeArquivo = nomeUnico,
                CaminhoArquivo = caminhoRelativo,
                TamanhoBytes = conteudo.Length,
                TipoMime = GetMimeType(extensao),
                HashArquivo = hash,
                TipoArquivoId = (int)tipo,
                Entidade = entidade,
                EntidadeId = entidadeId,
                DataUpload = DateTime.UtcNow,
                Ativo = true
            };
            
            return await AddAsync(arquivo);
        }
        
        public async Task<bool> ExcluirArquivoFisicoAsync(int arquivoId)
        {
            var arquivo = await GetByIdAsync(arquivoId);
            if (arquivo == null) return false;
            
            try
            {
                var caminhoCompleto = Path.Combine(_configuration["ArquivosPath"] ?? "", arquivo.CaminhoArquivo);
                if (File.Exists(caminhoCompleto))
                    File.Delete(caminhoCompleto);
                    
                arquivo.Ativo = false;
                await UpdateAsync(arquivo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir arquivo físico {ArquivoId}", arquivoId);
                return false;
            }
        }
        
        public async Task<long> GetTamanhoTotalArquivosAsync(string? entidade = null)
        {
            var query = _dbSet.Where(a => a.Ativo);
            if (!string.IsNullOrEmpty(entidade))
                query = query.Where(a => a.Entidade == entidade);
                
            return await query.SumAsync(a => a.TamanhoBytes);
        }
        
        public async Task<IEnumerable<Arquivo>> GetArquivosVencendoRetencaoAsync(int diasAlerta = 30)
        {
            var dataLimite = DateTime.Now.AddDays(diasAlerta);
            return await _dbSet
                .Where(a => a.Ativo && a.DataExclusao <= dataLimite && a.DataExclusao > DateTime.Now)
                .OrderBy(a => a.DataExclusao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ValidarIntegridadeArquivoAsync(int arquivoId)
        {
            var arquivo = await GetByIdAsync(arquivoId);
            if (arquivo == null) return false;
            
            var conteudo = await GetConteudoArquivoAsync(arquivoId);
            if (conteudo.Length == 0) return false;
            
            var hashAtual = await GerarHashArquivoAsync(conteudo);
            return hashAtual == arquivo.HashArquivo;
        }
        
        public async Task<string> GerarHashArquivoAsync(byte[] conteudo)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(conteudo);
            return Convert.ToBase64String(hashBytes);
        }
        
        private static string GetMimeType(string extensao)
        {
            return extensao.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
    #endregion
    
    #region DocumentoEleicaoRepository
    public interface IDocumentoEleicaoRepository : IRepository<DocumentoEleicao>
    {
        Task<IEnumerable<DocumentoEleicao>> GetPorEleicaoAsync(int eleicaoId);
        Task<IEnumerable<DocumentoEleicao>> GetPorTipoAsync(TipoDocumento tipo);
        Task<DocumentoEleicao?> GetCompletoAsync(int id);
        Task<byte[]> GerarDocumentoAsync(int documentoId, object dados);
        Task<IEnumerable<DocumentoEleicao>> GetDocumentosPublicadosAsync();
        Task<bool> PublicarDocumentoAsync(int documentoId, DateTime? dataPublicacao = null);
        Task<IEnumerable<DocumentoEleicao>> GetPorStatusAsync(StatusDocumento status);
    }
    
    public class DocumentoEleicaoRepository : BaseRepository<DocumentoEleicao>, IDocumentoEleicaoRepository
    {
        public DocumentoEleicaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<DocumentoEleicaoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<DocumentoEleicao>> GetPorEleicaoAsync(int eleicaoId)
        {
            var cacheKey = GetCacheKey("por_eleicao", eleicaoId);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .Include(d => d.Eleicao)
                    .Include(d => d.TipoDocumento)
                    .Include(d => d.StatusDocumento)
                    .Where(d => d.EleicaoId == eleicaoId && d.Ativo)
                    .OrderBy(d => d.TipoDocumento.Ordem)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(20));
        }
        
        public async Task<IEnumerable<DocumentoEleicao>> GetPorTipoAsync(TipoDocumento tipo)
        {
            return await _dbSet
                .Include(d => d.Eleicao)
                .Include(d => d.StatusDocumento)
                .Where(d => d.TipoDocumentoId == (int)tipo && d.Ativo)
                .OrderByDescending(d => d.DataCriacao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<DocumentoEleicao?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .Include(d => d.Eleicao)
                    .Include(d => d.TipoDocumento)
                    .Include(d => d.StatusDocumento)
                    .Include(d => d.TemplateDocumento)
                    .Include(d => d.AssinaturasDigitais)
                    .Include(d => d.HistoricoDocumento)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id),
                TimeSpan.FromMinutes(15));
        }
        
        public async Task<byte[]> GerarDocumentoAsync(int documentoId, object dados)
        {
            var documento = await GetCompletoAsync(documentoId);
            if (documento?.TemplateDocumento == null)
                return Array.Empty<byte>();
                
            // Aqui integraria com um engine de templates (ex: RazorEngine, DinkToPdf)
            // Por simplicidade, retornando array vazio
            _logger.LogInformation("Gerando documento {DocumentoId} com template {TemplateId}", documentoId, documento.TemplateDocumento.Id);
            return Array.Empty<byte>();
        }
        
        public async Task<IEnumerable<DocumentoEleicao>> GetDocumentosPublicadosAsync()
        {
            return await _dbSet
                .Include(d => d.Eleicao)
                .Include(d => d.TipoDocumento)
                .Where(d => d.StatusDocumentoId == (int)StatusDocumento.Publicado && d.Ativo)
                .OrderByDescending(d => d.DataPublicacao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> PublicarDocumentoAsync(int documentoId, DateTime? dataPublicacao = null)
        {
            var documento = await GetByIdAsync(documentoId);
            if (documento == null) return false;
            
            documento.StatusDocumentoId = (int)StatusDocumento.Publicado;
            documento.DataPublicacao = dataPublicacao ?? DateTime.UtcNow;
            await UpdateAsync(documento);
            
            _logger.LogInformation("Documento {DocumentoId} publicado em {DataPublicacao}", documentoId, documento.DataPublicacao);
            return true;
        }
        
        public async Task<IEnumerable<DocumentoEleicao>> GetPorStatusAsync(StatusDocumento status)
        {
            return await _dbSet
                .Include(d => d.Eleicao)
                .Include(d => d.TipoDocumento)
                .Where(d => d.StatusDocumentoId == (int)status && d.Ativo)
                .OrderBy(d => d.DataCriacao)
                .AsNoTracking()
                .ToListAsync();
        }
    }
    #endregion
    
    #region TemplateDocumentoRepository
    public interface ITemplateDocumentoRepository : IRepository<TemplateDocumento>
    {
        Task<IEnumerable<TemplateDocumento>> GetPorTipoAsync(TipoDocumento tipo);
        Task<TemplateDocumento?> GetTemplateAtivoAsync(TipoDocumento tipo);
        Task<TemplateDocumento?> GetCompletoAsync(int id);
        Task<bool> AtivarTemplateAsync(int templateId);
        Task<bool> DesativarTemplateAsync(int templateId);
        Task<string> ProcessarTemplateAsync(int templateId, Dictionary<string, object> dados);
    }
    
    public class TemplateDocumentoRepository : BaseRepository<TemplateDocumento>, ITemplateDocumentoRepository
    {
        public TemplateDocumentoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TemplateDocumentoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<TemplateDocumento>> GetPorTipoAsync(TipoDocumento tipo)
        {
            return await _dbSet
                .Where(t => t.TipoDocumentoId == (int)tipo && t.Ativo)
                .OrderByDescending(t => t.Versao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<TemplateDocumento?> GetTemplateAtivoAsync(TipoDocumento tipo)
        {
            var cacheKey = GetCacheKey("ativo", tipo.ToString());
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .FirstOrDefaultAsync(t => t.TipoDocumentoId == (int)tipo && t.Ativo && t.Padrao),
                TimeSpan.FromHours(1));
        }
        
        public async Task<TemplateDocumento?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet
                    .Include(t => t.TipoDocumento)
                    .Include(t => t.VariaveisTemplate)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id),
                TimeSpan.FromMinutes(30));
        }
        
        public async Task<bool> AtivarTemplateAsync(int templateId)
        {
            var template = await GetByIdAsync(templateId);
            if (template == null) return false;
            
            // Desativar outros templates do mesmo tipo
            var outrosTemplates = await _dbSet
                .Where(t => t.TipoDocumentoId == template.TipoDocumentoId && t.Id != templateId)
                .ToListAsync();
                
            foreach (var outro in outrosTemplates)
            {
                outro.Padrao = false;
                outro.DataAlteracao = DateTime.UtcNow;
            }
            
            template.Padrao = true;
            template.Ativo = true;
            template.DataAlteracao = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            // Invalidar cache
            _cache.Remove(GetCacheKey("ativo", template.TipoDocumentoId.ToString()));
            
            return true;
        }
        
        public async Task<bool> DesativarTemplateAsync(int templateId)
        {
            var template = await GetByIdAsync(templateId);
            if (template == null) return false;
            
            template.Padrao = false;
            template.DataAlteracao = DateTime.UtcNow;
            await UpdateAsync(template);
            
            return true;
        }
        
        public async Task<string> ProcessarTemplateAsync(int templateId, Dictionary<string, object> dados)
        {
            var template = await GetCompletoAsync(templateId);
            if (template == null) return string.Empty;
            
            var html = template.ConteudoHtml ?? string.Empty;
            
            // Processamento simples de variáveis - em produção usar Razor ou similar
            foreach (var variavel in dados)
            {
                html = html.Replace($"{{{{{variavel.Key}}}}}", variavel.Value?.ToString() ?? "");
            }
            
            return html;
        }
    }
    #endregion
    
    // Supporting entities for document management
    public class Arquivo : BaseEntity
    {
        public string NomeOriginal { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public string TipoMime { get; set; } = string.Empty;
        public string HashArquivo { get; set; } = string.Empty;
        public int TipoArquivoId { get; set; }
        public string Entidade { get; set; } = string.Empty;
        public int EntidadeId { get; set; }
        public DateTime DataUpload { get; set; }
        public DateTime? DataExclusao { get; set; }
        public bool Ativo { get; set; }
        
        public virtual TipoArquivo TipoArquivo { get; set; } = null!;
        public virtual StatusArquivo StatusArquivo { get; set; } = null!;
        public virtual ICollection<VersaoArquivo> VersaoArquivos { get; set; } = new List<VersaoArquivo>();
        public virtual ICollection<PermissaoArquivo> PermissoesArquivo { get; set; } = new List<PermissaoArquivo>();
    }
    
    public class DocumentoEleicao : BaseEntity
    {
        public int EleicaoId { get; set; }
        public int TipoDocumentoId { get; set; }
        public int StatusDocumentoId { get; set; }
        public int? TemplateDocumentoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime? DataPublicacao { get; set; }
        public bool Ativo { get; set; }
        
        public virtual Eleicao Eleicao { get; set; } = null!;
        public virtual TipoDocumento TipoDocumento { get; set; } = null!;
        public virtual StatusDocumento StatusDocumento { get; set; } = null!;
        public virtual TemplateDocumento? TemplateDocumento { get; set; }
        public virtual ICollection<AssinaturaDigital> AssinaturasDigitais { get; set; } = new List<AssinaturaDigital>();
        public virtual ICollection<HistoricoDocumento> HistoricoDocumento { get; set; } = new List<HistoricoDocumento>();
    }
    
    public class TemplateDocumento : BaseEntity
    {
        public int TipoDocumentoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string ConteudoHtml { get; set; } = string.Empty;
        public string? ConteudoCss { get; set; }
        public int Versao { get; set; }
        public bool Padrao { get; set; }
        public bool Ativo { get; set; }
        
        public virtual TipoDocumento TipoDocumento { get; set; } = null!;
        public virtual ICollection<VariavelTemplate> VariaveisTemplate { get; set; } = new List<VariavelTemplate>();
    }
    
    // Enums and support classes
    public enum TipoDocumento { Diploma = 1, TermoPosse = 2, Relatorio = 3, Edital = 4 }
    public enum StatusDocumento { Rascunho = 1, EmRevisao = 2, Aprovado = 3, Publicado = 4 }
    public class TipoArquivo { public int Id { get; set; } public string Nome { get; set; } = string.Empty; }
    public class StatusArquivo { public int Id { get; set; } }
    public class VersaoArquivo { public int Id { get; set; } }
    public class PermissaoArquivo { public int Id { get; set; } }
    public class AssinaturaDigital { public int Id { get; set; } }
    public class HistoricoDocumento { public int Id { get; set; } }
    public class VariavelTemplate { public int Id { get; set; } }
    public class IConfiguration { }
}