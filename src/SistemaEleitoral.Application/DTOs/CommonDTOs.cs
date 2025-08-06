using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Application.DTOs
{
    // Auth DTOs
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginCAUDto : LoginDto
    {
        public string RegistroCAU { get; set; }
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class LogoutDto
    {
        public string Token { get; set; }
    }

    public class AlterarSenhaDto
    {
        public string SenhaAtual { get; set; }
        public string NovaSenha { get; set; }
    }

    public class RecuperarSenhaDto
    {
        public string Email { get; set; }
    }

    public class RedefinirSenhaDto
    {
        public string Token { get; set; }
        public string NovaSenha { get; set; }
    }

    // Calendario DTOs
    public class CriarCalendarioDTO
    {
        public string Nome { get; set; }
        public int Ano { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }

    // Chapa DTOs
    public class RegistrarChapaDTO
    {
        public string NomeChapa { get; set; }
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public int ResponsavelId { get; set; }
        public List<int> MembrosIds { get; set; }
    }

    public class ConfirmarChapaDTO
    {
        public string ObservacoesConfirmacao { get; set; }
        public int UsuarioConfirmacaoId { get; set; }
    }

    public class AdicionarMembroChapaDTO
    {
        public int ChapaId { get; set; }
        public int ProfissionalId { get; set; }
        public int CalendarioId { get; set; }
        public string Cargo { get; set; }
        public int OrdemExibicao { get; set; }
        public int UsuarioRegistroId { get; set; }
    }

    public class RemoverMembroChapaDTO
    {
        public string Motivo { get; set; }
        public int UsuarioRemocaoId { get; set; }
    }

    public class SubstituirMembroChapaDTO
    {
        public int NovoProfissionalId { get; set; }
        public int CalendarioId { get; set; }
        public string Motivo { get; set; }
        public int UsuarioSubstituicaoId { get; set; }
    }

    public class FiltroChapasDTO
    {
        public int? CalendarioId { get; set; }
        public int? UfId { get; set; }
        public StatusChapa? Status { get; set; }
        public string? NomeChapa { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanhoPagina { get; set; } = 20;
    }

    // Denuncia DTOs
    public class RegistrarDenunciaDTO
    {
        public int CalendarioId { get; set; }
        public int ChapaId { get; set; }
        public int DenuncianteId { get; set; }
        public string TipoDenuncia { get; set; }
        public string Descricao { get; set; }
        public string Fundamentacao { get; set; }
        public List<string> DocumentosAnexos { get; set; }
        public int UsuarioRegistroId { get; set; }
    }

    public class EncaminharDenunciaDTO
    {
        public int ComissaoDestinoId { get; set; }
        public string MotivoEncaminhamento { get; set; }
        public int UsuarioEncaminhamentoId { get; set; }
    }

    public class AdmitirDenunciaDTO
    {
        public bool Admitida { get; set; }
        public string Parecer { get; set; }
        public int RelatorId { get; set; }
    }

    public class RegistrarDefesaDTO
    {
        public string ConteudoDefesa { get; set; }
        public List<string> DocumentosDefesa { get; set; }
        public int DefensorId { get; set; }
    }

    public class RegistrarProvasDTO
    {
        public string DescricaoProvas { get; set; }
        public List<string> DocumentosProva { get; set; }
        public int ResponsavelId { get; set; }
    }

    public class FiltroDenunciasDTO
    {
        public int? CalendarioId { get; set; }
        public int? ChapaId { get; set; }
        public string? Status { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class ImpedimentoSuspeicaoDTO
    {
        public int MembroId { get; set; }
        public string TipoDeclaracao { get; set; }
        public string Motivo { get; set; }
    }

    // Impugnacao DTOs
    public class RegistrarImpugnacaoDTO
    {
        public int CalendarioId { get; set; }
        public int ChapaId { get; set; }
        public int SolicitanteId { get; set; }
        public string Motivo { get; set; }
        public string Fundamentacao { get; set; }
        public List<string> DocumentosAnexos { get; set; }
        public int UsuarioRegistroId { get; set; }
    }

    public class RegistrarDefesaImpugnacaoDTO
    {
        public string ConteudoDefesa { get; set; }
        public List<string> DocumentosDefesa { get; set; }
        public int ApresentadaPorId { get; set; }
        public int UsuarioRegistroId { get; set; }
    }

    public class JulgarImpugnacaoDTO
    {
        public DecisaoJulgamento Decisao { get; set; }
        public string Fundamentacao { get; set; }
        public int RelatorId { get; set; }
        public int UsuarioJulgamentoId { get; set; }
    }

    public class RegistrarRecursoImpugnacaoDTO
    {
        public string MotivoRecurso { get; set; }
        public string Fundamentacao { get; set; }
        public List<string> DocumentosRecurso { get; set; }
        public int RecorrenteId { get; set; }
        public int UsuarioRegistroId { get; set; }
    }

    public class JulgarRecursoDTO
    {
        public DecisaoJulgamento Decisao { get; set; }
        public string Fundamentacao { get; set; }
        public int RelatorId { get; set; }
        public int UsuarioJulgamentoId { get; set; }
    }

    // Votacao DTOs
    public class AbrirVotacaoDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public string UfSigla { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime DataFechamento { get; set; }
        public int UsuarioAberturaId { get; set; }
    }

    public class FecharVotacaoDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public bool IniciarApuracao { get; set; }
        public int UsuarioFechamentoId { get; set; }
    }

    public class RegistrarVotoDTO
    {
        public int SessaoVotacaoId { get; set; }
        public int? ChapaId { get; set; }
        public bool VotoEmBranco { get; set; }
        public bool VotoNulo { get; set; }
        public int EleitorId { get; set; }
        public string IpOrigem { get; set; }
        public string UserAgent { get; set; }
    }

    public class ConfigurarSegundoTurnoDTO
    {
        public int CalendarioId { get; set; }
        public DateTime DataSegundoTurno { get; set; }
        public List<int> ChapasSegundoTurno { get; set; }
        public int UsuarioConfiguradorId { get; set; }
    }

    // Comissao DTOs
    public class CriarComissaoDTO
    {
        public string Nome { get; set; }
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public string TipoComissao { get; set; }
        public List<int> MembrosIds { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }

    public class AtualizarComissaoDTO
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    public class AlterarStatusDTO
    {
        public bool Ativo { get; set; }
        public string Motivo { get; set; }
    }

    public class AdicionarMembroComissaoDTO
    {
        public int ProfissionalId { get; set; }
        public string Cargo { get; set; }
        public DateTime DataNomeacao { get; set; }
        public int UsuarioNomeacaoId { get; set; }
    }

    public class RemoverMembroDTO
    {
        public string Motivo { get; set; }
        public int UsuarioRemocaoId { get; set; }
    }

    public class AtualizarCargoMembroDTO
    {
        public string NovoCargo { get; set; }
        public string Motivo { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    public class RegistrarAtividadeComissaoDTO
    {
        public string TipoAtividade { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAtividade { get; set; }
        public int RegistradoPorId { get; set; }
    }

    public class RegistrarDeliberacaoDTO
    {
        public string Assunto { get; set; }
        public string Deliberacao { get; set; }
        public string Fundamentacao { get; set; }
        public int RelatorId { get; set; }
    }

    public class AgendarReuniaoDTO
    {
        public DateTime DataReuniao { get; set; }
        public string Local { get; set; }
        public string Pauta { get; set; }
        public List<int> ParticipantesIds { get; set; }
        public int ConvocadaPorId { get; set; }
    }

    public class RegistrarAtaReuniaoDTO
    {
        public string ConteudoAta { get; set; }
        public List<string> Deliberacoes { get; set; }
        public List<int> PresentesIds { get; set; }
        public int SecretarioId { get; set; }
    }

    // Membro Chapa DTOs
    public class AceitarConviteDTO
    {
        public string TokenConvite { get; set; }
        public int ProfissionalId { get; set; }
        public bool Aceito { get; set; }
    }

    public class RecusarConviteDTO
    {
        public string Motivo { get; set; }
        public int ProfissionalId { get; set; }
    }

    public class OrdenarMembrosDTO
    {
        public List<OrdemMembroDTO> NovaOrdem { get; set; }
        public int UsuarioOrdenacaoId { get; set; }
    }

    public class OrdemMembroDTO
    {
        public int MembroId { get; set; }
        public int NovaOrdem { get; set; }
    }

    public class AtualizarCargoMembroChapaDTO
    {
        public string NovoCargo { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    public class ValidarElegibilidadeDTO
    {
        public int ProfissionalId { get; set; }
        public int CalendarioId { get; set; }
    }

    public class AnexarDocumentoMembroDTO
    {
        public string TipoDocumento { get; set; }
        public string NomeArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
        public int UsuarioUploadId { get; set; }
    }

    public class AnexarDocumentoImpugnacaoDTO : AnexarDocumentoMembroDTO { }

    // Relatorio DTOs
    public class ComparativoChapaDTO
    {
        public List<int> ChapaIds { get; set; }
        public int CalendarioId { get; set; }
        public bool IncluirMembros { get; set; }
        public bool IncluirPropostas { get; set; }
        public bool IncluirHistorico { get; set; }
    }

    public class ComparativoEleicoesDTO
    {
        public List<int> CalendarioIds { get; set; }
        public int? UfId { get; set; }
        public List<string> MetricasComparar { get; set; }
        public bool IncluirGraficos { get; set; }
    }

    public class RelatorioPersonalizadoDTO
    {
        public string TipoRelatorio { get; set; }
        public int CalendarioId { get; set; }
        public Dictionary<string, object> Parametros { get; set; }
        public List<string> SeçõesIncluir { get; set; }
        public string FormatoSaida { get; set; }
        public int SolicitadoPorId { get; set; }
        public bool AgendarEnvio { get; set; }
        public string EmailDestino { get; set; }
    }

    public class AgendarRelatorioDTO
    {
        public string TipoRelatorio { get; set; }
        public Dictionary<string, object> Parametros { get; set; }
        public bool Recorrente { get; set; }
        public string CronExpression { get; set; }
        public DateTime? DataExecucao { get; set; }
        public List<string> EmailsDestino { get; set; }
        public int AgendadoPorId { get; set; }
    }

    // Comuns para Email Jobs e notificações
    public class EmailModel
    {
        public List<string> Para { get; set; } = new List<string>();
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public string Assunto { get; set; } = "";
        public string TemplateId { get; set; } = "";
        public Dictionary<string, string>? ParametrosTemplate { get; set; }
        public EmailPrioridade Prioridade { get; set; } = EmailPrioridade.Normal;
        public List<AnexoEmail>? Anexos { get; set; }
    }

    public class AnexoEmail
    {
        public string NomeArquivo { get; set; } = "";
        public byte[] Conteudo { get; set; } = new byte[0];
        public string TipoConteudo { get; set; } = "";
    }

    public enum EmailPrioridade
    {
        Baixa = 1,
        Normal = 2,
        Alta = 3,
        Urgente = 4
    }

    public enum DecisaoJulgamento
    {
        Procedente = 1,
        ImprocedenteParcial = 2,
        Improcedente = 3,
        Arquivado = 4
    }

    public enum TipoVoto
    {
        Valido = 1,
        Branco = 2,
        Nulo = 3
    }
    // Result DTOs
    public class MembroChapaDetalhadoDTO
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public string Nome { get; set; }
        public string Cargo { get; set; }
        public int OrdemExibicao { get; set; }
        public bool ConviteAceito { get; set; }
        public DateTime? DataAceite { get; set; }
    }

    public class ConviteMembroDTO
    {
        public int Id { get; set; }
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public string Cargo { get; set; }
        public string TokenConvite { get; set; }
        public DateTime DataConvite { get; set; }
        public DateTime? DataExpiracao { get; set; }
    }

    public class ValidacaoMembroResult
    {
        public bool EhElegivel { get; set; }
        public List<string> MotivosPendencias { get; set; }
        public DateTime DataValidacao { get; set; }
    }

    public class PendenciaMembroDTO
    {
        public string TipoPendencia { get; set; }
        public string Descricao { get; set; }
        public bool Impeditiva { get; set; }
    }

    public class DenunciaDTO
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public string TipoDenuncia { get; set; }
        public string Status { get; set; }
        public DateTime DataRegistro { get; set; }
    }

    public class EstatisticasDenunciasDTO
    {
        public int TotalDenuncias { get; set; }
        public int DenunciasPendentes { get; set; }
        public int DenunciasJulgadas { get; set; }
        public int DenunciasProcedentes { get; set; }
        public int DenunciasImprocedentes { get; set; }
    }

    public class PedidoImpugnacaoDTO
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public string Status { get; set; }
        public DateTime DataSolicitacao { get; set; }
        public bool FoiJulgado { get; set; }
        public string Decisao { get; set; }
    }

    public class QuantidadeImpugnacaoPorUfDTO
    {
        public int UfId { get; set; }
        public string UfSigla { get; set; }
        public int QuantidadePedidos { get; set; }
        public int Deferidos { get; set; }
        public int Indeferidos { get; set; }
    }

    public class ComprovanteVotoDTO
    {
        public string ProtocoloComprovante { get; set; }
        public string CodigoVerificacao { get; set; }
        public DateTime DataHoraVoto { get; set; }
        public string HashVoto { get; set; }
    }

    public class StatusVotacaoDTO
    {
        public bool VotacaoAberta { get; set; }
        public DateTime? DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public decimal PercentualParticipacao { get; set; }
    }

    public class EstatisticasVotacaoDTO
    {
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public int TotalAbstencoes { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosValidos { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public Dictionary<int, int> VotosPorChapa { get; set; }
    }

    public class ResultadoApuracaoDTO
    {
        public int Id { get; set; }
        public int CalendarioId { get; set; }
        public DateTime DataApuracao { get; set; }
        public int TotalVotantes { get; set; }
        public int? ChapaVencedoraId { get; set; }
        public bool NecessitaSegundoTurno { get; set; }
        public List<ResultadoChapaDTO> ResultadosChapas { get; set; }
    }

    public class ResultadoChapaDTO
    {
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public int QuantidadeVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int Posicao { get; set; }
    }

    public class ValidacaoElegibilidadeResult
    {
        public bool EhElegivel { get; set; }
        public List<string> MotivosPendencias { get; set; }
        public List<RestricaoElegibilidade> Restricoes { get; set; }
    }

    public class RestricaoElegibilidade
    {
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public bool Impeditiva { get; set; }
        public DateTime? DataResolucao { get; set; }
    }

    public class EmailMessage
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; }
        public List<string> Attachments { get; set; }
    }

    public class TipoParticipacaoMembro
    {
        public bool ParticipaNaChapa { get; set; }
        public int? ChapaId { get; set; }
        public string NomeCargo { get; set; }
    }

    public class AgendamentoRelatorio
    {
        public int Id { get; set; }
        public string TipoRelatorio { get; set; }
        public bool Ativo { get; set; }
        public DateTime ProximaExecucao { get; set; }
        public int TotalExecucoes { get; set; }
    }
}