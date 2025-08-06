using System;
using System.Collections.Generic;

namespace Eleitoral.Application.DTOs.Apuracao
{
    /// <summary>
    /// DTO para estatísticas de apuração
    /// </summary>
    public class EstatisticasApuracaoDto
    {
        // Estatísticas de participação
        public int TotalEleitoresAptos { get; set; }
        public int TotalComparecimento { get; set; }
        public int TotalAbstencoes { get; set; }
        public decimal PercentualComparecimento { get; set; }
        public decimal PercentualAbstencao { get; set; }
        
        // Estatísticas de votos
        public int VotosValidos { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosAnulados { get; set; }
        public decimal PercentualVotosValidos { get; set; }
        public decimal PercentualVotosBrancos { get; set; }
        public decimal PercentualVotosNulos { get; set; }
        
        // Estatísticas de urnas
        public int TotalUrnas { get; set; }
        public int UrnasProcessadas { get; set; }
        public int UrnasPendentes { get; set; }
        public int UrnasComProblema { get; set; }
        public decimal PercentualUrnasProcessadas { get; set; }
        
        // Estatísticas temporais
        public DateTime? InicioVotacao { get; set; }
        public DateTime? FimVotacao { get; set; }
        public TimeSpan? DuracaoVotacao { get; set; }
        public int PicoVotacaoHora { get; set; }
        public DateTime? HoraPicoVotacao { get; set; }
        
        // Estatísticas regionais
        public List<EstatisticaRegionalDto> EstatisticasRegionais { get; set; }
        
        // Dados de auditoria
        public DateTime DataGeracao { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
        
        public EstatisticasApuracaoDto()
        {
            EstatisticasRegionais = new List<EstatisticaRegionalDto>();
        }
    }
    
    /// <summary>
    /// DTO para estatística regional
    /// </summary>
    public class EstatisticaRegionalDto
    {
        public string Regiao { get; set; }
        public string UF { get; set; }
        public string Cidade { get; set; }
        
        public int EleitoresAptos { get; set; }
        public int Comparecimento { get; set; }
        public int Abstencoes { get; set; }
        
        public int VotosValidos { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        
        public decimal PercentualComparecimento { get; set; }
        public decimal PercentualAbstencao { get; set; }
        
        public int TotalUrnas { get; set; }
        public int UrnasProcessadas { get; set; }
        
        public List<ResultadoChapaRegionalDto> ResultadosChapas { get; set; }
        
        public EstatisticaRegionalDto()
        {
            ResultadosChapas = new List<ResultadoChapaRegionalDto>();
        }
    }
    
    /// <summary>
    /// DTO para resultado de chapa por região
    /// </summary>
    public class ResultadoChapaRegionalDto
    {
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public int NumeroChapa { get; set; }
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
    }
}