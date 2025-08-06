namespace SistemaEleitoral.Domain.Entities
{
    public class EmailLog
    {
        public int Id { get; set; }
        public int? ChapaId { get; set; }
        public string TipoEmail { get; set; } = "";
        public DateTime DataEnvio { get; set; }
        public bool Sucesso { get; set; }
        public int QuantidadeDestinatarios { get; set; }
        public string? MensagemErro { get; set; }
    }

    public class Notificacao
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; }
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public string Tipo { get; set; }
        public bool Lida { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataLeitura { get; set; }
        
        public virtual ApplicationUser Usuario { get; set; }
    }
}