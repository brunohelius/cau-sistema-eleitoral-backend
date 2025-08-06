using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Application.DTOs.Denuncia
{
    /// <summary>
    /// DTOs para operações do workflow de denúncias
    /// </summary>

    public class DenunciaFiltroDto
    {
        public string Protocolo { get; set; }
        public int? TipoDenunciaId { get; set; }
        public int? FilialId { get; set; }
        public string Status { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string DenuncianteName { get; set; }
        public int? RelatorId { get; set; }
        public bool? ApenasPrazosVencidos { get; set; }
        public bool? ApenasComSigilo { get; set; }
        public string OrderBy { get; set; } = "DataHoraDenuncia";
        public bool OrderDesc { get; set; } = true;
    }

    public class AtualizarDenunciaDto
    {
        [MaxLength(2000)]
        public string DescricaoFatos { get; set; }

        public bool? TemSigilo { get; set; }

        public int? FilialId { get; set; }

        // Permitir atualização apenas se denúncia não foi analisada
        public CriarDenunciaChapaDto DenunciaChapa { get; set; }
        public CriarDenunciaMembroChapaDto DenunciaMembroChapa { get; set; }
        public CriarDenunciaMembroComissaoDto DenunciaMembroComissao { get; set; }
        public CriarDenunciaOutroDto DenunciaOutro { get; set; }
    }

    public class ConcluirAnaliseAdmissibilidadeDto
    {
        [Required]
        public bool Admissivel { get; set; }

        public string Motivo { get; set; }
    }

    public class ReceberDefesaDto
    {
        [Required]
        public string TextoDefesa { get; set; }
    }

    public class AgendarAudienciaDto
    {
        [Required]
        public DateTime DataAudiencia { get; set; }
    }

    public class RegistrarAudienciaDto
    {
        [Required]
        public string ResumoAudiencia { get; set; }
    }

    public class JulgarDenunciaDto
    {
        [Required]
        public string Decisao { get; set; }

        [Required]
        public bool CabeRecurso { get; set; }
    }

    public class InterporRecursoDto
    {
        [Required]
        public string Fundamentacao { get; set; }

        public string Pedido { get; set; }
    }

    public class JulgarRecursoDto
    {
        [Required]
        public string DecisaoRecurso { get; set; }
    }

    public class ArquivarDenunciaDto
    {
        [Required]
        public string Motivo { get; set; }
    }

    public class DesignarRelatorDto
    {
        [Required]
        public int RelatorId { get; set; }
    }

    public class EstatisticasDenunciaDto
    {
        public int TotalDenuncias { get; set; }
        public int DenunciasRecebidas { get; set; }
        public int DenunciasEmAnalise { get; set; }
        public int DenunciasAdmissiveis { get; set; }
        public int DenunciasInadmissiveis { get; set; }
        public int DenunciasAguardandoDefesa { get; set; }
        public int DenunciasDefesaRecebida { get; set; }
        public int DenunciasAudienciaInstrucao { get; set; }
        public int DenunciasAguardandoJulgamento { get; set; }
        public int DenunciasJulgadas { get; set; }
        public int DenunciasEmRecurso { get; set; }
        public int DenunciasArquivadas { get; set; }

        public int DenunciasPrazoDefesaVencido { get; set; }
        public int DenunciasPrazoRecursoVencido { get; set; }
        public int DenunciasPrazoProvasVencido { get; set; }

        public Dictionary<string, int> DenunciasPorTipo { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DenunciasPorFilial { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DenunciasPorMes { get; set; } = new Dictionary<string, int>();

        public double TempoMedioAnaliseAdmissibilidade { get; set; } // em dias
        public double TempoMedioDefesa { get; set; } // em dias
        public double TempoMedioJulgamento { get; set; } // em dias
        public double TempoMedioProcessoCompleto { get; set; } // em dias

        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class PrazosDenunciaDto
    {
        public int DenunciaId { get; set; }
        public string Protocolo { get; set; }
        public string Status { get; set; }

        // Prazos de defesa
        public DateTime? PrazoDefesa { get; set; }
        public bool PrazoDefesaVencido { get; set; }
        public int? DiasParaVencimentoDefesa { get; set; }

        // Prazos de recurso
        public DateTime? PrazoRecurso { get; set; }
        public bool PrazoRecursoVencido { get; set; }
        public int? DiasParaVencimentoRecurso { get; set; }

        // Prazos de produção de provas
        public DateTime? PrazoProducaoProvas { get; set; }
        public bool PrazoProvasVencido { get; set; }
        public int? DiasParaVencimentoProvas { get; set; }

        // Prazos de alegações finais
        public DateTime? PrazoAlegacoesFinais { get; set; }
        public bool PrazoAlegacoesVencido { get; set; }
        public int? DiasParaVencimentoAlegacoes { get; set; }

        public bool EstaEmPrazo { get; set; }
        public string ProximoVencimento { get; set; }
        public DateTime? DataProximoVencimento { get; set; }
        public int? DiasProximoVencimento { get; set; }
    }

    public class DenunciaVencimentoDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public string Status { get; set; }
        public string TipoVencimento { get; set; } // "DEFESA", "RECURSO", "PROVAS", "ALEGACOES"
        public DateTime DataVencimento { get; set; }
        public int DiasParaVencimento { get; set; }
        public string DenuncianteName { get; set; }
        public string FilialNome { get; set; }
        public string RelatorNome { get; set; }
        public bool PrioridadeAlta { get; set; } // se vence em menos de 3 dias
    }

    public class ArquivoDownloadDto
    {
        public byte[] Conteudo { get; set; }
        public string NomeOriginal { get; set; }
        public string TipoMime { get; set; }
    }
}