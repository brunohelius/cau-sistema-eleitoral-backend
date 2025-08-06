using System;
using System.Security.Cryptography;
using System.Text;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Representa um voto registrado no sistema eleitoral com segurança criptográfica
    /// </summary>
    public class VotoEleitoral : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int SessaoVotacaoId { get; set; }
        public int EleicaoId { get; set; }
        public int? ChapaId { get; set; }
        public string CpfHasheado { get; private set; } // CPF anonimizado com SHA-256
        public DateTime DataHoraVoto { get; set; }
        public TipoVoto TipoVoto { get; set; }
        public StatusVoto Status { get; set; }
        
        // Segurança e Integridade
        public string HashVoto { get; private set; }
        public string HashIntegridade { get; private set; }
        public string TokenSessao { get; set; }
        
        // Auditoria
        public string IpOrigem { get; set; }
        public string UserAgent { get; set; }
        public string Dispositivo { get; set; }
        public string LocalizacaoAproximada { get; set; }
        
        // Controle de Anulação
        public bool Anulado { get; set; }
        public DateTime? DataAnulacao { get; set; }
        public string MotivoAnulacao { get; set; }
        public int? AnuladoPorId { get; set; }
        
        // Navegação
        public virtual SessaoVotacao SessaoVotacao { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        public virtual ChapaEleicao Chapa { get; set; }
        public virtual Usuario AnuladoPor { get; set; }
        public virtual ComprovanteVotacao Comprovante { get; set; }
        
        // Construtor
        public VotoEleitoral()
        {
            Status = StatusVoto.Registrado;
            DataHoraVoto = DateTime.UtcNow;
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Registra um novo voto com segurança criptográfica
        /// </summary>
        public static VotoEleitoral RegistrarVoto(
            int sessaoId, 
            int eleicaoId, 
            int? chapaId, 
            string cpf,
            TipoVoto tipoVoto,
            string tokenSessao,
            string ipOrigem,
            string userAgent = null)
        {
            var voto = new VotoEleitoral
            {
                SessaoVotacaoId = sessaoId,
                EleicaoId = eleicaoId,
                ChapaId = chapaId,
                TipoVoto = tipoVoto,
                TokenSessao = tokenSessao,
                IpOrigem = ipOrigem,
                UserAgent = userAgent,
                DataHoraVoto = DateTime.UtcNow,
                Status = StatusVoto.Registrado
            };
            
            // Anonimizar CPF
            voto.CpfHasheado = GerarHashCpf(cpf);
            
            // Gerar hash do voto
            voto.HashVoto = voto.GerarHashVoto();
            
            // Gerar hash de integridade
            voto.HashIntegridade = voto.GerarHashIntegridade();
            
            return voto;
        }
        
        /// <summary>
        /// Gera hash SHA-256 do CPF para anonimização
        /// </summary>
        private static string GerarHashCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
                throw new ArgumentException("CPF não pode ser vazio");
                
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(cpf + "SALT_ELEITORAL_2025");
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Gera hash único do voto
        /// </summary>
        private string GerarHashVoto()
        {
            var dados = $"{SessaoVotacaoId}|{EleicaoId}|{ChapaId}|{TipoVoto}|{DataHoraVoto:O}|{CpfHasheado}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Gera hash de integridade para verificação
        /// </summary>
        private string GerarHashIntegridade()
        {
            var dados = $"{HashVoto}|{TokenSessao}|{IpOrigem}|{UserAgent}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Verifica a integridade do voto
        /// </summary>
        public bool VerificarIntegridade()
        {
            var hashCalculado = GerarHashIntegridade();
            return hashCalculado == HashIntegridade;
        }
        
        /// <summary>
        /// Anula o voto com justificativa
        /// </summary>
        public void AnularVoto(string motivo, int usuarioId)
        {
            if (Anulado)
                throw new InvalidOperationException("Voto já foi anulado");
                
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Motivo da anulação é obrigatório");
                
            Anulado = true;
            DataAnulacao = DateTime.UtcNow;
            MotivoAnulacao = motivo;
            AnuladoPorId = usuarioId;
            Status = StatusVoto.Anulado;
        }
        
        /// <summary>
        /// Marca o voto como computado na apuração
        /// </summary>
        public void MarcarComoComputado()
        {
            if (Status != StatusVoto.Registrado)
                throw new InvalidOperationException($"Voto não pode ser computado no status {Status}");
                
            Status = StatusVoto.Computado;
        }
        
        /// <summary>
        /// Retorna resumo do voto para logs (sem dados sensíveis)
        /// </summary>
        public string ObterResumoParaLog()
        {
            return $"Voto #{Id} - Sessão: {SessaoVotacaoId}, Tipo: {TipoVoto}, Status: {Status}, Data: {DataHoraVoto:yyyy-MM-dd HH:mm:ss}";
        }
    }
    
    /// <summary>
    /// Status do voto no sistema
    /// </summary>
    public enum StatusVoto
    {
        Registrado,
        Computado,
        Anulado,
        Pendente,
        Verificado
    }
    
    /// <summary>
    /// Tipo de voto registrado
    /// </summary>
    public enum TipoVoto
    {
        Valido,
        Branco,
        Nulo,
        Abstencao
    }
}