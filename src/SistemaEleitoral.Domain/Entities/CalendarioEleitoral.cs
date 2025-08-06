using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class CalendarioEleitoral : AuditableEntity
    {
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public string Atividade { get; set; }
        public string Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFim { get; set; }
        public bool Ativo { get; set; } = true;
        public int? OrdemExecucao { get; set; }
        public string TipoAtividade { get; set; } // CADASTRO_CHAPAS, IMPUGNACAO, JULGAMENTO, VOTACAO, etc.
        public bool RequerValidacao { get; set; }
        public bool PermiteExtensao { get; set; }
        public int? DiasExtensao { get; set; }
        public DateTime? DataExtensao { get; set; }
        public string MotivoExtensao { get; set; }
        public bool Obrigatorio { get; set; } = true;
        public string ConfiguracaoJson { get; set; } // Additional configuration as JSON
        public string DependenciasJson { get; set; } // Dependencies on other activities
        public bool BloqueiaOutrasAtividades { get; set; }
        public string AtividadesBloqueadasJson { get; set; } // List of blocked activities
        
        // Navigation Properties
        public virtual Eleicao Eleicao { get; set; }
        public virtual ICollection<HistoricoWorkflow> HistoricosWorkflow { get; set; } = new List<HistoricoWorkflow>();
        public virtual ICollection<NotificacaoEleitoral> Notificacoes { get; set; } = new List<NotificacaoEleitoral>();

        // Business Methods
        public bool IsAtividadeAtiva()
        {
            var agora = DateTime.Now;
            var dataInicioCompleta = ObterDataInicioCompleta();
            var dataFimCompleta = ObterDataFimCompleta();

            return Ativo && agora >= dataInicioCompleta && agora <= dataFimCompleta;
        }

        public bool IsAtividadeFutura()
        {
            return DateTime.Now < ObterDataInicioCompleta();
        }

        public bool IsAtividadePassada()
        {
            return DateTime.Now > ObterDataFimCompleta();
        }

        public DateTime ObterDataInicioCompleta()
        {
            var data = DataInicio;
            if (HoraInicio.HasValue)
                data = DataInicio.Date.Add(HoraInicio.Value);
            return data;
        }

        public DateTime ObterDataFimCompleta()
        {
            var data = DataFim;
            if (HoraFim.HasValue)
                data = DataFim.Date.Add(HoraFim.Value);
            if (DataExtensao.HasValue)
                data = DataExtensao.Value;
            return data;
        }

        public bool PodeExecutarAtividade(string tipoAtividade)
        {
            return IsAtividadeAtiva() && TipoAtividade == tipoAtividade;
        }

        public void EstenderPrazo(int dias, string motivo)
        {
            if (!PermiteExtensao)
                throw new BusinessException("Esta atividade não permite extensão de prazo");

            if (DiasExtensao.HasValue && dias > DiasExtensao.Value)
                throw new BusinessException($"Extensão máxima permitida é de {DiasExtensao.Value} dias");

            DataExtensao = DataFim.AddDays(dias);
            MotivoExtensao = motivo;
        }

        public void Ativar()
        {
            Ativo = true;
        }

        public void Desativar()
        {
            Ativo = false;
        }

        public int? ObterDiasRestantes()
        {
            if (IsAtividadePassada())
                return 0;

            if (IsAtividadeFutura())
                return null;

            var dataFim = ObterDataFimCompleta();
            var diasRestantes = (dataFim - DateTime.Now).Days;
            return diasRestantes < 0 ? 0 : diasRestantes;
        }

        public int? ObterHorasRestantes()
        {
            if (IsAtividadePassada())
                return 0;

            if (IsAtividadeFutura())
                return null;

            var dataFim = ObterDataFimCompleta();
            var horasRestantes = (int)(dataFim - DateTime.Now).TotalHours;
            return horasRestantes < 0 ? 0 : horasRestantes;
        }

        public bool TemDependenciasPendentes(List<CalendarioEleitoral> outrasAtividades)
        {
            if (string.IsNullOrEmpty(DependenciasJson))
                return false;

            try
            {
                var dependencias = System.Text.Json.JsonSerializer.Deserialize<List<string>>(DependenciasJson);
                foreach (var dep in dependencias)
                {
                    var atividadeDependente = outrasAtividades.Find(a => a.TipoAtividade == dep);
                    if (atividadeDependente != null && !atividadeDependente.IsAtividadePassada())
                        return true;
                }
            }
            catch { }

            return false;
        }

        public bool BloqueiaAtividade(string tipoAtividade)
        {
            if (!BloqueiaOutrasAtividades || string.IsNullOrEmpty(AtividadesBloqueadasJson))
                return false;

            try
            {
                var atividadesBloqueadas = System.Text.Json.JsonSerializer.Deserialize<List<string>>(AtividadesBloqueadasJson);
                return atividadesBloqueadas.Contains(tipoAtividade);
            }
            catch
            {
                return false;
            }
        }
    }

    public class HistoricoWorkflow : AuditableEntity
    {
        public int Id { get; set; }
        public string TipoEntidade { get; set; } // CHAPA, DENUNCIA, IMPUGNACAO, etc.
        public int EntidadeId { get; set; }
        public string StatusAnterior { get; set; }
        public string StatusNovo { get; set; }
        public string Acao { get; set; }
        public string Descricao { get; set; }
        public string UsuarioResponsavel { get; set; }
        public DateTime DataAcao { get; set; }
        public string DadosAdicionaisJson { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        
        // Navigation Properties
        public virtual CalendarioEleitoral CalendarioEleitoral { get; set; }
    }
}