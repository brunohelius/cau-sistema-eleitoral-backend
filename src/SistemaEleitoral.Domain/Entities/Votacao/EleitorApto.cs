using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Representa um eleitor apto a votar com validações e controles
    /// </summary>
    public class EleitorApto : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public int EleicaoId { get; set; }
        public string Cpf { get; set; }
        public string NumeroRegistro { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public int? UfId { get; set; }
        
        // Status e Situação
        public SituacaoEleitor Situacao { get; set; }
        public bool JaVotou { get; set; }
        public DateTime? DataHoraVoto { get; set; }
        public bool TemImpedimento { get; set; }
        public string MotivoImpedimento { get; set; }
        
        // Controle de Acesso
        public string TokenVotacao { get; private set; }
        public DateTime? TokenExpiracao { get; private set; }
        public int TentativasAcesso { get; set; }
        public DateTime? UltimaTentativa { get; set; }
        public bool Suspenso { get; set; }
        public DateTime? DataSuspensao { get; set; }
        public string MotivoSuspensao { get; set; }
        
        // Verificações
        public DateTime DataVerificacao { get; set; }
        public string VerificadoPor { get; set; }
        public bool AdimplenteCau { get; set; }
        public bool RegistroAtivo { get; set; }
        public DateTime? DataUltimaAtualizacao { get; set; }
        
        // Navegação
        public virtual Profissional Profissional { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        public virtual Uf Uf { get; set; }
        public virtual ICollection<HistoricoVerificacaoEleitor> Historicos { get; set; }
        
        // Construtor
        public EleitorApto()
        {
            Situacao = SituacaoEleitor.Pendente;
            TentativasAcesso = 0;
            JaVotou = false;
            Suspenso = false;
            TemImpedimento = false;
            DataVerificacao = DateTime.UtcNow;
            Historicos = new List<HistoricoVerificacaoEleitor>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Verifica se o eleitor está apto a votar
        /// </summary>
        public async Task<(bool apto, string motivo)> VerificarAptidaoAsync()
        {
            // Resetar tentativas se passou mais de 1 hora
            if (UltimaTentativa.HasValue && 
                (DateTime.UtcNow - UltimaTentativa.Value).TotalHours > 1)
            {
                TentativasAcesso = 0;
            }
            
            // Verificar suspensão
            if (Suspenso)
            {
                return (false, $"Eleitor suspenso: {MotivoSuspensao}");
            }
            
            // Verificar se já votou
            if (JaVotou)
            {
                return (false, "Eleitor já registrou seu voto nesta eleição");
            }
            
            // Verificar impedimentos
            if (TemImpedimento)
            {
                return (false, $"Eleitor com impedimento: {MotivoImpedimento}");
            }
            
            // Verificar adimplência
            if (!AdimplenteCau)
            {
                return (false, "Eleitor não está adimplente com o CAU");
            }
            
            // Verificar registro ativo
            if (!RegistroAtivo)
            {
                return (false, "Registro profissional não está ativo");
            }
            
            // Verificar situação
            if (Situacao != SituacaoEleitor.Apto)
            {
                return (false, $"Situação do eleitor: {Situacao}");
            }
            
            // Registrar verificação
            RegistrarVerificacao(true, "Eleitor apto para votar");
            
            return (true, "Eleitor apto para votar");
        }
        
        /// <summary>
        /// Gera token temporário para votação
        /// </summary>
        public string GerarTokenVotacao(int validadeMinutos = 30)
        {
            if (JaVotou)
                throw new InvalidOperationException("Eleitor já votou");
                
            if (Suspenso)
                throw new InvalidOperationException("Eleitor suspenso não pode gerar token");
                
            // Gerar token único
            var tokenData = $"{Id}|{Cpf}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(tokenData);
                var hash = sha256.ComputeHash(bytes);
                TokenVotacao = Convert.ToBase64String(hash);
            }
            
            TokenExpiracao = DateTime.UtcNow.AddMinutes(validadeMinutos);
            
            // Registrar geração do token
            RegistrarVerificacao(true, $"Token de votação gerado, válido até {TokenExpiracao:dd/MM/yyyy HH:mm}");
            
            return TokenVotacao;
        }
        
        /// <summary>
        /// Valida o token de votação
        /// </summary>
        public bool ValidarToken(string token)
        {
            if (string.IsNullOrEmpty(TokenVotacao))
                return false;
                
            if (TokenVotacao != token)
                return false;
                
            if (!TokenExpiracao.HasValue || DateTime.UtcNow > TokenExpiracao.Value)
            {
                TokenVotacao = null;
                TokenExpiracao = null;
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Registra o voto do eleitor
        /// </summary>
        public void RegistrarVoto()
        {
            if (JaVotou)
                throw new InvalidOperationException("Eleitor já votou");
                
            JaVotou = true;
            DataHoraVoto = DateTime.UtcNow;
            Situacao = SituacaoEleitor.JaVotou;
            
            // Limpar token após votar
            TokenVotacao = null;
            TokenExpiracao = null;
            
            // Registrar no histórico
            RegistrarVerificacao(true, "Voto registrado com sucesso");
        }
        
        /// <summary>
        /// Registra tentativa de acesso
        /// </summary>
        public void RegistrarTentativaAcesso(bool sucesso, string detalhes = null)
        {
            UltimaTentativa = DateTime.UtcNow;
            
            if (!sucesso)
            {
                TentativasAcesso++;
                
                // Suspender após 5 tentativas falhas
                if (TentativasAcesso >= 5)
                {
                    Suspender("Excesso de tentativas de acesso falhas");
                }
            }
            else
            {
                TentativasAcesso = 0;
            }
            
            RegistrarVerificacao(sucesso, detalhes ?? "Tentativa de acesso registrada");
        }
        
        /// <summary>
        /// Suspende o eleitor
        /// </summary>
        public void Suspender(string motivo)
        {
            if (Suspenso)
                return;
                
            Suspenso = true;
            DataSuspensao = DateTime.UtcNow;
            MotivoSuspensao = motivo;
            Situacao = SituacaoEleitor.Suspenso;
            
            RegistrarVerificacao(false, $"Eleitor suspenso: {motivo}");
        }
        
        /// <summary>
        /// Reabilita o eleitor suspenso
        /// </summary>
        public void Reabilitar(string motivo)
        {
            if (!Suspenso)
                return;
                
            Suspenso = false;
            DataSuspensao = null;
            MotivoSuspensao = null;
            TentativasAcesso = 0;
            Situacao = SituacaoEleitor.Apto;
            
            RegistrarVerificacao(true, $"Eleitor reabilitado: {motivo}");
        }
        
        /// <summary>
        /// Adiciona impedimento ao eleitor
        /// </summary>
        public void AdicionarImpedimento(string motivo)
        {
            TemImpedimento = true;
            MotivoImpedimento = motivo;
            Situacao = SituacaoEleitor.Impedido;
            
            RegistrarVerificacao(false, $"Impedimento adicionado: {motivo}");
        }
        
        /// <summary>
        /// Remove impedimento do eleitor
        /// </summary>
        public void RemoverImpedimento(string motivo)
        {
            TemImpedimento = false;
            MotivoImpedimento = null;
            Situacao = SituacaoEleitor.Apto;
            
            RegistrarVerificacao(true, $"Impedimento removido: {motivo}");
        }
        
        /// <summary>
        /// Atualiza situação de adimplência
        /// </summary>
        public void AtualizarAdimplencia(bool adimplente)
        {
            AdimplenteCau = adimplente;
            DataUltimaAtualizacao = DateTime.UtcNow;
            
            if (!adimplente && Situacao == SituacaoEleitor.Apto)
            {
                Situacao = SituacaoEleitor.Inadimplente;
            }
            else if (adimplente && Situacao == SituacaoEleitor.Inadimplente)
            {
                Situacao = SituacaoEleitor.Apto;
            }
            
            RegistrarVerificacao(true, $"Adimplência atualizada: {(adimplente ? "Adimplente" : "Inadimplente")}");
        }
        
        /// <summary>
        /// Registra verificação no histórico
        /// </summary>
        private void RegistrarVerificacao(bool sucesso, string detalhes)
        {
            var historico = new HistoricoVerificacaoEleitor
            {
                EleitorAptoId = Id,
                DataHora = DateTime.UtcNow,
                Sucesso = sucesso,
                Detalhes = detalhes,
                Situacao = Situacao.ToString()
            };
            
            Historicos?.Add(historico);
        }
    }
    
    /// <summary>
    /// Situação do eleitor no sistema
    /// </summary>
    public enum SituacaoEleitor
    {
        Pendente,
        Apto,
        Impedido,
        JaVotou,
        Suspenso,
        Inadimplente,
        RegistroInativo
    }
    
    /// <summary>
    /// Histórico de verificações do eleitor
    /// </summary>
    public class HistoricoVerificacaoEleitor
    {
        public int Id { get; set; }
        public int EleitorAptoId { get; set; }
        public DateTime DataHora { get; set; }
        public bool Sucesso { get; set; }
        public string Detalhes { get; set; }
        public string Situacao { get; set; }
        
        public virtual EleitorApto EleitorApto { get; set; }
    }
}