using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de gestão de profissionais
    /// </summary>
    public interface IProfissionalService
    {
        Task<Profissional> BuscarPorRegistroCAUAsync(string registroCAU);
        Task<Profissional> BuscarPorCPFAsync(string cpf);
        Task<List<Profissional>> ListarProfissionaisAsync(FiltroProfissionalDTO filtro);
        Task<Profissional> AtualizarDadosAsync(int id, AtualizarProfissionalDTO dto);
        Task<ValidacaoElegibilidadeResultDTO> VerificarElegibilidadeAsync(int profissionalId, int calendarioId);
        Task<Profissional> SincronizarComCorporativoAsync(string registroCAU);
        Task<HistoricoProfissionalDTO> ObterHistoricoAsync(int profissionalId);
        Task<List<PendenciaProfissionalDTO>> ObterPendenciasAsync(int profissionalId);
    }

    // DTOs
    public class FiltroProfissionalDTO
    {
        public string? Nome { get; set; }
        public int? UfId { get; set; }
        public StatusRegistroProfissional? StatusRegistro { get; set; }
        public bool? PodeVotar { get; set; }
        public bool? EhElegivel { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }

    public class AtualizarProfissionalDTO
    {
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Celular { get; set; }
        public string? Endereco { get; set; }
        public string? Cidade { get; set; }
        public string? CEP { get; set; }
    }

    public class HistoricoProfissionalDTO
    {
        public int ProfissionalId { get; set; }
        public string NomeProfissional { get; set; }
        public string RegistroCAU { get; set; }
        public List<ParticipacaoChapaDTO> ParticipacoesChapas { get; set; }
        public int DenunciasRecebidas { get; set; }
        public int EleicoesVotadas { get; set; }
        public DateTime? DataPrimeiraParticipacao { get; set; }
        public DateTime? DataUltimaParticipacao { get; set; }
    }

    public class ParticipacaoChapaDTO
    {
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public int Ano { get; set; }
        public string Cargo { get; set; }
        public string Status { get; set; }
    }

    public class PendenciaProfissionalDTO
    {
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public bool Impeditiva { get; set; }
        public DateTime DataDeteccao { get; set; }
        public DateTime? DataResolucao { get; set; }
    }

    public class ValidacaoElegibilidadeResultDTO
    {
        public bool EhElegivel { get; set; }
        public List<string> MotivosPendencias { get; set; }
        public DateTime DataValidacao { get; set; }
    }
}