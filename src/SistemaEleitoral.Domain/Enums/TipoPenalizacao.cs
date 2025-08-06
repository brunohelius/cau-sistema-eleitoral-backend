namespace SistemaEleitoral.Domain.Enums
{
    /// <summary>
    /// Enumeração dos tipos de penalização ética
    /// </summary>
    public enum TipoPenalizacao
    {
        Advertencia = 1,
        CensuraPublica = 2,
        Suspensao = 3,
        InabilitacaoTemporaria = 4,
        Cassacao = 5
    }
    
    /// <summary>
    /// Enumeração dos status de processo judicial
    /// </summary>
    public enum StatusProcessoJudicial
    {
        EmAndamento = 1,
        Suspenso = 2,
        Arquivado = 3,
        Julgado = 4,
        EmRecurso = 5
    }
    
    
    /// <summary>
    /// Enumeração dos status de contrato
    /// </summary>
    public enum StatusContrato
    {
        Ativo = 1,
        Suspenso = 2,
        Cancelado = 3,
        Concluido = 4,
        EmElaboracao = 5
    }
}