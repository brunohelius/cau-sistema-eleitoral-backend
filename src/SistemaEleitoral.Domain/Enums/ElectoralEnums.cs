namespace SistemaEleitoral.Domain.Enums
{
    public enum StatusChapa
    {
        EmElaboracao,
        AguardandoAprovacao,
        Aprovada,
        Rejeitada,
        Homologada,
        Impugnada,
        Cancelada,
        Eleita
    }

    public enum TipoChapa
    {
        Nacional,
        Estadual,
        IES
    }

    public enum TipoMembroChapa
    {
        Coordenador,
        ViceCoordenador,
        Membro,
        Suplente
    }

    public enum StatusMembroChapa
    {
        ConvitePendente,
        Ativo,
        Recusado,
        Removido,
        Substituido,
        Inativo
    }

    public enum TipoComissao
    {
        Nacional,
        Estadual
    }

    public enum CargoComissao
    {
        Coordenador,
        ViceCoordenador,
        Membro,
        Suplente
    }

    public enum StatusMembroComissao
    {
        Ativo,
        Inativo,
        Removido,
        Substituido
    }

    public enum TipoDecisao
    {
        Administrativa,
        Judicial,
        Homologacao,
        Impugnacao,
        Recurso,
        Substituicao
    }

    public enum ResultadoDecisao
    {
        Aprovado,
        Rejeitado,
        Pendente,
        Adiado
    }

    public enum TipoVoto
    {
        Favor,
        Contra,
        Abstencao
    }

    public enum StatusDenuncia
    {
        Recebida,
        EmAnalise,
        Admissivel,
        Inadmissivel,
        AguardandoDefesa,
        DefesaRecebida,
        AudienciaInstrucao,
        AlegacoesFinais,
        AguardandoJulgamento,
        Julgada,
        Arquivada,
        EmRecurso
    }

    public enum StatusImpugnacao
    {
        Protocolada,
        EmAnalise,
        AguardandoDefesa,
        DefesaRecebida,
        AguardandoJulgamento,
        Procedente,
        Improcedente,
        ParcialmenteProcedente,
        Arquivada,
        EmRecurso
    }

    public enum StatusJulgamento
    {
        Agendado,
        EmAndamento,
        Suspenso,
        Adiado,
        Julgado,
        Anulado
    }

    public enum InstanciaJulgamento
    {
        Primeira,
        Segunda,
        Especial
    }

    public enum DecisaoJulgamento
    {
        Procedente,
        Improcedente,
        ParcialmenteProcedente,
        Extinto,
        Anulado
    }

    public enum TipoProcessoJudicial
    {
        Denuncia,
        Impugnacao,
        Recurso,
        Substituicao,
        Cassacao
    }

    public enum StatusRecurso
    {
        Protocolado,
        EmAnalise,
        AguardandoContraRazoes,
        ContraRazoesRecebidas,
        AguardandoJulgamento,
        Provido,
        NaoProvido,
        ParcialmenteProvido,
        NaoConhecido
    }

    public enum StatusProcessoSubstituicao
    {
        Solicitado,
        EmAnalise,
        AguardandoDocumentacao,
        DocumentacaoRecebida,
        AguardandoValidacao,
        Aprovado,
        Rejeitado,
        Efetivado
    }

    public enum StatusEleicao
    {
        Planejada,
        Ativa,
        EmAndamento,
        Encerrada,
        Homologada,
        Anulada,
        Cancelada
    }

    public enum TipoDocumentoEleitoral
    {
        Diploma,
        TermoPosse,
        AtaReuniao,
        Decisao,
        Recurso,
        Impugnacao,
        Denuncia,
        Notificacao,
        Edital,
        Relatorio,
        ComprovanteSituacao,
        DeclaracaoDiversidade,
        Outros
    }

    public enum TipoNotificacao
    {
        Email,
        SMS,
        Sistema,
        Whatsapp
    }

    public enum StatusValidacao
    {
        Pendente,
        Validado,
        Invalido,
        EmAnalise,
        Expirado
    }

    public enum TipoUsuario
    {
        Profissional,
        ComissaoEleitoral,
        Administrador,
        Sistema
    }

    public enum StatusMembroComissaoEleitoral
    {
        Ativo,
        Inativo,
        Removido,
        Substituido
    }

    public enum TipoMembroComissaoEleitoral
    {
        Coordenador,
        ViceCoordenador,
        Membro,
        Suplente
    }

    public enum SituacaoMembroComissaoEleitoral
    {
        Normal,
        Afastado,
        Impedido,
        Suspenso
    }
}