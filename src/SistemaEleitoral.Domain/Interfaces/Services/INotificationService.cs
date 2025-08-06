using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de notificações
    /// </summary>
    public interface INotificationService
    {
        Task EnviarNotificacaoAsync(NotificacaoModel notificacao);
        Task EnviarNotificacaoLoteAsync(List<NotificacaoModel> notificacoes);
        Task EnviarEmailAsync(EmailModel email);
        Task EnviarEmailLoteAsync(List<EmailModel> emails);
        Task AgendarNotificacaoAsync(NotificacaoModel notificacao, DateTime dataEnvio);
    }
    
    public class NotificacaoModel
    {
        public TipoNotificacao Tipo { get; set; }
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public int? CalendarioId { get; set; }
        public int? ChapaId { get; set; }
        public int? DenunciaId { get; set; }
        public List<int> DestinatariosIds { get; set; }
        public Dictionary<string, string> Parametros { get; set; }
    }
    
    public class EmailModel
    {
        public string De { get; set; }
        public List<string> Para { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Cco { get; set; }
        public string Assunto { get; set; }
        public string CorpoHtml { get; set; }
        public string CorpoTexto { get; set; }
        public List<AnexoEmail> Anexos { get; set; }
        public string TemplateId { get; set; }
        public Dictionary<string, string> ParametrosTemplate { get; set; }
    }
    
    public class AnexoEmail
    {
        public string Nome { get; set; }
        public byte[] Conteudo { get; set; }
        public string TipoMime { get; set; }
    }
    
    public enum TipoNotificacao
    {
        CalendarioPublicado,
        ChapaCriada,
        ChapaConfirmada,
        MembroAdicionadoChapa,
        PrazoExpirando,
        DenunciaRecebida,
        ImpugnacaoRecebida,
        JulgamentoRealizado,
        VotacaoIniciada,
        VotacaoEncerrada,
        ResultadoPublicado
    }
}