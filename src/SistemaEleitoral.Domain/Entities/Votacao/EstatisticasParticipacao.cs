using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Estatísticas detalhadas de participação na eleição
    /// </summary>
    public class EstatisticasParticipacao : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public int EleicaoId { get; set; }
        public int? UfId { get; set; }
        public DateTime DataGeracao { get; set; }
        public TipoEstatistica TipoEstatistica { get; set; }
        
        // Participação Geral
        public int TotalEleitoresAptos { get; set; }
        public int TotalVotantes { get; set; }
        public int TotalAbstencoes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        
        // Por Categoria Profissional
        public int TotalArquitetos { get; set; }
        public int TotalUrbanistas { get; set; }
        public int TotalArquitetosUrbanistas { get; set; }
        public int VotantesArquitetos { get; set; }
        public int VotantesUrbanistas { get; set; }
        public int VotantesArquitetosUrbanistas { get; set; }
        
        // Por Faixa Etária
        public string DistribuicaoFaixaEtariaJson { get; set; } // JSON com dados por faixa etária
        public int VotantesMenos30 { get; set; }
        public int Votantes30a40 { get; set; }
        public int Votantes40a50 { get; set; }
        public int Votantes50a60 { get; set; }
        public int VotantesMais60 { get; set; }
        
        // Por Gênero
        public int VotantesMasculino { get; set; }
        public int VotantesFeminino { get; set; }
        public int VotantesOutroGenero { get; set; }
        public decimal PercentualMasculino { get; set; }
        public decimal PercentualFeminino { get; set; }
        
        // Por UF/Região
        public string DistribuicaoRegionalJson { get; set; } // JSON com dados por região
        public string UfMaiorParticipacao { get; set; }
        public string UfMenorParticipacao { get; set; }
        public decimal MediaParticipacaoRegional { get; set; }
        
        // Temporal
        public string DistribuicaoHorariaJson { get; set; } // JSON com votos por hora
        public DateTime HorarioPicoVotacao { get; set; }
        public int VotosHorarioPico { get; set; }
        public decimal VelocidadeMediaVotacao { get; set; } // Votos por minuto
        
        // Comparativo Histórico
        public decimal VariacaoEleicaoAnterior { get; set; }
        public int RankingParticipacao { get; set; } // Posição histórica
        public bool RecordeParticipacao { get; set; }
        public string ComparativoHistoricoJson { get; set; } // JSON com dados históricos
        
        // Análise de Tendências
        public decimal TendenciaParticipacao { get; set; } // Projeção baseada em dados parciais
        public string PadraoVotacaoIdentificado { get; set; }
        public bool AnomaliaDetectada { get; set; }
        public string DescricaoAnomalia { get; set; }
        
        // Navegação
        public virtual ResultadoApuracaoAvancado ResultadoApuracao { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        public virtual Uf Uf { get; set; }
        
        // Construtor
        public EstatisticasParticipacao()
        {
            DataGeracao = DateTime.UtcNow;
            TipoEstatistica = TipoEstatistica.Parcial;
            RecordeParticipacao = false;
            AnomaliaDetectada = false;
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Calcula estatísticas de participação
        /// </summary>
        public void CalcularEstatisticas(List<VotoEleitoral> votos, List<EleitorApto> eleitores)
        {
            if (votos == null || eleitores == null)
                return;
            
            // Totais gerais
            TotalEleitoresAptos = eleitores.Count;
            TotalVotantes = votos.Count;
            TotalAbstencoes = TotalEleitoresAptos - TotalVotantes;
            
            // Percentuais
            if (TotalEleitoresAptos > 0)
            {
                PercentualParticipacao = (decimal)TotalVotantes / TotalEleitoresAptos * 100;
                PercentualAbstencao = (decimal)TotalAbstencoes / TotalEleitoresAptos * 100;
            }
            
            // Calcular por categoria
            CalcularPorCategoria(votos, eleitores);
            
            // Calcular por faixa etária
            CalcularPorFaixaEtaria(votos, eleitores);
            
            // Calcular por gênero
            CalcularPorGenero(votos, eleitores);
            
            // Calcular distribuição temporal
            CalcularDistribuicaoTemporal(votos);
            
            // Identificar padrões
            IdentificarPadroes();
        }
        
        /// <summary>
        /// Calcula estatísticas por categoria profissional
        /// </summary>
        private void CalcularPorCategoria(List<VotoEleitoral> votos, List<EleitorApto> eleitores)
        {
            // Simular categorias baseado em dados disponíveis
            // Em produção, usar dados reais do cadastro profissional
            
            TotalArquitetos = eleitores.Count(e => e.NumeroRegistro.StartsWith("A"));
            TotalUrbanistas = eleitores.Count(e => e.NumeroRegistro.StartsWith("U"));
            TotalArquitetosUrbanistas = eleitores.Count - TotalArquitetos - TotalUrbanistas;
            
            // Contar votantes por categoria (simplificado)
            VotantesArquitetos = (int)(TotalArquitetos * (PercentualParticipacao / 100));
            VotantesUrbanistas = (int)(TotalUrbanistas * (PercentualParticipacao / 100));
            VotantesArquitetosUrbanistas = TotalVotantes - VotantesArquitetos - VotantesUrbanistas;
        }
        
        /// <summary>
        /// Calcula estatísticas por faixa etária
        /// </summary>
        private void CalcularPorFaixaEtaria(List<VotoEleitoral> votos, List<EleitorApto> eleitores)
        {
            var distribuicao = new Dictionary<string, int>
            {
                ["Menos de 30"] = 0,
                ["30-40"] = 0,
                ["40-50"] = 0,
                ["50-60"] = 0,
                ["Mais de 60"] = 0
            };
            
            // Simular distribuição (em produção, usar data de nascimento real)
            var random = new Random();
            foreach (var voto in votos)
            {
                var faixa = random.Next(0, 5);
                switch (faixa)
                {
                    case 0:
                        VotantesMenos30++;
                        distribuicao["Menos de 30"]++;
                        break;
                    case 1:
                        Votantes30a40++;
                        distribuicao["30-40"]++;
                        break;
                    case 2:
                        Votantes40a50++;
                        distribuicao["40-50"]++;
                        break;
                    case 3:
                        Votantes50a60++;
                        distribuicao["50-60"]++;
                        break;
                    case 4:
                        VotantesMais60++;
                        distribuicao["Mais de 60"]++;
                        break;
                }
            }
            
            DistribuicaoFaixaEtariaJson = System.Text.Json.JsonSerializer.Serialize(distribuicao);
        }
        
        /// <summary>
        /// Calcula estatísticas por gênero
        /// </summary>
        private void CalcularPorGenero(List<VotoEleitoral> votos, List<EleitorApto> eleitores)
        {
            // Simular distribuição (em produção, usar dados reais)
            VotantesMasculino = (int)(TotalVotantes * 0.55);
            VotantesFeminino = (int)(TotalVotantes * 0.44);
            VotantesOutroGenero = TotalVotantes - VotantesMasculino - VotantesFeminino;
            
            if (TotalVotantes > 0)
            {
                PercentualMasculino = (decimal)VotantesMasculino / TotalVotantes * 100;
                PercentualFeminino = (decimal)VotantesFeminino / TotalVotantes * 100;
            }
        }
        
        /// <summary>
        /// Calcula distribuição temporal dos votos
        /// </summary>
        private void CalcularDistribuicaoTemporal(List<VotoEleitoral> votos)
        {
            if (votos == null || !votos.Any())
                return;
            
            var distribuicaoHoraria = votos
                .GroupBy(v => v.DataHoraVoto.Hour)
                .ToDictionary(g => g.Key.ToString("00") + ":00", g => g.Count());
            
            DistribuicaoHorariaJson = System.Text.Json.JsonSerializer.Serialize(distribuicaoHoraria);
            
            // Identificar horário de pico
            var pico = distribuicaoHoraria.OrderByDescending(d => d.Value).FirstOrDefault();
            if (pico.Key != null)
            {
                HorarioPicoVotacao = DateTime.Today.AddHours(int.Parse(pico.Key.Substring(0, 2)));
                VotosHorarioPico = pico.Value;
            }
            
            // Calcular velocidade média
            if (votos.Any())
            {
                var duracao = votos.Max(v => v.DataHoraVoto) - votos.Min(v => v.DataHoraVoto);
                if (duracao.TotalMinutes > 0)
                {
                    VelocidadeMediaVotacao = (decimal)(votos.Count / duracao.TotalMinutes);
                }
            }
        }
        
        /// <summary>
        /// Atualiza estatísticas regionais
        /// </summary>
        public void AtualizarEstatisticasRegionais(Dictionary<string, ParticipacaoRegional> dadosRegionais)
        {
            if (dadosRegionais == null || !dadosRegionais.Any())
                return;
            
            DistribuicaoRegionalJson = System.Text.Json.JsonSerializer.Serialize(dadosRegionais);
            
            // Identificar UF com maior e menor participação
            var ordenado = dadosRegionais.OrderByDescending(d => d.Value.PercentualParticipacao).ToList();
            
            if (ordenado.Any())
            {
                UfMaiorParticipacao = ordenado.First().Key;
                UfMenorParticipacao = ordenado.Last().Key;
                MediaParticipacaoRegional = ordenado.Average(d => d.Value.PercentualParticipacao);
            }
        }
        
        /// <summary>
        /// Compara com eleições anteriores
        /// </summary>
        public void CompararComHistorico(List<EstatisticasParticipacao> historico)
        {
            if (historico == null || !historico.Any())
                return;
            
            // Calcular variação com eleição anterior
            var eleicaoAnterior = historico.OrderByDescending(h => h.DataGeracao).FirstOrDefault();
            if (eleicaoAnterior != null)
            {
                VariacaoEleicaoAnterior = PercentualParticipacao - eleicaoAnterior.PercentualParticipacao;
            }
            
            // Determinar ranking histórico
            var todasEleicoes = historico.Concat(new[] { this })
                .OrderByDescending(e => e.PercentualParticipacao)
                .ToList();
            
            RankingParticipacao = todasEleicoes.IndexOf(this) + 1;
            
            // Verificar se é recorde
            RecordeParticipacao = RankingParticipacao == 1;
            
            // Gerar comparativo
            var comparativo = historico.Select(h => new
            {
                Ano = h.DataGeracao.Year,
                Participacao = h.PercentualParticipacao,
                TotalVotantes = h.TotalVotantes
            });
            
            ComparativoHistoricoJson = System.Text.Json.JsonSerializer.Serialize(comparativo);
        }
        
        /// <summary>
        /// Projeta tendência de participação
        /// </summary>
        public void ProjetarTendencia(decimal percentualApurado)
        {
            if (percentualApurado <= 0 || percentualApurado >= 100)
                return;
            
            // Projeção simples baseada em dados parciais
            TendenciaParticipacao = PercentualParticipacao * (100 / percentualApurado);
            
            // Ajustar baseado em padrões históricos
            if (HorarioPicoVotacao.Hour < 12)
            {
                // Votação matutina tende a ser menor
                TendenciaParticipacao *= 1.1m;
            }
            else if (HorarioPicoVotacao.Hour > 16)
            {
                // Votação vespertina tende a ser maior
                TendenciaParticipacao *= 0.95m;
            }
        }
        
        /// <summary>
        /// Identifica padrões e anomalias
        /// </summary>
        private void IdentificarPadroes()
        {
            var padroes = new List<string>();
            
            // Verificar participação
            if (PercentualParticipacao > 80)
            {
                padroes.Add("Alta participação");
            }
            else if (PercentualParticipacao < 40)
            {
                padroes.Add("Baixa participação");
            }
            
            // Verificar distribuição de gênero
            if (Math.Abs(PercentualMasculino - PercentualFeminino) > 20)
            {
                padroes.Add("Desbalanceamento de gênero significativo");
            }
            
            // Verificar velocidade de votação
            if (VelocidadeMediaVotacao > 100)
            {
                padroes.Add("Votação muito rápida");
                AnomaliaDetectada = true;
                DescricaoAnomalia = "Velocidade de votação anormalmente alta";
            }
            
            // Verificar horário de pico
            if (HorarioPicoVotacao.Hour < 8 || HorarioPicoVotacao.Hour > 18)
            {
                padroes.Add("Horário de pico incomum");
            }
            
            PadraoVotacaoIdentificado = string.Join(", ", padroes);
        }
        
        /// <summary>
        /// Finaliza as estatísticas
        /// </summary>
        public void Finalizar()
        {
            TipoEstatistica = TipoEstatistica.Final;
            DataGeracao = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Obtém resumo das estatísticas
        /// </summary>
        public ResumoEstatisticas ObterResumo()
        {
            return new ResumoEstatisticas
            {
                TotalEleitoresAptos = TotalEleitoresAptos,
                TotalVotantes = TotalVotantes,
                PercentualParticipacao = PercentualParticipacao,
                UfMaiorParticipacao = UfMaiorParticipacao,
                UfMenorParticipacao = UfMenorParticipacao,
                HorarioPico = HorarioPicoVotacao.ToString("HH:mm"),
                VelocidadeMedia = VelocidadeMediaVotacao,
                RecordeHistorico = RecordeParticipacao,
                TendenciaProjetada = TendenciaParticipacao,
                AnomaliaDetectada = AnomaliaDetectada
            };
        }
    }
    
    /// <summary>
    /// Tipo de estatística
    /// </summary>
    public enum TipoEstatistica
    {
        Parcial,
        Final,
        Projecao,
        Historica
    }
    
    /// <summary>
    /// Dados de participação regional
    /// </summary>
    public class ParticipacaoRegional
    {
        public string Uf { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public decimal PercentualParticipacao { get; set; }
    }
    
    /// <summary>
    /// Resumo das estatísticas
    /// </summary>
    public class ResumoEstatisticas
    {
        public int TotalEleitoresAptos { get; set; }
        public int TotalVotantes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public string UfMaiorParticipacao { get; set; }
        public string UfMenorParticipacao { get; set; }
        public string HorarioPico { get; set; }
        public decimal VelocidadeMedia { get; set; }
        public bool RecordeHistorico { get; set; }
        public decimal TendenciaProjetada { get; set; }
        public bool AnomaliaDetectada { get; set; }
    }
}