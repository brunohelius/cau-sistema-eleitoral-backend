using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Log detalhado de todas as operações de votação para auditoria
    /// </summary>
    public class LogVotacao : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public TipoOperacaoVotacao TipoOperacao { get; set; }
        public StatusOperacao StatusOperacao { get; set; }
        public string Entidade { get; set; }
        public int? EntidadeId { get; set; }
        
        // Identificação
        public int? UsuarioId { get; set; }
        public string UsuarioNome { get; set; }
        public string UsuarioCpf { get; set; }
        public string UsuarioPerfil { get; set; }
        public int? EleicaoId { get; set; }
        public int? SessaoVotacaoId { get; set; }
        
        // Detalhes da Operação
        public string Acao { get; set; }
        public string Descricao { get; set; }
        public string DadosAntes { get; set; } // JSON com estado anterior
        public string DadosDepois { get; set; } // JSON com estado posterior
        public string Parametros { get; set; } // JSON com parâmetros da operação
        
        // Rastreabilidade
        public string IpOrigem { get; set; }
        public string UserAgent { get; set; }
        public string Dispositivo { get; set; }
        public string SessaoSistema { get; set; }
        public string Localizacao { get; set; }
        
        // Segurança
        public string HashOperacao { get; private set; }
        public string HashAnterior { get; set; }
        public bool Critico { get; set; }
        public NivelSeguranca NivelSeguranca { get; set; }
        
        // Resultado
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public string StackTrace { get; set; }
        public int? CodigoErro { get; set; }
        
        // Auditoria
        public bool RequerRevisao { get; set; }
        public bool FoiRevisado { get; set; }
        public DateTime? DataRevisao { get; set; }
        public string RevisadoPor { get; set; }
        public string ParecerRevisao { get; set; }
        
        // Navegação
        public virtual Usuario Usuario { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        public virtual SessaoVotacaoAvancada SessaoVotacao { get; set; }
        
        // Construtor
        public LogVotacao()
        {
            DataHora = DateTime.UtcNow;
            Sucesso = true;
            Critico = false;
            RequerRevisao = false;
            FoiRevisado = false;
            NivelSeguranca = NivelSeguranca.Normal;
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Registra operação de votação
        /// </summary>
        public static LogVotacao RegistrarOperacao(
            TipoOperacaoVotacao tipo,
            string entidade,
            int? entidadeId,
            string acao,
            string descricao,
            int? usuarioId = null,
            string usuarioNome = null)
        {
            var log = new LogVotacao
            {
                TipoOperacao = tipo,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Acao = acao,
                Descricao = descricao,
                UsuarioId = usuarioId,
                UsuarioNome = usuarioNome,
                StatusOperacao = StatusOperacao.Iniciada
            };
            
            // Determinar criticidade
            log.DeterminarCriticidade();
            
            // Gerar hash
            log.HashOperacao = log.GerarHashOperacao();
            
            return log;
        }
        
        /// <summary>
        /// Registra início de operação
        /// </summary>
        public void IniciarOperacao(string parametros = null)
        {
            StatusOperacao = StatusOperacao.EmAndamento;
            Parametros = parametros;
            DataHora = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Registra conclusão com sucesso
        /// </summary>
        public void ConcluirComSucesso(string dadosDepois = null)
        {
            StatusOperacao = StatusOperacao.Concluida;
            Sucesso = true;
            DadosDepois = dadosDepois;
            
            // Atualizar hash com dados finais
            HashOperacao = GerarHashOperacao();
        }
        
        /// <summary>
        /// Registra falha na operação
        /// </summary>
        public void RegistrarFalha(string mensagemErro, int? codigoErro = null, string stackTrace = null)
        {
            StatusOperacao = StatusOperacao.Falha;
            Sucesso = false;
            MensagemErro = mensagemErro;
            CodigoErro = codigoErro;
            StackTrace = stackTrace;
            
            // Operações críticas que falharam requerem revisão
            if (Critico)
            {
                RequerRevisao = true;
            }
        }
        
        /// <summary>
        /// Adiciona informações de contexto
        /// </summary>
        public void AdicionarContexto(string ip, string userAgent, string dispositivo = null, string sessao = null)
        {
            IpOrigem = ip;
            UserAgent = userAgent;
            Dispositivo = dispositivo;
            SessaoSistema = sessao;
        }
        
        /// <summary>
        /// Adiciona informações de segurança
        /// </summary>
        public void AdicionarSeguranca(string hashAnterior, NivelSeguranca nivel)
        {
            HashAnterior = hashAnterior;
            NivelSeguranca = nivel;
            
            if (nivel == NivelSeguranca.Critico)
            {
                Critico = true;
                RequerRevisao = true;
            }
        }
        
        /// <summary>
        /// Determina criticidade da operação
        /// </summary>
        private void DeterminarCriticidade()
        {
            // Operações críticas que sempre requerem atenção
            var operacoesCriticas = new[]
            {
                TipoOperacaoVotacao.RegistroVoto,
                TipoOperacaoVotacao.AnulacaoVoto,
                TipoOperacaoVotacao.AlteracaoResultado,
                TipoOperacaoVotacao.AssinaturaDigital,
                TipoOperacaoVotacao.PublicacaoResultado,
                TipoOperacaoVotacao.Recontagem
            };
            
            if (Array.Exists(operacoesCriticas, op => op == TipoOperacao))
            {
                Critico = true;
                NivelSeguranca = NivelSeguranca.Critico;
            }
        }
        
        /// <summary>
        /// Gera hash da operação
        /// </summary>
        private string GerarHashOperacao()
        {
            var dados = $"{TipoOperacao}|{Entidade}|{EntidadeId}|{Acao}|{DataHora:O}|{UsuarioId}|{IpOrigem}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Marca log para revisão
        /// </summary>
        public void MarcarParaRevisao(string motivo)
        {
            RequerRevisao = true;
            Descricao += $" [REVISÃO NECESSÁRIA: {motivo}]";
        }
        
        /// <summary>
        /// Registra revisão do log
        /// </summary>
        public void RegistrarRevisao(string revisor, string parecer, bool aprovado)
        {
            FoiRevisado = true;
            DataRevisao = DateTime.UtcNow;
            RevisadoPor = revisor;
            ParecerRevisao = parecer;
            
            if (!aprovado)
            {
                StatusOperacao = StatusOperacao.Rejeitada;
            }
        }
        
        /// <summary>
        /// Verifica integridade do log
        /// </summary>
        public bool VerificarIntegridade()
        {
            var hashCalculado = GerarHashOperacao();
            return hashCalculado == HashOperacao;
        }
        
        /// <summary>
        /// Obtém resumo do log para visualização
        /// </summary>
        public string ObterResumo()
        {
            var status = Sucesso ? "✓" : "✗";
            var critico = Critico ? "[CRÍTICO]" : "";
            return $"{status} {DataHora:yyyy-MM-dd HH:mm:ss} - {TipoOperacao} {critico}: {Acao} ({Entidade}#{EntidadeId})";
        }
    }
    
    /// <summary>
    /// Log agregado de votação para análise
    /// </summary>
    public class LogVotacaoAgregado : BaseEntity
    {
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public TipoAgregacao TipoAgregacao { get; set; }
        
        // Estatísticas
        public int TotalOperacoes { get; set; }
        public int OperacoesSucesso { get; set; }
        public int OperacoesFalha { get; set; }
        public int OperacoesCriticas { get; set; }
        public int OperacoesRevisadas { get; set; }
        public int OperacoesPendentesRevisao { get; set; }
        
        // Por Tipo
        public string DistribuicaoPorTipoJson { get; set; } // JSON com contagem por tipo
        public string OperacaoMaisFrequente { get; set; }
        public int QuantidadeOperacaoMaisFrequente { get; set; }
        
        // Por Usuário
        public string UsuarioMaisAtivo { get; set; }
        public int OperacoesUsuarioMaisAtivo { get; set; }
        public int QuantidadeUsuariosAtivos { get; set; }
        
        // Por Tempo
        public string DistribuicaoTemporalJson { get; set; } // JSON com operações por hora
        public DateTime HorarioPico { get; set; }
        public int OperacoesHorarioPico { get; set; }
        
        // Anomalias
        public int AnomaliaDetectadas { get; set; }
        public string AnomaliasJson { get; set; } // JSON com detalhes das anomalias
        public bool RequerInvestigacao { get; set; }
        
        // Navegação
        public virtual Eleicao Eleicao { get; set; }
        
        // Métodos
        
        /// <summary>
        /// Agrega logs de votação
        /// </summary>
        public void AgregarLogs(List<LogVotacao> logs)
        {
            if (logs == null || !logs.Any())
                return;
            
            DataInicio = logs.Min(l => l.DataHora);
            DataFim = logs.Max(l => l.DataHora);
            
            // Estatísticas básicas
            TotalOperacoes = logs.Count;
            OperacoesSucesso = logs.Count(l => l.Sucesso);
            OperacoesFalha = logs.Count(l => !l.Sucesso);
            OperacoesCriticas = logs.Count(l => l.Critico);
            OperacoesRevisadas = logs.Count(l => l.FoiRevisado);
            OperacoesPendentesRevisao = logs.Count(l => l.RequerRevisao && !l.FoiRevisado);
            
            // Distribuição por tipo
            var porTipo = logs.GroupBy(l => l.TipoOperacao)
                .Select(g => new { Tipo = g.Key.ToString(), Quantidade = g.Count() })
                .OrderByDescending(x => x.Quantidade);
            
            DistribuicaoPorTipoJson = System.Text.Json.JsonSerializer.Serialize(porTipo);
            
            var maisFrequente = porTipo.FirstOrDefault();
            if (maisFrequente != null)
            {
                OperacaoMaisFrequente = maisFrequente.Tipo;
                QuantidadeOperacaoMaisFrequente = maisFrequente.Quantidade;
            }
            
            // Por usuário
            var porUsuario = logs.Where(l => l.UsuarioId.HasValue)
                .GroupBy(l => new { l.UsuarioId, l.UsuarioNome })
                .Select(g => new { Usuario = g.Key.UsuarioNome, Quantidade = g.Count() })
                .OrderByDescending(x => x.Quantidade);
            
            QuantidadeUsuariosAtivos = porUsuario.Count();
            
            var usuarioMaisAtivo = porUsuario.FirstOrDefault();
            if (usuarioMaisAtivo != null)
            {
                UsuarioMaisAtivo = usuarioMaisAtivo.Usuario;
                OperacoesUsuarioMaisAtivo = usuarioMaisAtivo.Quantidade;
            }
            
            // Distribuição temporal
            var porHora = logs.GroupBy(l => l.DataHora.Hour)
                .Select(g => new { Hora = $"{g.Key:00}:00", Quantidade = g.Count() })
                .OrderBy(x => x.Hora);
            
            DistribuicaoTemporalJson = System.Text.Json.JsonSerializer.Serialize(porHora);
            
            var pico = porHora.OrderByDescending(x => x.Quantidade).FirstOrDefault();
            if (pico != null)
            {
                HorarioPico = DateTime.Today.AddHours(int.Parse(pico.Hora.Substring(0, 2)));
                OperacoesHorarioPico = pico.Quantidade;
            }
            
            // Detectar anomalias
            DetectarAnomalias(logs);
        }
        
        /// <summary>
        /// Detecta anomalias nos logs
        /// </summary>
        private void DetectarAnomalias(List<LogVotacao> logs)
        {
            var anomalias = new List<object>();
            
            // Muitas falhas consecutivas
            var falhasConsecutivas = 0;
            foreach (var log in logs.OrderBy(l => l.DataHora))
            {
                if (!log.Sucesso)
                {
                    falhasConsecutivas++;
                    if (falhasConsecutivas >= 5)
                    {
                        anomalias.Add(new
                        {
                            tipo = "Falhas Consecutivas",
                            quantidade = falhasConsecutivas,
                            momento = log.DataHora
                        });
                    }
                }
                else
                {
                    falhasConsecutivas = 0;
                }
            }
            
            // Operações críticas fora do horário
            var operacoesCriticasForaHorario = logs
                .Where(l => l.Critico && (l.DataHora.Hour < 6 || l.DataHora.Hour > 22))
                .Select(l => new
                {
                    tipo = "Operação Crítica Fora do Horário",
                    operacao = l.TipoOperacao.ToString(),
                    horario = l.DataHora.ToString("HH:mm:ss")
                });
            
            anomalias.AddRange(operacoesCriticasForaHorario);
            
            // Taxa de erro alta
            var taxaErro = (decimal)OperacoesFalha / TotalOperacoes * 100;
            if (taxaErro > 10)
            {
                anomalias.Add(new
                {
                    tipo = "Taxa de Erro Alta",
                    percentual = taxaErro,
                    total_falhas = OperacoesFalha
                });
            }
            
            // Muitas operações pendentes de revisão
            if (OperacoesPendentesRevisao > 10)
            {
                anomalias.Add(new
                {
                    tipo = "Muitas Operações Pendentes",
                    quantidade = OperacoesPendentesRevisao
                });
            }
            
            AnomaliaDetectadas = anomalias.Count;
            AnomaliasJson = System.Text.Json.JsonSerializer.Serialize(anomalias);
            RequerInvestigacao = AnomaliaDetectadas > 0;
        }
    }
    
    /// <summary>
    /// Tipo de operação de votação
    /// </summary>
    public enum TipoOperacaoVotacao
    {
        // Votação
        RegistroVoto,
        AnulacaoVoto,
        ValidacaoEleitor,
        GeracaoComprovante,
        
        // Sessão
        AberturaSessao,
        EncerramentoSessao,
        SuspensaoSessao,
        ReaberturaSessao,
        
        // Apuração
        InicioApuracao,
        AtualizacaoContagem,
        FinalizacaoApuracao,
        Recontagem,
        
        // Resultado
        GeracaoBoletim,
        PublicacaoResultado,
        AlteracaoResultado,
        ContestacaoResultado,
        
        // Segurança
        AssinaturaDigital,
        VerificacaoIntegridade,
        AuditoriaSeguranca,
        
        // Sistema
        LoginSistema,
        LogoutSistema,
        AlteracaoPermissao,
        BackupDados,
        
        // Outros
        ConsultaDados,
        ExportacaoDados,
        ImportacaoDados,
        ManutencaoSistema
    }
    
    /// <summary>
    /// Status da operação
    /// </summary>
    public enum StatusOperacao
    {
        Iniciada,
        EmAndamento,
        Concluida,
        Falha,
        Cancelada,
        Rejeitada,
        Timeout
    }
    
    /// <summary>
    /// Nível de segurança da operação
    /// </summary>
    public enum NivelSeguranca
    {
        Baixo,
        Normal,
        Alto,
        Critico
    }
    
    /// <summary>
    /// Tipo de agregação de logs
    /// </summary>
    public enum TipoAgregacao
    {
        Horaria,
        Diaria,
        Semanal,
        Mensal,
        PorSessao,
        PorEleicao
    }
}