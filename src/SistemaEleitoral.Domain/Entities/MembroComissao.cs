using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_MEMBRO_COMISSAO", Schema = "eleitoral")]
public class MembroComissao
{
    [Key]
    [Column("ID_MEMBRO_COMISSAO")]
    public int Id { get; set; }

    [Column("ID_PESSOA")]
    public int PessoaId { get; set; }

    [Column("ID_CAU_UF")]
    public int IdCauUf { get; set; }

    [Column("ST_EXCLUIDO")]
    public bool Excluido { get; set; }

    [Column("ST_RESPOSTA_DECLARACAO")]
    public bool? SitRespostaDeclaracao { get; set; }

    [Column("ID_TIPO_PARTICIPACAO")]
    public int TipoParticipacaoId { get; set; }

    [Column("ID_INF_COMISSAO_MEMBRO")]
    public int InformacaoComissaoMembroId { get; set; }

    [Column("ID_MEMBRO_SUBSTITUTO")]
    public int? MembroSubstitutoId { get; set; }

    [Column("ID_FILIAL")]
    public int FilialId { get; set; }

    [Column("ID_PESSOA_ENTITY")]
    public int? PessoaEntityId { get; set; }

    [Column("ID_PROFISSIONAL_ENTITY")]
    public int? ProfissionalEntityId { get; set; }

    // Navegação
    public virtual TipoParticipacaoMembro TipoParticipacao { get; set; } = null!;
    public virtual InformacaoComissaoMembro InformacaoComissaoMembro { get; set; } = null!;
    public virtual MembroComissaoEleitoral? MembroSubstituto { get; set; }
    public virtual Filial Filial { get; set; } = null!;
    public virtual Pessoa? PessoaEntity { get; set; }
    public virtual Profissional? ProfissionalEntity { get; set; }
    public virtual ICollection<MembroComissaoEleitoralSituacao> MembroComissaoEleitoralSituacoes { get; set; } = new List<MembroComissaoEleitoralSituacao>();
    public virtual ICollection<ArquivoDecMembroComissao> Arquivos { get; set; } = new List<ArquivoDecMembroComissao>();
    public virtual ICollection<DenunciaAdmitida> DenunciasAdmitidas { get; set; } = new List<DenunciaAdmitida>();
}