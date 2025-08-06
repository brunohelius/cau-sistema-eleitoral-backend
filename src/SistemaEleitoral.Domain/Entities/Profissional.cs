using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    [Table("tb_profissionais", Schema = "public")]
    public class Profissional : BaseEntity
    {
        [Column("cpf")]
        public string Cpf { get; set; }
        
        [Column("nome")]
        public string Nome { get; set; }
        
        [Column("email")]
        public string Email { get; set; }
        
        [Column("telefone")]
        public string Telefone { get; set; }
        
        [Column("telefone_celular")]
        public string TelefoneCelular { get; set; }
        
        [Column("numero_registro")]
        public string NumeroRegistro { get; set; }
        
        [Column("uf_registro")]
        public string UfRegistro { get; set; }
        
        [Column("data_registro")]
        public DateTime? DataRegistro { get; set; }
        
        [Column("data_formatura")]
        public DateTime? DataFormatura { get; set; }
        
        [Column("instituicao_formacao")]
        public string InstituicaoFormacao { get; set; }
        
        [Column("registro_ativo")]
        public bool RegistroAtivo { get; set; }
        
        [Column("adimplente_situacao_financeira")]
        public bool AdimplenteSituacaoFinanceira { get; set; }
        
        [Column("adimplente_situacao_etica")]
        public bool AdimplenteSituacaoEtica { get; set; }
        
        [Column("data_nascimento")]
        public DateTime? DataNascimento { get; set; }
        
        [Column("genero")]
        public string Genero { get; set; }
        
        [Column("etnia")]
        public string Etnia { get; set; }
        
        [Column("lgbtqi")]
        public bool LGBTQI { get; set; }
        
        [Column("possui_deficiencia")]
        public bool PossuiDeficiencia { get; set; }
        
        [Column("tipo_deficiencia")]
        public string TipoDeficiencia { get; set; }
        
        [Column("endereco_completo")]
        public string EnderecoCompleto { get; set; }
        
        [Column("cidade")]
        public string Cidade { get; set; }
        
        [Column("estado")]
        public string Estado { get; set; }
        
        [Column("cep")]
        public string Cep { get; set; }
        
        [Column("foto_url")]
        public string FotoUrl { get; set; }
        
        [Column("ultimo_acesso")]
        public DateTime? UltimoAcesso { get; set; }
        
        [Column("email_verificado")]
        public bool EmailVerificado { get; set; }
        
        [Column("telefone_verificado")]
        public bool TelefoneVerificado { get; set; }
        
        // Navigation Properties
        public virtual ICollection<MembroChapa> ParticipacoesChapaS { get; set; } = new List<MembroChapa>();
        public virtual ICollection<MembroComissaoEleitoral> ParticipacoesComisSao { get; set; } = new List<MembroComissaoEleitoral>();
        public virtual ICollection<Denuncia> DenunciasRealizadas { get; set; } = new List<Denuncia>();
        public virtual ICollection<Denuncia> DenunciasRecebidas { get; set; } = new List<Denuncia>();
        public virtual ICollection<Impugnacao> ImpugnacoesRealizadas { get; set; } = new List<Impugnacao>();

        // Business Methods
        public bool IsElegivel()
        {
            if (!RegistroAtivo)
                return false;

            if (!AdimplenteSituacaoFinanceira)
                return false;

            if (!AdimplenteSituacaoEtica)
                return false;

            if (!DataFormatura.HasValue)
                return false;

            var anosFormado = (DateTime.Now - DataFormatura.Value).TotalDays / 365;
            if (anosFormado < 3)
                return false;

            return true;
        }

        public int GetAnosFormacao()
        {
            if (!DataFormatura.HasValue)
                return 0;

            return (int)((DateTime.Now - DataFormatura.Value).TotalDays / 365);
        }

        public bool PodeSerCoordenador()
        {
            return IsElegivel() && GetAnosFormacao() >= 5;
        }

        public bool PodeSerViceCoordenador()
        {
            return IsElegivel() && GetAnosFormacao() >= 4;
        }

        public string GetIdentificacao()
        {
            return $"{Nome} - CAU {NumeroRegistro}/{UfRegistro}";
        }

        public void AtualizarSituacaoFinanceira(bool adimplente)
        {
            AdimplenteSituacaoFinanceira = adimplente;
            DataAtualizacao = DateTime.UtcNow;
        }

        public void AtualizarSituacaoEtica(bool adimplente)
        {
            AdimplenteSituacaoEtica = adimplente;
            DataAtualizacao = DateTime.UtcNow;
        }

        public void RegistrarAcesso()
        {
            UltimoAcesso = DateTime.UtcNow;
        }
    }
}