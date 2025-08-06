using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade principal de Denúncia - Sistema Eleitoral CAU
    /// Migrada de DenunciaBO.php (3.899 linhas) - Módulo crítico do sistema
    /// </summary>
    public class Denuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Número sequencial da denúncia para protocolo
        /// </summary>
        public int NumeroSequencial { get; set; }

        /// <summary>
        /// Protocolo gerado automaticamente (DEN/YYYY/NNNNNN)
        /// </summary>
        public string Protocolo { get; set; }

        /// <summary>
        /// ID da pessoa denunciante
        /// </summary>
        public int PessoaDenuncianteId { get; set; }

        /// <summary>
        /// Tipo da denúncia (ID da tabela TB_TIPO_DENUNCIA)
        /// </summary>
        public int TipoDenunciaId { get; set; }

        /// <summary>
        /// ID da atividade secundária do calendário eleitoral
        /// </summary>
        public int AtividadeSecundariaCalendarioId { get; set; }

        /// <summary>
        /// Filial/UF responsável pela denúncia
        /// </summary>
        public int? FilialId { get; set; }

        /// <summary>
        /// Descrição detalhada dos fatos denunciados
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string DescricaoFatos { get; set; }

        /// <summary>
        /// Data e hora da denúncia
        /// </summary>
        public DateTime DataHoraDenuncia { get; set; }

        /// <summary>
        /// Status atual da denúncia
        /// </summary>
        public StatusDenuncia Status { get; set; } = StatusDenuncia.Recebida;

        /// <summary>
        /// Indica se a denúncia tem sigilo
        /// </summary>
        public bool TemSigilo { get; set; }

        // WORKFLOW DE ADMISSIBILIDADE
        /// <summary>
        /// Data da análise de admissibilidade
        /// </summary>
        public DateTime? DataAnaliseAdmissibilidade { get; set; }

        /// <summary>
        /// Resultado da análise de admissibilidade
        /// </summary>
        public bool? Admissivel { get; set; }

        /// <summary>
        /// Motivo da inadmissibilidade (obrigatório se inadmissível)
        /// </summary>
        [MaxLength(1000)]
        public string MotivoInadmissibilidade { get; set; }

        // WORKFLOW DE DEFESA
        /// <summary>
        /// Data de notificação para apresentação da defesa
        /// </summary>
        public DateTime? DataNotificacaoDefesa { get; set; }

        /// <summary>
        /// Prazo limite para apresentação da defesa (15 dias úteis)
        /// </summary>
        public DateTime? PrazoDefesa { get; set; }

        /// <summary>
        /// Data de recebimento da defesa
        /// </summary>
        public DateTime? DataRecebimentoDefesa { get; set; }

        /// <summary>
        /// Texto da defesa apresentada
        /// </summary>
        public string DefesaTexto { get; set; }

        // WORKFLOW DE PRODUÇÃO DE PROVAS
        /// <summary>
        /// Data limite para produção de provas (30 dias)
        /// </summary>
        public DateTime? PrazoProducaoProvas { get; set; }

        /// <summary>
        /// Data de encerramento da produção de provas
        /// </summary>
        public DateTime? DataEncerramentoProvas { get; set; }

        // WORKFLOW DE AUDIÊNCIA DE INSTRUÇÃO
        /// <summary>
        /// Data agendada para audiência de instrução
        /// </summary>
        public DateTime? DataAudienciaInstrucao { get; set; }

        /// <summary>
        /// Resumo/ata da audiência de instrução
        /// </summary>
        public string ResumoAudiencia { get; set; }

        /// <summary>
        /// Indica se audiência foi realizada
        /// </summary>
        public bool? AudienciaRealizada { get; set; }

        // WORKFLOW DE ALEGAÇÕES FINAIS
        /// <summary>
        /// Data limite para alegações finais
        /// </summary>
        public DateTime? PrazoAlegacoesFinais { get; set; }

        /// <summary>
        /// Texto das alegações finais
        /// </summary>
        public string AlegacoesFinaisTexto { get; set; }

        /// <summary>
        /// Data de recebimento das alegações finais
        /// </summary>
        public DateTime? DataRecebimentoAlegacoes { get; set; }

        // WORKFLOW DE JULGAMENTO - 1ª INSTÂNCIA
        /// <summary>
        /// Data do julgamento em primeira instância
        /// </summary>
        public DateTime? DataJulgamentoPrimeiraInstancia { get; set; }

        /// <summary>
        /// Decisão do julgamento em primeira instância
        /// </summary>
        public string DecisaoPrimeiraInstancia { get; set; }

        /// <summary>
        /// Indica se cabe recurso
        /// </summary>
        public bool CabeRecurso { get; set; }

        /// <summary>
        /// Prazo limite para interposição de recurso (15 dias úteis)
        /// </summary>
        public DateTime? PrazoRecurso { get; set; }

        // WORKFLOW DE RECURSO - 2ª INSTÂNCIA
        /// <summary>
        /// Data de interposição do recurso
        /// </summary>
        public DateTime? DataInterposicaoRecurso { get; set; }

        /// <summary>
        /// Fundamentação do recurso
        /// </summary>
        public string FundamentacaoRecurso { get; set; }

        /// <summary>
        /// Data do julgamento do recurso
        /// </summary>
        public DateTime? DataJulgamentoRecurso { get; set; }

        /// <summary>
        /// Decisão final do recurso (segunda instância)
        /// </summary>
        public string DecisaoRecurso { get; set; }

        // CONTROLE DE RELATOR
        /// <summary>
        /// ID do membro da comissão designado como relator
        /// </summary>
        public int? RelatorId { get; set; }

        /// <summary>
        /// Data de designação do relator
        /// </summary>
        public DateTime? DataDesignacaoRelator { get; set; }

        // CONTROLE DE ARQUIVAMENTO
        /// <summary>
        /// Data do arquivamento definitivo
        /// </summary>
        public DateTime? DataArquivamento { get; set; }

        /// <summary>
        /// Motivo do arquivamento
        /// </summary>
        public string MotivoArquivamento { get; set; }

        // NAVIGATION PROPERTIES
        /// <summary>
        /// Pessoa que fez a denúncia
        /// </summary>
        public virtual Profissional Denunciante { get; set; }

        /// <summary>
        /// Tipo da denúncia
        /// </summary>
        public virtual TipoDenuncia TipoDenuncia { get; set; }

        /// <summary>
        /// Atividade secundária do calendário
        /// </summary>
        public virtual AtividadeSecundariaCalendario AtividadeSecundaria { get; set; }

        /// <summary>
        /// Filial/UF responsável
        /// </summary>
        public virtual Filial Filial { get; set; }

        /// <summary>
        /// Relator designado
        /// </summary>
        public virtual MembroComissaoEleitoral Relator { get; set; }

        // RELACIONAMENTOS ESPECÍFICOS DE DENÚNCIA
        /// <summary>
        /// Denúncia contra chapa eleitoral
        /// </summary>
        public virtual DenunciaChapa DenunciaChapa { get; set; }

        /// <summary>
        /// Denúncia contra membro de chapa
        /// </summary>
        public virtual DenunciaMembroChapa DenunciaMembroChapa { get; set; }

        /// <summary>
        /// Denúncia contra membro de comissão
        /// </summary>
        public virtual DenunciaMembroComissaoEleitoral DenunciaMembroComissaoEleitoral { get; set; }

        /// <summary>
        /// Denúncia contra outros (terceiros)
        /// </summary>
        public virtual DenunciaOutro DenunciaOutro { get; set; }

        // COLEÇÕES
        /// <summary>
        /// Arquivos anexados à denúncia
        /// </summary>
        public virtual ICollection<ArquivoDenuncia> Arquivos { get; set; } = new List<ArquivoDenuncia>();

        /// <summary>
        /// Histórico de situações da denúncia
        /// </summary>
        public virtual ICollection<DenunciaSituacao> HistoricoSituacoes { get; set; } = new List<DenunciaSituacao>();

        /// <summary>
        /// Testemunhas da denúncia
        /// </summary>
        public virtual ICollection<TestemunhaDenuncia> Testemunhas { get; set; } = new List<TestemunhaDenuncia>();

        /// <summary>
        /// Julgamentos da denúncia (primeira e segunda instância)
        /// </summary>
        public virtual ICollection<JulgamentoDenuncia> Julgamentos { get; set; } = new List<JulgamentoDenuncia>();

        /// <summary>
        /// Recursos interpostos
        /// </summary>
        public virtual ICollection<RecursoDenuncia> Recursos { get; set; } = new List<RecursoDenuncia>();

        /// <summary>
        /// Encaminhamentos da denúncia
        /// </summary>
        public virtual ICollection<EncaminhamentoDenuncia> Encaminhamentos { get; set; } = new List<EncaminhamentoDenuncia>();

        /// <summary>
        /// Histórico completo da denúncia
        /// </summary>
        public virtual ICollection<HistoricoDenuncia> Historico { get; set; } = new List<HistoricoDenuncia>();

        /// <summary>
        /// Notificações enviadas
        /// </summary>
        public virtual ICollection<NotificacaoEleitoral> Notificacoes { get; set; } = new List<NotificacaoEleitoral>();

        // MÉTODOS DE NEGÓCIO - WORKFLOW LEGAL
        
        /// <summary>
        /// Gera o protocolo da denúncia automaticamente
        /// Formato: DEN/YYYY/NNNNNN
        /// </summary>
        public void GerarProtocolo()
        {
            if (string.IsNullOrEmpty(Protocolo))
            {
                var ano = DataHoraDenuncia.Year;
                Protocolo = $"DEN/{ano:D4}/{NumeroSequencial:D6}";
            }
        }

        /// <summary>
        /// Inicia análise de admissibilidade da denúncia
        /// </summary>
        public void IniciarAnaliseAdmissibilidade()
        {
            if (Status != StatusDenuncia.Recebida)
                throw new BusinessException("Denúncia deve estar em status 'Recebida' para iniciar análise de admissibilidade");

            Status = StatusDenuncia.EmAnalise;
            AdicionarHistorico("Iniciada análise de admissibilidade");
        }

        /// <summary>
        /// Conclui análise de admissibilidade - Regra legal crítica
        /// </summary>
        /// <param name="admissivel">Se a denúncia é admissível</param>
        /// <param name="motivo">Motivo da inadmissibilidade (obrigatório se inadmissível)</param>
        public void ConcluirAnaliseAdmissibilidade(bool admissivel, string motivo = null)
        {
            if (Status != StatusDenuncia.EmAnalise)
                throw new BusinessException("Denúncia deve estar em análise para concluir admissibilidade");

            Admissivel = admissivel;
            DataAnaliseAdmissibilidade = DateTime.UtcNow;
            
            if (admissivel)
            {
                Status = StatusDenuncia.Admissivel;
                AdicionarHistorico("Denúncia considerada ADMISSÍVEL");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(motivo))
                    throw new BusinessException("Motivo de inadmissibilidade é obrigatório");
                
                Status = StatusDenuncia.Inadmissivel;
                MotivoInadmissibilidade = motivo;
                AdicionarHistorico($"Denúncia considerada INADMISSÍVEL - Motivo: {motivo}");
            }
        }

        /// <summary>
        /// Notifica denunciado para apresentar defesa - Prazo legal: 15 dias úteis
        /// </summary>
        public void NotificarParaDefesa()
        {
            if (Status != StatusDenuncia.Admissivel)
                throw new BusinessException("Denúncia deve estar admitida para notificar defesa");

            DataNotificacaoDefesa = DateTime.UtcNow;
            PrazoDefesa = CalcularPrazoUtil(DateTime.UtcNow, 15); // 15 dias úteis
            Status = StatusDenuncia.AguardandoDefesa;
            
            AdicionarHistorico($"Notificado para defesa - Prazo até: {PrazoDefesa:dd/MM/yyyy}");
        }

        /// <summary>
        /// Recebe defesa apresentada pelo denunciado
        /// </summary>
        /// <param name="textoDefesa">Texto da defesa</param>
        public void ReceberDefesa(string textoDefesa)
        {
            if (Status != StatusDenuncia.AguardandoDefesa)
                throw new BusinessException("Denúncia não está aguardando defesa");

            if (string.IsNullOrWhiteSpace(textoDefesa))
                throw new BusinessException("Texto da defesa é obrigatório");

            if (DateTime.UtcNow > PrazoDefesa)
                throw new BusinessException("Prazo para defesa expirado");

            DefesaTexto = textoDefesa;
            DataRecebimentoDefesa = DateTime.UtcNow;
            Status = StatusDenuncia.DefesaRecebida;
            
            AdicionarHistorico("Defesa recebida dentro do prazo");
        }

        /// <summary>
        /// Abre prazo para produção de provas - 30 dias
        /// </summary>
        public void AbrirProducaoProvas()
        {
            if (Status != StatusDenuncia.DefesaRecebida)
                throw new BusinessException("Denúncia deve ter defesa recebida para abrir produção de provas");

            PrazoProducaoProvas = DateTime.UtcNow.AddDays(30);
            AdicionarHistorico($"Aberto prazo para produção de provas até: {PrazoProducaoProvas:dd/MM/yyyy}");
        }

        /// <summary>
        /// Agenda audiência de instrução
        /// </summary>
        /// <param name="dataAudiencia">Data da audiência</param>
        public void AgendarAudienciaInstrucao(DateTime dataAudiencia)
        {
            if (dataAudiencia <= DateTime.UtcNow)
                throw new BusinessException("Data da audiência deve ser futura");

            DataAudienciaInstrucao = dataAudiencia;
            Status = StatusDenuncia.AudienciaInstrucao;
            
            AdicionarHistorico($"Audiência de instrução agendada para: {dataAudiencia:dd/MM/yyyy HH:mm}");
        }

        /// <summary>
        /// Registra realização da audiência de instrução
        /// </summary>
        /// <param name="resumo">Resumo/ata da audiência</param>
        public void RegistrarAudiencia(string resumo)
        {
            if (Status != StatusDenuncia.AudienciaInstrucao)
                throw new BusinessException("Não há audiência agendada");

            if (string.IsNullOrWhiteSpace(resumo))
                throw new BusinessException("Resumo da audiência é obrigatório");

            ResumoAudiencia = resumo;
            AudienciaRealizada = true;
            PrazoAlegacoesFinais = DateTime.UtcNow.AddDays(10); // 10 dias para alegações finais
            Status = StatusDenuncia.AlegacoesFinais;
            
            AdicionarHistorico("Audiência de instrução realizada");
        }

        /// <summary>
        /// Recebe alegações finais
        /// </summary>
        /// <param name="alegacoes">Texto das alegações finais</param>
        public void ReceberAlegacoesFinais(string alegacoes)
        {
            if (Status != StatusDenuncia.AlegacoesFinais)
                throw new BusinessException("Denúncia não está na fase de alegações finais");

            AlegacoesFinaisTexto = alegacoes;
            DataRecebimentoAlegacoes = DateTime.UtcNow;
            Status = StatusDenuncia.AguardandoJulgamento;
            
            AdicionarHistorico("Alegações finais recebidas");
        }

        /// <summary>
        /// Julga a denúncia em primeira instância
        /// </summary>
        /// <param name="decisao">Texto da decisão</param>
        /// <param name="cabeRecurso">Se cabe recurso da decisão</param>
        public void Julgar(string decisao, bool cabeRecurso)
        {
            if (Status != StatusDenuncia.AguardandoJulgamento)
                throw new BusinessException("Denúncia não está aguardando julgamento");

            if (string.IsNullOrWhiteSpace(decisao))
                throw new BusinessException("Texto da decisão é obrigatório");

            DecisaoPrimeiraInstancia = decisao;
            DataJulgamentoPrimeiraInstancia = DateTime.UtcNow;
            CabeRecurso = cabeRecurso;
            
            if (cabeRecurso)
            {
                PrazoRecurso = CalcularPrazoUtil(DateTime.UtcNow, 15); // 15 dias úteis para recurso
                Status = StatusDenuncia.Julgada;
                AdicionarHistorico($"Julgada em 1ª instância - Cabe recurso até: {PrazoRecurso:dd/MM/yyyy}");
            }
            else
            {
                Status = StatusDenuncia.Arquivada;
                DataArquivamento = DateTime.UtcNow;
                AdicionarHistorico("Julgada em 1ª instância - Não cabe recurso - ARQUIVADA");
            }
        }

        /// <summary>
        /// Interpõe recurso contra a decisão
        /// </summary>
        /// <param name="fundamentacao">Fundamentação do recurso</param>
        public void InterporRecurso(string fundamentacao)
        {
            if (Status != StatusDenuncia.Julgada)
                throw new BusinessException("Denúncia deve estar julgada para interpor recurso");

            if (!CabeRecurso)
                throw new BusinessException("Não cabe recurso desta decisão");

            if (DateTime.UtcNow > PrazoRecurso)
                throw new BusinessException("Prazo para recurso expirado");

            if (string.IsNullOrWhiteSpace(fundamentacao))
                throw new BusinessException("Fundamentação do recurso é obrigatória");

            FundamentacaoRecurso = fundamentacao;
            DataInterposicaoRecurso = DateTime.UtcNow;
            Status = StatusDenuncia.EmRecurso;
            
            AdicionarHistorico("Recurso interposto - Aguardando julgamento em 2ª instância");
        }

        /// <summary>
        /// Julga o recurso em segunda instância
        /// </summary>
        /// <param name="decisaoRecurso">Decisão do recurso</param>
        public void JulgarRecurso(string decisaoRecurso)
        {
            if (Status != StatusDenuncia.EmRecurso)
                throw new BusinessException("Não há recurso para julgar");

            if (string.IsNullOrWhiteSpace(decisaoRecurso))
                throw new BusinessException("Texto da decisão do recurso é obrigatório");

            DecisaoRecurso = decisaoRecurso;
            DataJulgamentoRecurso = DateTime.UtcNow;
            Status = StatusDenuncia.Arquivada;
            DataArquivamento = DateTime.UtcNow;
            
            AdicionarHistorico("Recurso julgado em 2ª instância - PROCESSO ENCERRADO");
        }

        /// <summary>
        /// Arquiva a denúncia com motivo específico
        /// </summary>
        /// <param name="motivo">Motivo do arquivamento</param>
        public void Arquivar(string motivo)
        {
            if (Status == StatusDenuncia.Arquivada)
                throw new BusinessException("Denúncia já está arquivada");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BusinessException("Motivo do arquivamento é obrigatório");

            Status = StatusDenuncia.Arquivada;
            DataArquivamento = DateTime.UtcNow;
            MotivoArquivamento = motivo;
            
            AdicionarHistorico($"ARQUIVADA - Motivo: {motivo}");
        }

        /// <summary>
        /// Designa relator para a denúncia
        /// </summary>
        /// <param name="relatorId">ID do membro da comissão relator</param>
        public void DesignarRelator(int relatorId)
        {
            RelatorId = relatorId;
            DataDesignacaoRelator = DateTime.UtcNow;
            
            AdicionarHistorico($"Relator designado - ID: {relatorId}");
        }

        // MÉTODOS DE VERIFICAÇÃO DE PRAZOS
        
        public bool PrazoDefesaVencido()
        {
            return PrazoDefesa.HasValue && DateTime.UtcNow > PrazoDefesa.Value;
        }

        public bool PrazoRecursoVencido()
        {
            return PrazoRecurso.HasValue && DateTime.UtcNow > PrazoRecurso.Value;
        }

        public bool PrazoProvasVencido()
        {
            return PrazoProducaoProvas.HasValue && DateTime.UtcNow > PrazoProducaoProvas.Value;
        }

        public bool PrazoAlegacoesVencido()
        {
            return PrazoAlegacoesFinais.HasValue && DateTime.UtcNow > PrazoAlegacoesFinais.Value;
        }

        // MÉTODOS AUXILIARES

        /// <summary>
        /// Adiciona entrada no histórico da denúncia
        /// </summary>
        /// <param name="observacao">Observação do histórico</param>
        private void AdicionarHistorico(string observacao)
        {
            var historico = new HistoricoDenuncia
            {
                DenunciaId = Id,
                DataOcorrencia = DateTime.UtcNow,
                Observacao = observacao,
                UsuarioId = int.TryParse(CreatedBy, out var userId) ? userId : 0 // Usuário que criou/modificou
            };
            
            Historico.Add(historico);
        }

        /// <summary>
        /// Calcula prazo útil (excluindo finais de semana)
        /// Simplified version - production should consider holidays
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="diasUteis">Quantidade de dias úteis</param>
        /// <returns>Data final do prazo</returns>
        private static DateTime CalcularPrazoUtil(DateTime dataInicio, int diasUteis)
        {
            var dataFinal = dataInicio;
            var diasAdicionados = 0;

            while (diasAdicionados < diasUteis)
            {
                dataFinal = dataFinal.AddDays(1);
                
                // Se não for sábado ou domingo, conta como dia útil
                if (dataFinal.DayOfWeek != DayOfWeek.Saturday && dataFinal.DayOfWeek != DayOfWeek.Sunday)
                {
                    diasAdicionados++;
                }
            }

            return dataFinal;
        }

        /// <summary>
        /// Verifica se a denúncia pode ser editada
        /// </summary>
        /// <returns>True se pode ser editada</returns>
        public bool PodeSerEditada()
        {
            return Status == StatusDenuncia.Recebida || Status == StatusDenuncia.EmAnalise;
        }

        /// <summary>
        /// Verifica se a denúncia está em prazo processual
        /// </summary>
        /// <returns>True se está em prazo</returns>
        public bool EstaEmPrazo()
        {
            return Status switch
            {
                StatusDenuncia.AguardandoDefesa => !PrazoDefesaVencido(),
                StatusDenuncia.Julgada => !PrazoRecursoVencido(),
                StatusDenuncia.AlegacoesFinais => !PrazoAlegacoesVencido(),
                _ => true
            };
        }
    }
}