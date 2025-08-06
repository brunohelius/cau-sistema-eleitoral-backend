using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Julgamentos da denúncia (primeira e segunda instância)
    /// </summary>
    public class JulgamentoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Instância do julgamento (primeira, segunda)
        /// </summary>
        public InstanciaJulgamento Instancia { get; set; }

        /// <summary>
        /// Data de agendamento do julgamento
        /// </summary>
        public DateTime? DataAgendamento { get; set; }

        /// <summary>
        /// Data de realização do julgamento
        /// </summary>
        public DateTime? DataRealizacao { get; set; }

        /// <summary>
        /// Status do julgamento
        /// </summary>
        public StatusJulgamento Status { get; set; } = StatusJulgamento.Agendado;

        /// <summary>
        /// Relator designado para o julgamento
        /// </summary>
        public int? RelatorId { get; set; }

        /// <summary>
        /// Relatório do relator
        /// </summary>
        public string RelatorioRelator { get; set; }

        /// <summary>
        /// Voto do relator
        /// </summary>
        public string VotoRelator { get; set; }

        /// <summary>
        /// Decisão do julgamento
        /// </summary>
        public DecisaoJulgamento? Decisao { get; set; }

        /// <summary>
        /// Resultado detalhado do julgamento
        /// </summary>
        public string ResultadoDetalhado { get; set; }

        /// <summary>
        /// Fundamentação da decisão
        /// </summary>
        public string FundamentacaoDecisao { get; set; }

        /// <summary>
        /// Penalidade aplicada (se houver)
        /// </summary>
        public string PenalidadeAplicada { get; set; }

        /// <summary>
        /// Observações gerais do julgamento
        /// </summary>
        public string Observacoes { get; set; }

        /// <summary>
        /// Quórum presente no julgamento
        /// </summary>
        public int QuorumPresente { get; set; }

        /// <summary>
        /// Total de votos válidos
        /// </summary>
        public int TotalVotosValidos { get; set; }

        /// <summary>
        /// Votos a favor da procedência
        /// </summary>
        public int VotosProcedencia { get; set; }

        /// <summary>
        /// Votos contra a procedência
        /// </summary>
        public int VotosImprocedencia { get; set; }

        /// <summary>
        /// Abstenções
        /// </summary>
        public int VotosAbstencao { get; set; }

        /// <summary>
        /// Indica se houve unanimidade
        /// </summary>
        public bool DecisaoUnanime { get; set; }

        /// <summary>
        /// Ata do julgamento
        /// </summary>
        public string AtaJulgamento { get; set; }

        /// <summary>
        /// Ordem do dia do julgamento
        /// </summary>
        public int? OrdemDia { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Relator do julgamento
        /// </summary>
        public virtual ComissaoEleitoral Relator { get; set; }

        /// <summary>
        /// Votos individuais dos membros
        /// </summary>
        public virtual ICollection<VotoJulgamentoDenuncia> Votos { get; set; } = new List<VotoJulgamentoDenuncia>();

        /// <summary>
        /// Arquivos relacionados ao julgamento
        /// </summary>
        public virtual ICollection<ArquivoJulgamentoDenuncia> Arquivos { get; set; } = new List<ArquivoJulgamentoDenuncia>();
    }
}