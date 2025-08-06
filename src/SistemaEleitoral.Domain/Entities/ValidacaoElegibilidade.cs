using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    public class ValidacaoElegibilidade : AuditableEntity
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public int? MembroChapaId { get; set; }
        public DateTime DataValidacao { get; set; }
        public bool Elegivel { get; set; }
        public StatusValidacao Status { get; set; } = StatusValidacao.Pendente;
        public string ResultadoJson { get; set; } // JSON with all validation results
        public DateTime? DataExpiracao { get; set; }
        
        // Individual validation results
        public bool SituacaoFinanceiraRegular { get; set; }
        public bool SituacaoEticaRegular { get; set; }
        public bool RegistroAtivo { get; set; }
        public bool TempoMinimoFormacao { get; set; }
        public int AnosFormacao { get; set; }
        public bool SemPenalidadesEticas { get; set; }
        public bool SemDebitosFinanceiros { get; set; }
        public bool RegistroRegular { get; set; }
        public bool NaoParticipouOutraChapa { get; set; }
        public bool NaoMembroComissaoEleitoral { get; set; }
        public bool ResideNaUF { get; set; }
        public bool AtendeIdadeMaxima { get; set; }
        public bool AtendeIdadeMinima { get; set; }
        
        public string MotivosInelegibilidade { get; set; }
        public string ObservacoesValidacao { get; set; }
        
        // Navigation Properties
        public virtual Profissional Profissional { get; set; }
        public virtual MembroChapa MembroChapa { get; set; }

        // Business Methods
        public void ExecutarValidacaoCompleta(Profissional profissional, ValidacaoElegibilidadeConfig config)
        {
            DataValidacao = DateTime.UtcNow;
            Status = StatusValidacao.EmAnalise;
            var motivos = new List<string>();

            // Rule 1: Financial situation
            SituacaoFinanceiraRegular = profissional.AdimplenteSituacaoFinanceira;
            if (!SituacaoFinanceiraRegular)
                motivos.Add("Situação financeira irregular");

            // Rule 2: Ethical situation
            SituacaoEticaRegular = profissional.AdimplenteSituacaoEtica;
            if (!SituacaoEticaRegular)
                motivos.Add("Situação ética irregular");

            // Rule 3: Active registration
            RegistroAtivo = profissional.RegistroAtivo;
            if (!RegistroAtivo)
                motivos.Add("Registro profissional inativo");

            // Rule 4: Minimum years since graduation
            if (profissional.DataFormatura.HasValue)
            {
                AnosFormacao = (int)((DateTime.Now - profissional.DataFormatura.Value).TotalDays / 365);
                TempoMinimoFormacao = AnosFormacao >= config.AnosMinimoFormacao;
                if (!TempoMinimoFormacao)
                    motivos.Add($"Tempo de formação insuficiente ({AnosFormacao} anos, mínimo: {config.AnosMinimoFormacao})");
            }
            else
            {
                TempoMinimoFormacao = false;
                motivos.Add("Data de formatura não informada");
            }

            // Rule 5: No ethical penalties
            SemPenalidadesEticas = true; // Would check against penalty database
            if (!SemPenalidadesEticas)
                motivos.Add("Possui penalidades éticas vigentes");

            // Rule 6: No financial debts
            SemDebitosFinanceiros = profissional.AdimplenteSituacaoFinanceira;
            if (!SemDebitosFinanceiros)
                motivos.Add("Possui débitos financeiros");

            // Rule 7: Regular registration
            RegistroRegular = !string.IsNullOrEmpty(profissional.NumeroRegistro) && RegistroAtivo;
            if (!RegistroRegular)
                motivos.Add("Registro irregular");

            // Rule 8: Not participating in another chapa
            NaoParticipouOutraChapa = true; // Would check against other chapas
            if (!NaoParticipouOutraChapa)
                motivos.Add("Já participa de outra chapa");

            // Rule 9: Not member of electoral commission
            NaoMembroComissaoEleitoral = true; // Would check against commission members
            if (!NaoMembroComissaoEleitoral)
                motivos.Add("É membro da comissão eleitoral");

            // Rule 10: Resides in the state (for state chapas)
            ResideNaUF = profissional.UfRegistro == profissional.Estado;
            if (config.RequerResidenciaUF && !ResideNaUF)
                motivos.Add($"Não reside na UF da chapa ({profissional.Estado})");

            // Rule 11: Maximum age
            if (profissional.DataNascimento.HasValue && config.IdadeMaxima.HasValue)
            {
                var idade = (int)((DateTime.Now - profissional.DataNascimento.Value).TotalDays / 365);
                AtendeIdadeMaxima = idade <= config.IdadeMaxima.Value;
                if (!AtendeIdadeMaxima)
                    motivos.Add($"Idade acima do máximo permitido ({idade} anos, máximo: {config.IdadeMaxima})");
            }
            else
            {
                AtendeIdadeMaxima = true;
            }

            // Rule 12: Minimum age
            if (profissional.DataNascimento.HasValue && config.IdadeMinima.HasValue)
            {
                var idade = (int)((DateTime.Now - profissional.DataNascimento.Value).TotalDays / 365);
                AtendeIdadeMinima = idade >= config.IdadeMinima.Value;
                if (!AtendeIdadeMinima)
                    motivos.Add($"Idade abaixo do mínimo permitido ({idade} anos, mínimo: {config.IdadeMinima})");
            }
            else
            {
                AtendeIdadeMinima = true;
            }

            // Final result
            Elegivel = motivos.Count == 0;
            MotivosInelegibilidade = string.Join("; ", motivos);
            Status = StatusValidacao.Validado;
            DataExpiracao = DateTime.UtcNow.AddDays(30); // Validation valid for 30 days

            // Create result JSON
            var resultado = new
            {
                DataValidacao,
                Elegivel,
                Motivos = motivos,
                Detalhes = new
                {
                    SituacaoFinanceiraRegular,
                    SituacaoEticaRegular,
                    RegistroAtivo,
                    TempoMinimoFormacao,
                    AnosFormacao,
                    SemPenalidadesEticas,
                    SemDebitosFinanceiros,
                    RegistroRegular,
                    NaoParticipouOutraChapa,
                    NaoMembroComissaoEleitoral,
                    ResideNaUF,
                    AtendeIdadeMaxima,
                    AtendeIdadeMinima
                }
            };

            ResultadoJson = System.Text.Json.JsonSerializer.Serialize(resultado);
        }

        public bool IsValidacaoValida()
        {
            return Status == StatusValidacao.Validado && 
                   (!DataExpiracao.HasValue || DateTime.UtcNow <= DataExpiracao.Value);
        }

        public void InvalidarValidacao(string motivo)
        {
            Status = StatusValidacao.Invalido;
            ObservacoesValidacao = motivo;
            DataExpiracao = DateTime.UtcNow;
        }
    }

    public class ValidacaoElegibilidadeConfig
    {
        public int AnosMinimoFormacao { get; set; } = 3;
        public int AnosMinimoCordenador { get; set; } = 5;
        public int AnosMinimoViceCoordenador { get; set; } = 4;
        public bool RequerResidenciaUF { get; set; } = false;
        public int? IdadeMinima { get; set; }
        public int? IdadeMaxima { get; set; }
        public bool PermitirInadimplente { get; set; } = false;
        public bool PermitirComPenalidade { get; set; } = false;
        public int DiasValidadeValidacao { get; set; } = 30;
    }
}