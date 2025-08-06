using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaEleitoral.Application.DTOs.Auth;
using SistemaEleitoral.Application.Interfaces;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace SistemaEleitoral.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IHistoricoAcessoRepository _historicoAcessoRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            IHistoricoAcessoRepository historicoAcessoRepository,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _usuarioRepository = usuarioRepository;
            _historicoAcessoRepository = historicoAcessoRepository;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string ipAddress, string userAgent)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);
            
            var historico = new HistoricoAcesso
            {
                IpAcesso = ipAddress,
                UserAgent = userAgent,
                TipoAcesso = TipoAcesso.TentativaLogin
            };

            if (usuario == null || !BC.Verify(request.Senha, usuario.Senha))
            {
                historico.Sucesso = false;
                historico.Observacao = "Email ou senha inválidos";
                
                if (usuario != null)
                {
                    historico.UsuarioId = usuario.Id;
                    await _historicoAcessoRepository.AddAsync(historico);
                }
                
                throw new UnauthorizedAccessException("Email ou senha inválidos");
            }

            if (usuario.Status != StatusUsuario.Ativo)
            {
                historico.UsuarioId = usuario.Id;
                historico.Sucesso = false;
                historico.Observacao = $"Usuário {usuario.Status}";
                await _historicoAcessoRepository.AddAsync(historico);
                
                throw new UnauthorizedAccessException($"Usuário {usuario.Status}");
            }

            // Login bem-sucedido
            usuario.AtualizarUltimoAcesso();
            await _usuarioRepository.UpdateAsync(usuario);

            historico.UsuarioId = usuario.Id;
            historico.TipoAcesso = TipoAcesso.Login;
            historico.Sucesso = true;
            await _historicoAcessoRepository.AddAsync(historico);

            var token = GenerateJwtToken(usuario);

            return new LoginResponseDto
            {
                Token = token,
                Usuario = new UsuarioDto
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    TipoUsuario = usuario.TipoUsuario.ToString(),
                    ProfissionalId = usuario.ProfissionalId,
                    ConselheiroId = usuario.ConselheiroId
                }
            };
        }

        public async Task<bool> LogoutAsync(int usuarioId, string ipAddress, string userAgent)
        {
            var historico = new HistoricoAcesso
            {
                UsuarioId = usuarioId,
                IpAcesso = ipAddress,
                UserAgent = userAgent,
                TipoAcesso = TipoAcesso.Logout,
                Sucesso = true
            };

            await _historicoAcessoRepository.AddAsync(historico);
            return true;
        }

        public async Task<bool> RecuperarSenhaAsync(string email)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(email);
            
            if (usuario == null)
            {
                // Não revelamos se o email existe ou não por segurança
                return true;
            }

            usuario.GerarTokenRecuperacao();
            await _usuarioRepository.UpdateAsync(usuario);

            // Enviar email com link de recuperação
            var linkRecuperacao = $"{_configuration["AppUrl"]}/recuperar-senha?token={usuario.TokenRecuperacao}";
            
            await _emailService.EnviarEmailAsync(
                usuario.Email,
                "Recuperação de Senha - Sistema Eleitoral CAU",
                $@"
                <h2>Recuperação de Senha</h2>
                <p>Olá {usuario.Nome},</p>
                <p>Recebemos uma solicitação de recuperação de senha para sua conta.</p>
                <p>Clique no link abaixo para criar uma nova senha:</p>
                <a href='{linkRecuperacao}'>Recuperar Senha</a>
                <p>Este link é válido por 24 horas.</p>
                <p>Se você não solicitou esta recuperação, ignore este email.</p>
                "
            );

            var historico = new HistoricoAcesso
            {
                UsuarioId = usuario.Id,
                TipoAcesso = TipoAcesso.RecuperacaoSenha,
                Sucesso = true,
                Observacao = "Token de recuperação gerado"
            };
            await _historicoAcessoRepository.AddAsync(historico);

            return true;
        }

        public async Task<bool> RedefinirSenhaAsync(RedefinirSenhaDto request)
        {
            var usuario = await _usuarioRepository.GetByTokenRecuperacaoAsync(request.Token);
            
            if (usuario == null || !usuario.TokenValido(request.Token))
            {
                throw new UnauthorizedAccessException("Token inválido ou expirado");
            }

            usuario.Senha = BC.HashPassword(request.NovaSenha);
            usuario.TokenRecuperacao = null;
            usuario.ValidadeToken = null;
            
            await _usuarioRepository.UpdateAsync(usuario);

            var historico = new HistoricoAcesso
            {
                UsuarioId = usuario.Id,
                TipoAcesso = TipoAcesso.AlteracaoSenha,
                Sucesso = true,
                Observacao = "Senha redefinida via token de recuperação"
            };
            await _historicoAcessoRepository.AddAsync(historico);

            return true;
        }

        public async Task<bool> AlterarSenhaAsync(int usuarioId, AlterarSenhaDto request)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            
            if (usuario == null)
            {
                throw new NotFoundException("Usuário não encontrado");
            }

            if (!BC.Verify(request.SenhaAtual, usuario.Senha))
            {
                throw new UnauthorizedAccessException("Senha atual incorreta");
            }

            usuario.Senha = BC.HashPassword(request.NovaSenha);
            await _usuarioRepository.UpdateAsync(usuario);

            var historico = new HistoricoAcesso
            {
                UsuarioId = usuario.Id,
                TipoAcesso = TipoAcesso.AlteracaoSenha,
                Sucesso = true,
                Observacao = "Senha alterada pelo usuário"
            };
            await _historicoAcessoRepository.AddAsync(historico);

            return true;
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("TipoUsuario", usuario.TipoUsuario.ToString()),
                new Claim("ProfissionalId", usuario.ProfissionalId?.ToString() ?? ""),
                new Claim("ConselheiroId", usuario.ConselheiroId?.ToString() ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpirationHours"])),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}