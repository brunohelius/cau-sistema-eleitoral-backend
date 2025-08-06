using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_conselheiro", Schema = "public")]
public class Conselheiro
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("pessoa_id")]
    public int PessoaId { get; set; }

    [Column("dt_inicio_mandato")]
    public DateTime? DataInicioMandato { get; set; }

    [Column("dt_fim_mandato")]
    public DateTime? DataFimMandato { get; set; }

    [Column("tipo_conselheiro_id")]
    public int? TipoConselheiroId { get; set; }

    [Column("representacao_conselheiro_id")]
    public int? RepresentacaoConselheiroId { get; set; }

    [Column("filial_id")]
    public int FilialId { get; set; }

    [Column("ies")]
    public bool? Ies { get; set; }

    [Column("dt_cadastro")]
    public DateTime DataCadastro { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("processo_eleitoral_id")]
    public int? ProcessoEleitoralId { get; set; }

    [Column("recomposicao_mandato")]
    public bool RecomposicaoMandato { get; set; }

    [Column("ano_eleicao")]
    public string? AnoEleicao { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; }

    // Navegação
    public virtual Pessoa Pessoa { get; set; } = null!;
    public virtual Filial Filial { get; set; } = null!;
}