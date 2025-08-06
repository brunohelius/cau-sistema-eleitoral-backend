using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Application.DTOs.Denuncia
{
    /// <summary>
    /// DTO para criar nova denúncia
    /// </summary>
    public class CriarDenunciaDto
    {
        [Required(ErrorMessage = "Tipo de denúncia é obrigatório")]
        public int TipoDenunciaId { get; set; }

        [Required(ErrorMessage = "Descrição dos fatos é obrigatória")]
        [MaxLength(2000, ErrorMessage = "Descrição dos fatos não pode exceder 2000 caracteres")]
        public string DescricaoFatos { get; set; }

        public int? FilialId { get; set; }

        public bool TemSigilo { get; set; } = false;

        // Denúncia contra chapa
        public CriarDenunciaChapaDto DenunciaChapa { get; set; }

        // Denúncia contra membro de chapa
        public CriarDenunciaMembroChapaDto DenunciaMembroChapa { get; set; }

        // Denúncia contra membro de comissão
        public CriarDenunciaMembroComissaoDto DenunciaMembroComissao { get; set; }

        // Denúncia contra terceiros
        public CriarDenunciaOutroDto DenunciaOutro { get; set; }

        // Testemunhas
        public List<CriarTestemunhaDto> Testemunhas { get; set; } = new List<CriarTestemunhaDto>();
    }

    public class CriarDenunciaChapaDto
    {
        [Required]
        public int ChapaEleicaoId { get; set; }

        [MaxLength(1000)]
        public string DetalhesEspecificos { get; set; }

        [MaxLength(1000)]
        public string InfracoesAlegadas { get; set; }
    }

    public class CriarDenunciaMembroChapaDto
    {
        [Required]
        public int MembroChapaId { get; set; }

        [MaxLength(1000)]
        public string DetalhesEspecificos { get; set; }

        [MaxLength(1000)]
        public string CondutasIrregulares { get; set; }

        [MaxLength(100)]
        public string CargoNaChapa { get; set; }
    }

    public class CriarDenunciaMembroComissaoDto
    {
        [Required]
        public int MembroComissaoId { get; set; }

        [MaxLength(1000)]
        public string DetalhesEspecificos { get; set; }

        [MaxLength(1000)]
        public string AlegacoesParcialidade { get; set; }

        [MaxLength(100)]
        public string FuncaoComissao { get; set; }
    }

    public class CriarDenunciaOutroDto
    {
        [Required]
        [MaxLength(200)]
        public string NomeDenunciado { get; set; }

        [MaxLength(20)]
        public string CpfCnpjDenunciado { get; set; }

        [MaxLength(100)]
        public string CargoFuncao { get; set; }

        [MaxLength(200)]
        public string InstituicaoVinculada { get; set; }

        [MaxLength(1000)]
        public string DetalhesEspecificos { get; set; }

        [MaxLength(500)]
        public string RelacaoProcessoEleitoral { get; set; }

        [MaxLength(500)]
        public string Endereco { get; set; }

        [MaxLength(50)]
        public string Telefone { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }
    }

    public class CriarTestemunhaDto
    {
        [Required]
        [MaxLength(200)]
        public string NomeCompleto { get; set; }

        [MaxLength(14)]
        public string Cpf { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Telefone { get; set; }

        [MaxLength(500)]
        public string Endereco { get; set; }

        [MaxLength(100)]
        public string Profissao { get; set; }

        [MaxLength(500)]
        public string RelacaoComFatos { get; set; }

        public string ResumoTestemunho { get; set; }
    }
}