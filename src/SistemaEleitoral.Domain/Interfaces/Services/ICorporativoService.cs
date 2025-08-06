using System;
using System.Threading.Tasks;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para integração com sistema corporativo do CAU
    /// </summary>
    public interface ICorporativoService
    {
        Task<SituacaoFinanceiraDTO> ConsultarSituacaoFinanceiraAsync(int profissionalId);
        Task<bool> VerificarRegistroAtivoAsync(int profissionalId);
        Task<DadosProfissionalDTO> ObterDadosProfissionalAsync(int profissionalId);
        Task<bool> ValidarCPFAsync(string cpf);
        Task<EnderecoDTO> ConsultarEnderecoPorCEPAsync(string cep);
    }
    
    public class SituacaoFinanceiraDTO
    {
        public bool Adimplente { get; set; }
        public decimal ValorDebito { get; set; }
        public bool TemParcelamento { get; set; }
        public bool TemParcelamentoAtraso { get; set; }
        public DateTime? DataUltimoPagamento { get; set; }
    }
    
    public class DadosProfissionalDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string RegistroProfissional { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public DateTime? DataRegistro { get; set; }
        public string StatusRegistro { get; set; }
    }
    
    public class EnderecoDTO
    {
        public string Logradouro { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Cep { get; set; }
    }
}