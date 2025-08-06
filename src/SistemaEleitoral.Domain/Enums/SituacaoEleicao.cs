namespace SistemaEleitoral.Domain.Enums
{
    /// <summary>
    /// Situações possíveis de uma eleição
    /// </summary>
    public enum SituacaoEleicao
    {
        /// <summary>
        /// Eleição em andamento
        /// </summary>
        EmAndamento = 1,

        /// <summary>
        /// Eleição suspensa temporariamente
        /// </summary>
        Suspensa = 2,

        /// <summary>
        /// Eleição encerrada
        /// </summary>
        Encerrada = 3,

        /// <summary>
        /// Eleição cancelada
        /// </summary>
        Cancelada = 4,

        /// <summary>
        /// Eleição em planejamento
        /// </summary>
        EmPlanejamento = 5
    }
}