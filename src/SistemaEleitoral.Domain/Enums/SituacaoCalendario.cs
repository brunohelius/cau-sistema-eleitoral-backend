namespace SistemaEleitoral.Domain.Enums
{
    /// <summary>
    /// Enumeração das situações possíveis de um calendário eleitoral
    /// </summary>
    public enum SituacaoCalendario
    {
        /// <summary>
        /// Calendário em fase de elaboração
        /// </summary>
        EmElaboracao = 1,
        
        /// <summary>
        /// Calendário publicado e disponível
        /// </summary>
        Publicado = 2,
        
        /// <summary>
        /// Calendário em andamento
        /// </summary>
        EmAndamento = 3,
        
        /// <summary>
        /// Calendário temporariamente suspenso
        /// </summary>
        Suspenso = 4,
        
        /// <summary>
        /// Calendário concluído
        /// </summary>
        Concluido = 5,
        
        /// <summary>
        /// Calendário cancelado
        /// </summary>
        Cancelado = 6
    }
}