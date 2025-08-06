namespace SistemaEleitoral.Domain.Enums
{
    /// <summary>
    /// Enumeração dos tipos de atividades do calendário eleitoral
    /// </summary>
    public enum TipoAtividadeCalendario
    {
        /// <summary>
        /// Período de registro de chapas
        /// </summary>
        RegistroChapa = 1,
        
        /// <summary>
        /// Análise de documentação das chapas
        /// </summary>
        AnaliseDocumentacao = 2,
        
        /// <summary>
        /// Período para impugnações
        /// </summary>
        Impugnacao = 3,
        
        /// <summary>
        /// Julgamento de impugnações
        /// </summary>
        JulgamentoImpugnacao = 4,
        
        /// <summary>
        /// Período de recursos
        /// </summary>
        Recurso = 5,
        
        /// <summary>
        /// Julgamento de recursos
        /// </summary>
        JulgamentoRecurso = 6,
        
        /// <summary>
        /// Pedidos de substituição
        /// </summary>
        Substituicao = 7,
        
        /// <summary>
        /// Período de campanha eleitoral
        /// </summary>
        Campanha = 8,
        
        /// <summary>
        /// Período de votação
        /// </summary>
        Votacao = 9,
        
        /// <summary>
        /// Apuração dos votos
        /// </summary>
        Apuracao = 10,
        
        /// <summary>
        /// Divulgação dos resultados
        /// </summary>
        DivulgacaoResultado = 11,
        
        /// <summary>
        /// Homologação da eleição
        /// </summary>
        Homologacao = 12,
        
        /// <summary>
        /// Diplomação dos eleitos
        /// </summary>
        Diplomacao = 13,
        
        /// <summary>
        /// Posse dos eleitos
        /// </summary>
        Posse = 14
    }
}