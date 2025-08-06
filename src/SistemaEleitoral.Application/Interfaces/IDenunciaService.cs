using Microsoft.AspNetCore.Http;
using SistemaEleitoral.Application.DTOs.Denuncia;

namespace SistemaEleitoral.Application.Interfaces
{
    /// <summary>
    /// Interface do serviço de denúncias
    /// Contrato completo para o sistema de denúncias migrado do PHP
    /// </summary>
    public interface IDenunciaService
    {
        // CRUD BÁSICO
        Task<PagedResult<DenunciaListDto>> ObterDenunciasAsync(DenunciaFiltroDto filtros, int page, int pageSize);
        Task<DenunciaDetalheDto> ObterDenunciaPorIdAsync(int id);
        Task<DenunciaDetalheDto> CriarDenunciaAsync(CriarDenunciaDto dto);
        Task<DenunciaDetalheDto> AtualizarDenunciaAsync(int id, AtualizarDenunciaDto dto);
        Task<bool> ExcluirDenunciaAsync(int id);

        // WORKFLOW LEGAL - ANÁLISE DE ADMISSIBILIDADE
        Task IniciarAnaliseAdmissibilidadeAsync(int denunciaId);
        Task ConcluirAnaliseAdmissibilidadeAsync(int denunciaId, bool admissivel, string motivo = null);

        // WORKFLOW LEGAL - PROCESSO DE DEFESA
        Task NotificarParaDefesaAsync(int denunciaId);
        Task ReceberDefesaAsync(int denunciaId, string textoDefesa);

        // WORKFLOW LEGAL - PRODUÇÃO DE PROVAS
        Task AbrirProducaoProvasAsync(int denunciaId);
        Task EncerrarProducaoProvasAsync(int denunciaId);

        // WORKFLOW LEGAL - AUDIÊNCIA DE INSTRUÇÃO
        Task AgendarAudienciaInstrucaoAsync(int denunciaId, DateTime dataAudiencia);
        Task RegistrarAudienciaAsync(int denunciaId, string resumoAudiencia);

        // WORKFLOW LEGAL - ALEGAÇÕES FINAIS
        Task ReceberAlegacoesFinaisAsync(int denunciaId, string alegacoes);

        // WORKFLOW LEGAL - JULGAMENTO (1ª INSTÂNCIA)
        Task JulgarDenunciaAsync(int denunciaId, string decisao, bool cabeRecurso);

        // WORKFLOW LEGAL - RECURSOS (2ª INSTÂNCIA)
        Task InterporRecursoAsync(int denunciaId, string fundamentacao);
        Task JulgarRecursoAsync(int denunciaId, string decisaoRecurso);

        // CONTROLE PROCESSUAL
        Task ArquivarDenunciaAsync(int denunciaId, string motivo);
        Task DesignarRelatorAsync(int denunciaId, int relatorId);

        // GESTÃO DE ARQUIVOS
        Task AdicionarArquivoAsync(int denunciaId, IFormFile arquivo, string descricao, string tipoDocumento);
        Task<ArquivoDownloadDto> ObterArquivoAsync(int denunciaId, int arquivoId);
        Task RemoverArquivoAsync(int denunciaId, int arquivoId);
        Task<List<ArquivoDenunciaDto>> ObterArquivosDenunciaAsync(int denunciaId);

        // GESTÃO DE TESTEMUNHAS
        Task AdicionarTestemunhaAsync(int denunciaId, CriarTestemunhaDto dto);
        Task AtualizarTestemunhaAsync(int denunciaId, int testemunhaId, CriarTestemunhaDto dto);
        Task RemoverTestemunhaAsync(int denunciaId, int testemunhaId);
        Task NotificarTestemunhaAsync(int denunciaId, int testemunhaId);
        Task RegistrarComparecimentoTestemunhaAsync(int denunciaId, int testemunhaId, bool compareceu, string observacoes = null);

        // HISTÓRICO E AUDITORIA
        Task<List<HistoricoDenunciaDto>> ObterHistoricoDenunciaAsync(int denunciaId);
        Task AdicionarHistoricoAsync(int denunciaId, string tipoOperacao, string observacao);

        // CONSULTAS ESPECIALIZADAS
        Task<List<TipoDenunciaDto>> ObterTiposDenunciaAsync();
        Task<EstatisticasDenunciaDto> ObterEstatisticasAsync(DateTime? dataInicio = null, DateTime? dataFim = null);
        Task<PrazosDenunciaDto> ValidarPrazosAsync(int denunciaId);
        Task<List<DenunciaVencimentoDto>> ObterDenunciasProximasVencimentoAsync(int diasAntecedencia = 7);

        // RELATÓRIOS E EXTRATOS
        Task<List<DenunciaListDto>> ObterDenunciasPorProfissionalAsync(int profissionalId);
        Task<List<DenunciaListDto>> ObterDenunciasPorFilialAsync(int filialId);
        Task<List<DenunciaListDto>> ObterDenunciasPorRelatorAsync(int relatorId);
        Task<List<DenunciaListDto>> ObterDenunciasPorStatusAsync(string status);
        Task<List<DenunciaListDto>> ObterDenunciasPorTipoAsync(int tipoId);

        // VALIDAÇÕES DE NEGÓCIO
        Task<bool> ValidarAcessoDenunciaAsync(int denunciaId, int usuarioId);
        Task<bool> ValidarPermissaoOperacaoAsync(int denunciaId, string operacao, int usuarioId);
        Task<bool> ValidarPrazoOperacaoAsync(int denunciaId, string operacao);

        // NOTIFICAÇÕES
        Task EnviarNotificacaoAdmissibilidadeAsync(int denunciaId);
        Task EnviarNotificacaoDefesaAsync(int denunciaId);
        Task EnviarNotificacaoJulgamentoAsync(int denunciaId);
        Task EnviarNotificacaoRecursoAsync(int denunciaId);
        Task EnviarNotificacaoArquivamentoAsync(int denunciaId);

        // INTEGRAÇÃO COM CALENDÁRIO ELEITORAL
        Task<bool> ValidarPeriodoEleitoral();
        Task<bool> ValidarAtividadeSecundariaAsync(int atividadeId);

        // MIGRAÇÃO E COMPATIBILIDADE
        Task<DenunciaDetalheDto> ObterDenunciaPorProtocoloAsync(string protocolo);
        Task<List<DenunciaListDto>> ObterDenunciasAgrupadasAsync(int pessoaId);
        Task<bool> ValidarProfissionalLogadoAsync(int denunciaId, int usuarioId);

        // DASHBOARD E MÉTRICAS
        Task<Dictionary<string, object>> ObterDashboardDenunciasAsync();
        Task<List<object>> ObterMetricasTemporalAsync(DateTime dataInicio, DateTime dataFim);
        Task<Dictionary<string, int>> ObterDistribuicaoPorStatusAsync();
        Task<Dictionary<string, int>> ObterDistribuicaoPorTipoAsync();
        Task<Dictionary<string, int>> ObterDistribuicaoPorFilialAsync();

        // AUTOMAÇÃO E JOBS
        Task ProcessarVencimentoPrazosAsync();
        Task EnviarLembretesVencimentoAsync();
        Task ProcessarNotificacoesAutomaticasAsync();
        Task ExecutarManutencaoPeriodicaAsync();
    }

    // Classe auxiliar para resultados paginados
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}