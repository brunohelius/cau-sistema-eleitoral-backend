using System;
using System.Threading.Tasks;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de arquivos
    /// </summary>
    public interface IArquivoService
    {
        Task<string> SalvarArquivoAsync(byte[] conteudo, string nomeArquivo, string pasta);
        Task<byte[]> ObterArquivoAsync(string caminho);
        Task<bool> ExcluirArquivoAsync(string caminho);
        Task<bool> ArquivoExisteAsync(string caminho);
        Task<string> ObterUrlArquivoAsync(string caminho);
        Task<long> ObterTamanhoArquivoAsync(string caminho);
        string ObterExtensaoArquivo(string nomeArquivo);
        bool ValidarTipoArquivo(string nomeArquivo, string[] tiposPermitidos);
        bool ValidarTamanhoArquivo(long tamanhoBytes, long tamanhoMaximo);
    }
}