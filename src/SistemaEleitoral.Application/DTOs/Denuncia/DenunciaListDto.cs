using SistemaEleitoral.Domain.Enums;
using System;

namespace SistemaEleitoral.Application.DTOs.Denuncia
{
    /// <summary>
    /// DTO para listagem de denúncias
    /// </summary>
    public class DenunciaListDto
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public DateTime DataHoraDenuncia { get; set; }
        public StatusDenuncia Status { get; set; }
        public string StatusDescricao => Status.ToString();
        public string DenuncianteName { get; set; }
        public string TipoDenunciaDescricao { get; set; }
        public string FilialNome { get; set; }
        public bool TemSigilo { get; set; }
        public DateTime? PrazoDefesa { get; set; }
        public DateTime? PrazoRecurso { get; set; }
        public bool PrazoVencido { get; set; }
        public string RelatorNome { get; set; }
    }
}