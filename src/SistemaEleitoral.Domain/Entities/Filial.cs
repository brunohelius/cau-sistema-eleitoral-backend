using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_filial", Schema = "public")]
public class Filial
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("prefixo")]
    public string Prefixo { get; set; } = string.Empty;

    [Required]
    [Column("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [Column("cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [Column("filial_id")]
    public int? FilialId { get; set; }

    [Column("tipofilial_id")]
    public int? TipoFilialId { get; set; }

    [Column("enderecologradouro")]
    public string? Logradouro { get; set; }

    [Column("endereconumero")]
    public string? NumeroEndereco { get; set; }

    [Column("enderecobairro")]
    public string? Bairro { get; set; }

    [Column("enderecocomplemento")]
    public string? Complemento { get; set; }

    [Column("enderecocidade")]
    public string? Cidade { get; set; }

    [Column("enderecouf")]
    public string? Uf { get; set; }

    [Column("enderecocep")]
    public string? Cep { get; set; }

    // Propriedade auxiliar (não mapeada)
    [NotMapped]
    public string? ImagemBandeira { get; set; }
}