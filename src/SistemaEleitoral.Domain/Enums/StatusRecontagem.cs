namespace SistemaEleitoral.Domain.Enums
{
    public enum StatusRecontagem
    {
        Pendente = 1,
        EmAndamento = 2,
        Concluida = 3,
        Cancelada = 4
    }

    public enum StatusApuracao
    {
        NaoIniciada = 1,
        EmAndamento = 2,
        Concluida = 3,
        Homologada = 4,
        Impugnada = 5,
        Cancelada = 6
    }

    public enum NotificationType
    {
        Email = 1,
        SMS = 2,
        Push = 3,
        InApp = 4
    }
}