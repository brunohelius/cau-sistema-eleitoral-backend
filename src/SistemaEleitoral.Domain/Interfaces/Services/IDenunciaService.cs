using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Denúncias
    /// </summary>
    public interface IDenunciaService
    {
        // Registro de Denúncias
        Task<DenunciaDTO> RegistrarDenunciaAsync(RegistrarDenunciaDTO dto);
        
        // Encaminhamento e Análise
        Task<bool> EncaminharParaRelatorAsync(int denunciaId, EncaminharDenunciaDTO dto);
        Task<bool> AdmitirInadmitirDenunciaAsync(int denunciaId, AdmitirDenunciaDTO dto);
        
        // Defesa e Contraditório
        Task<bool> RegistrarDefesaAsync(int denunciaId, RegistrarDefesaDTO dto);
        Task<bool> RegistrarProvasAsync(int denunciaId, RegistrarProvasDTO dto);
        
        // Consultas
        Task<DenunciaDTO> ObterDenunciaPorIdAsync(int id);
        Task<DenunciaDTO> ObterDenunciaPorProtocoloAsync(string protocolo);
        Task<ListaDenunciasPaginadaDTO> ListarDenunciasAsync(FiltroDenunciasDTO filtro);
        Task<List<DenunciaDTO>> ObterDenunciasPorChapaAsync(int chapaId);
        Task<List<DenunciaDTO>> ObterDenunciasPorMembroAsync(int membroId);
        Task<List<DenunciaDTO>> ObterDenunciasPorRelatorAsync(int relatorId);
        
        // Estatísticas
        Task<EstatisticasDenunciasDTO> ObterEstatisticasAsync(int calendarioId);
        Task<AcompanhamentoDenunciaDTO> ObterAcompanhamentoAsync(string protocolo);
        
        // Impedimentos e Suspeições
        Task<bool> RegistrarImpedimentoSuspeicaoAsync(int denunciaId, ImpedimentoSuspeicaoDTO dto);
        
        // Documentos
        Task<bool> AnexarDocumentoAsync(int denunciaId, AnexarDocumentoDenunciaDTO dto);
        Task<bool> RemoverDocumentoAsync(int denunciaId, int documentoId);
    }
    
    /// <summary>
    /// DTO para informações da denúncia
    /// </summary>
    public class DenunciaDTO
    {
        public int Id { get; set; }
        public string ProtocoloNumero { get; set; }
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public string UfNome { get; set; }
        public string TipoDenuncia { get; set; }
        public string Situacao { get; set; }
        public string Descricao { get; set; }
        public DateTime? DataOcorrencia { get; set; }
        public string LocalOcorrencia { get; set; }
        public DateTime DataRegistro { get; set; }
        
        // Denunciante
        public int? DenuncianteId { get; set; }
        public string DenuncianteNome { get; set; }
        public bool DenuncianteAnonimo { get; set; }
        
        // Flags
        public bool Urgente { get; set; }
        public bool Sigilosa { get; set; }
        public bool ForaDoPrazo { get; set; }
        
        // Denunciados
        public DenunciadoChapaDTO DenunciadoChapa { get; set; }
        public DenunciadoMembroChapaDTO DenunciadoMembroChapa { get; set; }
        public DenunciadoMembroComissaoEleitoralDTO DenunciadoMembroComissaoEleitoral { get; set; }
        public DenunciadoOutroDTO DenunciadoOutro { get; set; }
        
        // Listas
        public List<TestemunhaDenunciaDTO> Testemunhas { get; set; }
        public List<ArquivoDenunciaDTO> Arquivos { get; set; }
        
        // Encaminhamento
        public EncaminhamentoDenunciaDTO Encaminhamento { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar denúncia
    /// </summary>
    public class RegistrarDenunciaDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public TipoDenuncia TipoDenuncia { get; set; }
        public string Descricao { get; set; }
        public DateTime? DataOcorrencia { get; set; }
        public string LocalOcorrencia { get; set; }
        
        // Denunciante
        public int? DenuncianteId { get; set; }
        public bool DenuncianteAnonimo { get; set; }
        
        // Denunciado
        public TipoDenunciado TipoDenunciado { get; set; }
        public int? ChapaId { get; set; }
        public int? MembroChapaId { get; set; }
        public int? MembroComissaoEleitoralId { get; set; }
        public string DenunciadoOutroNome { get; set; }
        public string DenunciadoOutroCpf { get; set; }
        public string DenunciadoOutroDescricao { get; set; }
        
        // Flags
        public bool Urgente { get; set; }
        public bool Sigilosa { get; set; }
        public bool ForaDoProzo { get; set; }
        
        // Listas
        public List<TestemunhaDenunciaDTO> Testemunhas { get; set; }
        public List<AnexarArquivoDTO> Arquivos { get; set; }
        
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para encaminhar denúncia
    /// </summary>
    public class EncaminharDenunciaDTO
    {
        public int RelatorId { get; set; }
        public string Observacao { get; set; }
        public DateTime? PrazoAnalise { get; set; }
        public PrioridadeDenuncia Prioridade { get; set; }
        public int UsuarioEncaminhamentoId { get; set; }
    }
    
    /// <summary>
    /// DTO para admitir/inadmitir denúncia
    /// </summary>
    public class AdmitirDenunciaDTO
    {
        public bool Admitir { get; set; }
        public string Parecer { get; set; }
        public string MotivoInadmissao { get; set; }
        public DateTime? PrazoDefesa { get; set; }
        public List<AnexarArquivoDTO> Arquivos { get; set; }
        public int UsuarioId { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar defesa
    /// </summary>
    public class RegistrarDefesaDTO
    {
        public string Argumentacao { get; set; }
        public int ApresentadaPorId { get; set; }
        public bool ForaDoPrazo { get; set; }
        public List<AnexarArquivoDTO> Arquivos { get; set; }
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar provas
    /// </summary>
    public class RegistrarProvasDTO
    {
        public string Descricao { get; set; }
        public List<AnexarArquivoDTO> Arquivos { get; set; }
        public int UsuarioRegistroId { get; set; }
    }
    
    /// <summary>
    /// DTO para denunciado chapa
    /// </summary>
    public class DenunciadoChapaDTO
    {
        public int ChapaId { get; set; }
        public string NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
    }
    
    /// <summary>
    /// DTO para denunciado membro de chapa
    /// </summary>
    public class DenunciadoMembroChapaDTO
    {
        public int MembroChapaId { get; set; }
        public string NomeMembro { get; set; }
        public string CpfMembro { get; set; }
    }
    
    /// <summary>
    /// DTO para denunciado membro de comissão
    /// </summary>
    public class DenunciadoMembroComissaoEleitoralDTO
    {
        public int MembroComissaoEleitoralId { get; set; }
        public string NomeMembro { get; set; }
        public string CpfMembro { get; set; }
    }
    
    /// <summary>
    /// DTO para denunciado outro
    /// </summary>
    public class DenunciadoOutroDTO
    {
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Descricao { get; set; }
    }
    
    /// <summary>
    /// DTO para testemunha de denúncia
    /// </summary>
    public class TestemunhaDenunciaDTO
    {
        public int? Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Depoimento { get; set; }
    }
    
    /// <summary>
    /// DTO para arquivo de denúncia
    /// </summary>
    public class ArquivoDenunciaDTO
    {
        public int Id { get; set; }
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public string TipoArquivo { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime DataUpload { get; set; }
    }
    
    /// <summary>
    /// DTO para encaminhamento de denúncia
    /// </summary>
    public class EncaminhamentoDenunciaDTO
    {
        public int RelatorId { get; set; }
        public string NomeRelator { get; set; }
        public DateTime DataEncaminhamento { get; set; }
        public DateTime PrazoAnalise { get; set; }
        public PrioridadeDenuncia Prioridade { get; set; }
        public string ObservacaoEncaminhamento { get; set; }
    }
    
    /// <summary>
    /// DTO para filtro de denúncias
    /// </summary>
    public class FiltroDenunciasDTO
    {
        public int? CalendarioId { get; set; }
        public int? UfId { get; set; }
        public string Situacao { get; set; }
        public string TipoDenuncia { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int? RelatorId { get; set; }
        public bool? Urgente { get; set; }
        public bool? Sigilosa { get; set; }
        public string TextoBusca { get; set; }
        public int Pagina { get; set; } = 1;
        public int ItensPorPagina { get; set; } = 20;
    }
    
    /// <summary>
    /// DTO para lista paginada de denúncias
    /// </summary>
    public class ListaDenunciasPaginadaDTO
    {
        public List<DenunciaDTO> Denuncias { get; set; }
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItens { get; set; }
        public int ItensPorPagina { get; set; }
    }
    
    /// <summary>
    /// DTO para estatísticas de denúncias
    /// </summary>
    public class EstatisticasDenunciasDTO
    {
        public int TotalDenuncias { get; set; }
        public int DenunciasRegistradas { get; set; }
        public int DenunciasEmAnalise { get; set; }
        public int DenunciasEmRelatoria { get; set; }
        public int DenunciasAdmitidas { get; set; }
        public int DenunciasInadmitidas { get; set; }
        public int DenunciasComDefesa { get; set; }
        public int DenunciasJulgadas { get; set; }
        public Dictionary<string, int> DenunciasPorTipo { get; set; }
        public Dictionary<string, int> DenunciasPorUF { get; set; }
    }
    
    /// <summary>
    /// DTO para acompanhamento de denúncia
    /// </summary>
    public class AcompanhamentoDenunciaDTO
    {
        public string ProtocoloNumero { get; set; }
        public string SituacaoAtual { get; set; }
        public DateTime DataRegistro { get; set; }
        public List<HistoricoSituacaoDTO> HistoricoSituacoes { get; set; }
        public string ProximaEtapa { get; set; }
        public DateTime? PrazoProximaEtapa { get; set; }
    }
    
    /// <summary>
    /// DTO para histórico de situação
    /// </summary>
    public class HistoricoSituacaoDTO
    {
        public string Situacao { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Observacao { get; set; }
    }
    
    /// <summary>
    /// DTO para impedimento/suspeição
    /// </summary>
    public class ImpedimentoSuspeicaoDTO
    {
        public TipoImpedimento Tipo { get; set; }
        public string Motivo { get; set; }
        public int SolicitanteId { get; set; }
        public int MembroComissaoEleitoralId { get; set; }
    }
    
    /// <summary>
    /// DTO para anexar documento à denúncia
    /// </summary>
    public class AnexarDocumentoDenunciaDTO
    {
        public string TipoDocumento { get; set; }
        public string NomeArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
        public int UsuarioUploadId { get; set; }
    }
    
    /// <summary>
    /// DTO genérico para anexar arquivo
    /// </summary>
    public class AnexarArquivoDTO
    {
        public string NomeArquivo { get; set; }
        public string TipoArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
    }
    
    /// <summary>
    /// Enumeração de tipos de denúncia
    /// </summary>
    public enum TipoDenuncia
    {
        IrregularidadeCampanha = 1,
        CompraVotos = 2,
        UsoMaquinaPublica = 3,
        FalsidadeIdeologica = 4,
        ViolacaoNormas = 5,
        CondutaAntiEtica = 6,
        ConflitosInteresse = 7,
        Outro = 99
    }
    
    /// <summary>
    /// Enumeração de situações de denúncia
    /// </summary>
    public enum SituacaoDenuncia
    {
        Registrada = 1,
        EmAnalise = 2,
        EmRelatoria = 3,
        Admitida = 4,
        Inadmitida = 5,
        ComDefesa = 6,
        EmInstrucao = 7,
        EmJulgamento = 8,
        Julgada = 9,
        EmRecurso = 10,
        Arquivada = 11,
        Cancelada = 12
    }
    
    /// <summary>
    /// Enumeração de tipos de denunciado
    /// </summary>
    public enum TipoDenunciado
    {
        Chapa = 1,
        MembroChapa = 2,
        MembroComissaoEleitoral = 3,
        Outro = 4
    }
    
    /// <summary>
    /// Enumeração de prioridades de denúncia
    /// </summary>
    public enum PrioridadeDenuncia
    {
        Baixa = 1,
        Media = 2,
        Alta = 3,
        Urgente = 4
    }
    
    /// <summary>
    /// Enumeração de tipos de impedimento
    /// </summary>
    public enum TipoImpedimento
    {
        Impedimento = 1,
        Suspeicao = 2
    }
}