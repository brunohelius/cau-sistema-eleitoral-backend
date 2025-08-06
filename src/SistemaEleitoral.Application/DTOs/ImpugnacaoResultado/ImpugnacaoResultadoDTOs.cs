using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Application.DTOs.ImpugnacaoResultado
{
    /// <summary>
    /// DTO para registro de impugnação de resultado
    /// </summary>
    public class RegistrarImpugnacaoResultadoDTO
    {
        [Required(ErrorMessage = "A narração dos fatos é obrigatória")]
        public string NarracaoFatos { get; set; } = string.Empty;

        [Required(ErrorMessage = "O calendário é obrigatório")]
        public int CalendarioId { get; set; }

        [Required(ErrorMessage = "O profissional é obrigatório")]
        public int ProfissionalId { get; set; }

        public int? CauBrId { get; set; }

        [Required(ErrorMessage = "O nome do arquivo é obrigatório")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome físico do arquivo é obrigatório")]
        public string NomeArquivoFisico { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO básico de impugnação de resultado
    /// </summary>
    public class ImpugnacaoResultadoDTO
    {
        public int Id { get; set; }
        public string NarracaoFatos { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime DataCadastro { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string ProfissionalNome { get; set; } = string.Empty;
        public string StatusDescricao { get; set; } = string.Empty;
        public int CalendarioId { get; set; }
        public string Protocolo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO detalhado de impugnação de resultado
    /// </summary>
    public class ImpugnacaoResultadoDetalheDTO : ImpugnacaoResultadoDTO
    {
        public string NomeArquivoFisico { get; set; } = string.Empty;
        public int ProfissionalId { get; set; }
        public int StatusId { get; set; }
        public List<AlegacaoImpugnacaoResultadoDTO> Alegacoes { get; set; } = new();
        public List<RecursoImpugnacaoResultadoDTO> Recursos { get; set; } = new();
        public JulgamentoAlegacaoImpugResultadoDTO? JulgamentoAlegacao { get; set; }
        public JulgamentoRecursoImpugResultadoDTO? JulgamentoRecurso { get; set; }
    }

    /// <summary>
    /// DTO para adicionar alegação
    /// </summary>
    public class AdicionarAlegacaoDTO
    {
        [Required(ErrorMessage = "A impugnação é obrigatória")]
        public int ImpugnacaoResultadoId { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O profissional é obrigatório")]
        public int ProfissionalId { get; set; }

        public int? ChapaEleicaoId { get; set; }

        [Required(ErrorMessage = "O nome do arquivo é obrigatório")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome físico do arquivo é obrigatório")]
        public string NomeArquivoFisico { get; set; } = string.Empty;

        public bool IsImpugnante { get; set; }
    }

    /// <summary>
    /// DTO de alegação
    /// </summary>
    public class AlegacaoImpugnacaoResultadoDTO
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public string ProfissionalNome { get; set; } = string.Empty;
        public string TipoAlegante { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para adicionar recurso
    /// </summary>
    public class AdicionarRecursoDTO
    {
        [Required(ErrorMessage = "A impugnação é obrigatória")]
        public int ImpugnacaoResultadoId { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O profissional é obrigatório")]
        public int ProfissionalId { get; set; }

        [Required(ErrorMessage = "O tipo de recurso é obrigatório")]
        public int TipoRecursoId { get; set; }

        [Required(ErrorMessage = "O nome do arquivo é obrigatório")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome físico do arquivo é obrigatório")]
        public string NomeArquivoFisico { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de recurso
    /// </summary>
    public class RecursoImpugnacaoResultadoDTO
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public string ProfissionalNome { get; set; } = string.Empty;
        public string TipoRecurso { get; set; } = string.Empty;
        public bool? Deferido { get; set; }
        public string? Parecer { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para adicionar contrarrazão
    /// </summary>
    public class AdicionarContrarrazaoDTO
    {
        [Required(ErrorMessage = "O recurso é obrigatório")]
        public int RecursoImpugnacaoResultadoId { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O profissional é obrigatório")]
        public int ProfissionalId { get; set; }

        public int? ChapaEleicaoId { get; set; }

        [Required(ErrorMessage = "O nome do arquivo é obrigatório")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome físico do arquivo é obrigatório")]
        public string NomeArquivoFisico { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de contrarrazão
    /// </summary>
    public class ContrarrazaoImpugnacaoResultadoDTO
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public string ProfissionalNome { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para julgar alegação
    /// </summary>
    public class JulgarAlegacaoDTO
    {
        [Required(ErrorMessage = "A impugnação é obrigatória")]
        public int ImpugnacaoResultadoId { get; set; }

        [Required(ErrorMessage = "O parecer é obrigatório")]
        public string Parecer { get; set; } = string.Empty;

        public bool Deferido { get; set; }

        [Required(ErrorMessage = "O profissional julgador é obrigatório")]
        public int ProfissionalJulgadorId { get; set; }

        public string? NomeArquivoDecisao { get; set; }
        public string? NomeArquivoFisicoDecisao { get; set; }
    }

    /// <summary>
    /// DTO de julgamento de alegação
    /// </summary>
    public class JulgamentoAlegacaoImpugResultadoDTO
    {
        public int Id { get; set; }
        public string Parecer { get; set; } = string.Empty;
        public bool Deferido { get; set; }
        public DateTime DataJulgamento { get; set; }
        public string ProfissionalJulgadorNome { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para julgar recurso
    /// </summary>
    public class JulgarRecursoDTO
    {
        [Required(ErrorMessage = "A impugnação é obrigatória")]
        public int ImpugnacaoResultadoId { get; set; }

        [Required(ErrorMessage = "O parecer é obrigatório")]
        public string Parecer { get; set; } = string.Empty;

        public bool Deferido { get; set; }

        [Required(ErrorMessage = "O profissional julgador é obrigatório")]
        public int ProfissionalJulgadorId { get; set; }

        public bool DecisaoFinal { get; set; }

        public string? NomeArquivoDecisao { get; set; }
        public string? NomeArquivoFisicoDecisao { get; set; }
    }

    /// <summary>
    /// DTO de julgamento de recurso
    /// </summary>
    public class JulgamentoRecursoImpugResultadoDTO
    {
        public int Id { get; set; }
        public string Parecer { get; set; } = string.Empty;
        public bool Deferido { get; set; }
        public DateTime DataJulgamento { get; set; }
        public string ProfissionalJulgadorNome { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Instancia { get; set; } = string.Empty;
        public bool DecisaoFinal { get; set; }
    }
}