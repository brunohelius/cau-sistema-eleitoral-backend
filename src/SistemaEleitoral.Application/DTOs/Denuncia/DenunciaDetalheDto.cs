using SistemaEleitoral.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Application.DTOs.Denuncia
{
    /// <summary>
    /// DTO para detalhes completos da denúncia
    /// </summary>
    public class DenunciaDetalheDto
    {
        public int Id { get; set; }
        public int NumeroSequencial { get; set; }
        public string Protocolo { get; set; }
        public DateTime DataHoraDenuncia { get; set; }
        public StatusDenuncia Status { get; set; }
        public string StatusDescricao => Status.ToString();
        public string DescricaoFatos { get; set; }
        public bool TemSigilo { get; set; }

        // Dados do Denunciante
        public DenuncianteDto Denunciante { get; set; }

        // Tipo de Denúncia
        public TipoDenunciaDto TipoDenuncia { get; set; }

        // Filial
        public FilialDto Filial { get; set; }

        // Workflow de Admissibilidade
        public DateTime? DataAnaliseAdmissibilidade { get; set; }
        public bool? Admissivel { get; set; }
        public string MotivoInadmissibilidade { get; set; }

        // Workflow de Defesa
        public DateTime? DataNotificacaoDefesa { get; set; }
        public DateTime? PrazoDefesa { get; set; }
        public DateTime? DataRecebimentoDefesa { get; set; }
        public string DefesaTexto { get; set; }

        // Workflow de Produção de Provas
        public DateTime? PrazoProducaoProvas { get; set; }
        public DateTime? DataEncerramentoProvas { get; set; }

        // Workflow de Audiência
        public DateTime? DataAudienciaInstrucao { get; set; }
        public string ResumoAudiencia { get; set; }
        public bool? AudienciaRealizada { get; set; }

        // Workflow de Alegações Finais
        public DateTime? PrazoAlegacoesFinais { get; set; }
        public string AlegacoesFinaisTexto { get; set; }
        public DateTime? DataRecebimentoAlegacoes { get; set; }

        // Workflow de Julgamento - 1ª Instância
        public DateTime? DataJulgamentoPrimeiraInstancia { get; set; }
        public string DecisaoPrimeiraInstancia { get; set; }
        public bool CabeRecurso { get; set; }
        public DateTime? PrazoRecurso { get; set; }

        // Workflow de Recurso - 2ª Instância
        public DateTime? DataInterposicaoRecurso { get; set; }
        public string FundamentacaoRecurso { get; set; }
        public DateTime? DataJulgamentoRecurso { get; set; }
        public string DecisaoRecurso { get; set; }

        // Controle de Relator
        public RelatorDto Relator { get; set; }
        public DateTime? DataDesignacaoRelator { get; set; }

        // Controle de Arquivamento
        public DateTime? DataArquivamento { get; set; }
        public string MotivoArquivamento { get; set; }

        // Relacionamentos específicos
        public DenunciaChapaDto DenunciaChapa { get; set; }
        public DenunciaMembroChapaDto DenunciaMembroChapa { get; set; }
        public DenunciaMembroComissaoDto DenunciaMembroComissao { get; set; }
        public DenunciaOutroDto DenunciaOutro { get; set; }

        // Coleções
        public List<ArquivoDenunciaDto> Arquivos { get; set; } = new List<ArquivoDenunciaDto>();
        public List<TestemunhaDenunciaDto> Testemunhas { get; set; } = new List<TestemunhaDenunciaDto>();
        public List<JulgamentoDenunciaDto> Julgamentos { get; set; } = new List<JulgamentoDenunciaDto>();
        public List<RecursoDenunciaDto> Recursos { get; set; } = new List<RecursoDenunciaDto>();
        public List<HistoricoDenunciaDto> Historico { get; set; } = new List<HistoricoDenunciaDto>();

        // Informações de status do workflow
        public bool PodeSerEditada { get; set; }
        public bool EstaEmPrazo { get; set; }
        public bool PrazoDefesaVencido { get; set; }
        public bool PrazoRecursoVencido { get; set; }
        public bool PrazoProvasVencido { get; set; }
        public bool PrazoAlegacoesVencido { get; set; }

        // Auditoria
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class DenuncianteDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Cpf { get; set; }
        public string Cau { get; set; }
    }

    public class TipoDenunciaDto
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
    }

    public class FilialDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Uf { get; set; }
        public string Codigo { get; set; }
    }

    public class RelatorDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cargo { get; set; }
        public string Email { get; set; }
    }

    public class DenunciaChapaDto
    {
        public int Id { get; set; }
        public int ChapaEleicaoId { get; set; }
        public string ChapaNumero { get; set; }
        public string ChapaNome { get; set; }
        public string DetalhesEspecificos { get; set; }
        public string InfracoesAlegadas { get; set; }
    }

    public class DenunciaMembroChapaDto
    {
        public int Id { get; set; }
        public int MembroChapaId { get; set; }
        public string MembroNome { get; set; }
        public string CargoNaChapa { get; set; }
        public string DetalhesEspecificos { get; set; }
        public string CondutasIrregulares { get; set; }
    }

    public class DenunciaMembroComissaoDto
    {
        public int Id { get; set; }
        public int MembroComissaoId { get; set; }
        public string MembroNome { get; set; }
        public string FuncaoComissao { get; set; }
        public string DetalhesEspecificos { get; set; }
        public string AlegacoesParcialidade { get; set; }
    }

    public class DenunciaOutroDto
    {
        public int Id { get; set; }
        public string NomeDenunciado { get; set; }
        public string CpfCnpjDenunciado { get; set; }
        public string CargoFuncao { get; set; }
        public string InstituicaoVinculada { get; set; }
        public string DetalhesEspecificos { get; set; }
        public string RelacaoProcessoEleitoral { get; set; }
        public string Endereco { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
    }

    public class ArquivoDenunciaDto
    {
        public int Id { get; set; }
        public string NomeOriginal { get; set; }
        public string TipoDocumento { get; set; }
        public long TamanhoBytes { get; set; }
        public string TamanhoFormatado { get; set; }
        public string Descricao { get; set; }
        public DateTime DataUpload { get; set; }
        public string UploadedBy { get; set; }
    }

    public class TestemunhaDenunciaDto
    {
        public int Id { get; set; }
        public string NomeCompleto { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Profissao { get; set; }
        public string RelacaoComFatos { get; set; }
        public string ResumoTestemunho { get; set; }
        public bool Notificada { get; set; }
        public DateTime? DataNotificacao { get; set; }
        public bool Compareceu { get; set; }
        public DateTime? DataComparecimento { get; set; }
        public string Observacoes { get; set; }
    }

    public class JulgamentoDenunciaDto
    {
        public int Id { get; set; }
        public InstanciaJulgamento Instancia { get; set; }
        public string InstanciaDescricao => Instancia.ToString();
        public DateTime? DataAgendamento { get; set; }
        public DateTime? DataRealizacao { get; set; }
        public StatusJulgamento Status { get; set; }
        public string StatusDescricao => Status.ToString();
        public string RelatorioRelator { get; set; }
        public string VotoRelator { get; set; }
        public DecisaoJulgamento? Decisao { get; set; }
        public string DecisaoDescricao => Decisao?.ToString();
        public string ResultadoDetalhado { get; set; }
        public string FundamentacaoDecisao { get; set; }
        public string PenalidadeAplicada { get; set; }
        public int QuorumPresente { get; set; }
        public int VotosProcedencia { get; set; }
        public int VotosImprocedencia { get; set; }
        public int VotosAbstencao { get; set; }
        public bool DecisaoUnanime { get; set; }
    }

    public class RecursoDenunciaDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public StatusRecurso Status { get; set; }
        public string StatusDescricao => Status.ToString();
        public DateTime DataInterposicao { get; set; }
        public string Fundamentacao { get; set; }
        public string Pedido { get; set; }
        public string ContraRazoes { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public string DecisaoRecurso { get; set; }
        public string ResultadoRecurso { get; set; }
    }

    public class HistoricoDenunciaDto
    {
        public int Id { get; set; }
        public DateTime DataOcorrencia { get; set; }
        public string TipoOperacao { get; set; }
        public string Observacao { get; set; }
        public string UsuarioNome { get; set; }
        public string EnderecoIp { get; set; }
    }
}