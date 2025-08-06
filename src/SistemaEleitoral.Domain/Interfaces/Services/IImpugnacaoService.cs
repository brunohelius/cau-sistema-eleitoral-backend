using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Impugnações
    /// </summary>
    public interface IImpugnacaoService
    {
        // Registro de Impugnações
        Task<PedidoImpugnacaoDTO> RegistrarPedidoImpugnacaoAsync(RegistrarImpugnacaoDTO dto);
        
        // Defesa
        Task<bool> RegistrarDefesaImpugnacaoAsync(int pedidoId, RegistrarDefesaImpugnacaoDTO dto);
        
        // Julgamento
        Task<bool> JulgarImpugnacaoAsync(int pedidoId, JulgarImpugnacaoDTO dto);
        
        // Recursos
        Task<bool> RegistrarRecursoImpugnacaoAsync(int pedidoId, RegistrarRecursoImpugnacaoDTO dto);
        Task<bool> JulgarRecursoImpugnacaoAsync(int recursoId, JulgarRecursoDTO dto);
        
        // Consultas
        Task<PedidoImpugnacaoDTO> ObterPedidoPorIdAsync(int id);
        Task<PedidoImpugnacaoDTO> ObterPedidoPorProtocoloAsync(string protocolo);
        Task<List<PedidoImpugnacaoDTO>> ObterPedidosPorChapaAsync(int chapaId);
        Task<List<PedidoImpugnacaoDTO>> ObterPedidosPorCalendarioAsync(int calendarioId);
        Task<List<PedidoImpugnacaoDTO>> ObterPedidosPorSolicitanteAsync(int solicitanteId);
        Task<List<QuantidadeImpugnacaoPorUfDTO>> ObterQuantidadePorUfAsync(int calendarioId);
        
        // Estatísticas
        Task<EstatisticasImpugnacaoDTO> ObterEstatisticasAsync(int calendarioId);
        
        // Documentos
        Task<bool> AnexarDocumentoAsync(int pedidoId, AnexarDocumentoImpugnacaoDTO dto);
        Task<bool> RemoverDocumentoAsync(int pedidoId, int documentoId);
    }
    
    /// <summary>
    /// DTO para pedido de impugnação
    /// </summary>
    public class PedidoImpugnacaoDTO
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public int CalendarioId { get; set; }
        public int ChapaId { get; set; }
        public string NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
        public int? MembroChapaId { get; set; }
        public string NomeMembroImpugnado { get; set; }
        public int SolicitanteId { get; set; }
        public string NomeSolicitante { get; set; }
        public string TipoImpugnacao { get; set; }
        public string Fundamentacao { get; set; }
        public string Status { get; set; }
        public DateTime DataSolicitacao { get; set; }
        public bool Urgente { get; set; }
        public bool Sigiloso { get; set; }
        
        // Defesa
        public bool TemDefesa { get; set; }
        public DateTime? DataDefesa { get; set; }
        
        // Julgamento
        public bool FoiJulgado { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public string Decisao { get; set; }
        
        // Recursos
        public int QuantidadeRecursos { get; set; }
        
        // Arquivos
        public int QuantidadeArquivos { get; set; }
        
        // Histórico
        public List<HistoricoImpugnacaoDTO> Historico { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar impugnação
    /// </summary>
    public class RegistrarImpugnacaoDTO
    {
        public int CalendarioId { get; set; }
        public int ChapaId { get; set; }
        public int? MembroChapaId { get; set; }
        public int SolicitanteId { get; set; }
        public TipoImpugnacao TipoImpugnacao { get; set; }
        public string Fundamentacao { get; set; }
        public bool Urgente { get; set; }
        public bool Sigiloso { get; set; }
        public List<DocumentoImpugnacaoDTO> DocumentosComprobatorios { get; set; }
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar defesa de impugnação
    /// </summary>
    public class RegistrarDefesaImpugnacaoDTO
    {
        public string Argumentacao { get; set; }
        public int ApresentadaPorId { get; set; }
        public bool ForaDoPrazo { get; set; }
        public List<DocumentoImpugnacaoDTO> DocumentosDefesa { get; set; }
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para julgar impugnação
    /// </summary>
    public class JulgarImpugnacaoDTO
    {
        public DecisaoJulgamento Decisao { get; set; }
        public string Fundamentacao { get; set; }
        public int RelatorId { get; set; }
        public int? VotosFavoraveis { get; set; }
        public int? VotosContrarios { get; set; }
        public int? Abstencoes { get; set; }
        public List<DocumentoImpugnacaoDTO> DocumentosJulgamento { get; set; }
        public int UsuarioJulgamentoId { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar recurso de impugnação
    /// </summary>
    public class RegistrarRecursoImpugnacaoDTO
    {
        public int RecorrenteId { get; set; }
        public TipoRecorrente TipoRecorrente { get; set; }
        public string Fundamentacao { get; set; }
        public bool ForaDoPrazo { get; set; }
        public List<DocumentoImpugnacaoDTO> DocumentosRecurso { get; set; }
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para julgar recurso
    /// </summary>
    public class JulgarRecursoDTO
    {
        public DecisaoJulgamento Decisao { get; set; }
        public string Fundamentacao { get; set; }
        public int RelatorId { get; set; }
        public List<DocumentoImpugnacaoDTO> DocumentosJulgamento { get; set; }
        public int UsuarioJulgamentoId { get; set; }
    }
    
    /// <summary>
    /// DTO para documento de impugnação
    /// </summary>
    public class DocumentoImpugnacaoDTO
    {
        public string NomeArquivo { get; set; }
        public string TipoArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
    }
    
    /// <summary>
    /// DTO para histórico de impugnação
    /// </summary>
    public class HistoricoImpugnacaoDTO
    {
        public string Status { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAlteracao { get; set; }
    }
    
    /// <summary>
    /// DTO para quantidade de impugnações por UF
    /// </summary>
    public class QuantidadeImpugnacaoPorUfDTO
    {
        public int UfId { get; set; }
        public string UfNome { get; set; }
        public string UfSigla { get; set; }
        public int Total { get; set; }
        public int EmAnalise { get; set; }
        public int Deferidos { get; set; }
        public int Indeferidos { get; set; }
        public int EmRecurso { get; set; }
    }
    
    /// <summary>
    /// DTO para estatísticas de impugnações
    /// </summary>
    public class EstatisticasImpugnacaoDTO
    {
        public int TotalPedidos { get; set; }
        public int EmAnalise { get; set; }
        public int ComDefesa { get; set; }
        public int Deferidos { get; set; }
        public int Indeferidos { get; set; }
        public int EmRecurso { get; set; }
        public int RecursosJulgados { get; set; }
        public Dictionary<string, int> PorTipoImpugnacao { get; set; }
        public Dictionary<string, int> PorUF { get; set; }
        public double PercentualDeferimento { get; set; }
        public double TempoMedioJulgamento { get; set; }
    }
    
    /// <summary>
    /// DTO para anexar documento
    /// </summary>
    public class AnexarDocumentoImpugnacaoDTO
    {
        public string TipoDocumento { get; set; }
        public string NomeArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
        public int UsuarioUploadId { get; set; }
    }
    
    /// <summary>
    /// Enumeração de tipos de impugnação
    /// </summary>
    public enum TipoImpugnacao
    {
        Inelegibilidade = 1,
        DocumentacaoIrregular = 2,
        ViolacaoNormas = 3,
        FalsidadeIdeologica = 4,
        ConflitosInteresse = 5,
        ComposicaoIrregular = 6,
        DiversidadeInsuficiente = 7,
        Outro = 99
    }
    
    /// <summary>
    /// Enumeração de status de pedido de impugnação
    /// </summary>
    public enum StatusPedidoImpugnacao
    {
        EmAnalise = 1,
        ComDefesa = 2,
        EmJulgamento = 3,
        Deferido = 4,
        Indeferido = 5,
        EmRecurso = 6,
        RecursoJulgado = 7,
        Arquivado = 8,
        Cancelado = 9
    }
    
    /// <summary>
    /// Enumeração de decisões de julgamento
    /// </summary>
    public enum DecisaoJulgamento
    {
        Procedente = 1,
        Improcedente = 2,
        ParcialmenteProcedente = 3
    }
    
    /// <summary>
    /// Enumeração de tipos de recorrente
    /// </summary>
    public enum TipoRecorrente
    {
        Solicitante = 1,
        Chapa = 2,
        MembroChapa = 3,
        Terceiro = 4
    }
    
    /// <summary>
    /// Enumeração de status de recurso
    /// </summary>
    public enum StatusRecurso
    {
        EmAnalise = 1,
        ComContrarrazoes = 2,
        EmJulgamento = 3,
        Provido = 4,
        Improvido = 5,
        ParcialmenteProvido = 6,
        NaoConhecido = 7
    }
    
    /// <summary>
    /// Enumeração de status de membro impugnado
    /// </summary>
    public enum StatusMembroChapa
    {
        ConvitePendente = 1,
        Confirmado = 2,
        Recusado = 3,
        Removido = 4,
        Substituido = 5,
        Impugnado = 6
    }
}