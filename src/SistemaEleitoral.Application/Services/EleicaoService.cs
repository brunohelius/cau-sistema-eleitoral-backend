using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SistemaEleitoral.Application.DTOs.Eleicao;
using SistemaEleitoral.Application.Interfaces;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using AutoMapper;

namespace SistemaEleitoral.Application.Services
{
    public class EleicaoService : IEleicaoService
    {
        private readonly IEleicaoRepository _eleicaoRepository;
        private readonly IChapaEleicaoRepository _chapaRepository;
        private readonly IMembroChapaRepository _membroRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;

        public EleicaoService(
            IEleicaoRepository eleicaoRepository,
            IChapaEleicaoRepository chapaRepository,
            IMembroChapaRepository membroRepository,
            IEmailService emailService,
            IMapper mapper)
        {
            _eleicaoRepository = eleicaoRepository;
            _chapaRepository = chapaRepository;
            _membroRepository = membroRepository;
            _emailService = emailService;
            _mapper = mapper;
        }

        public async Task<EleicaoDto> CriarEleicaoAsync(CriarEleicaoDto dto)
        {
            var eleicao = new Eleicao
            {
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                Ano = dto.Ano,
                TipoEleicao = dto.TipoEleicao,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                DataInicioInscricao = dto.DataInicioInscricao,
                DataFimInscricao = dto.DataFimInscricao,
                DataInicioVotacao = dto.DataInicioVotacao,
                DataFimVotacao = dto.DataFimVotacao,
                DataApuracao = dto.DataApuracao,
                DataPosse = dto.DataPosse,
                NumeroVagas = dto.NumeroVagas,
                NumeroSuplentes = dto.NumeroSuplentes,
                QuorumMinimo = dto.QuorumMinimo,
                PermiteReeleicao = dto.PermiteReeleicao,
                MandatoAnos = dto.MandatoAnos,
                FilialId = dto.FilialId,
                CalendarioId = dto.CalendarioId
            };

            await _eleicaoRepository.AddAsync(eleicao);
            
            // Notificar sobre nova eleição
            await NotificarNovaEleicao(eleicao);
            
            return _mapper.Map<EleicaoDto>(eleicao);
        }

        public async Task<EleicaoDto> AtualizarEleicaoAsync(int id, AtualizarEleicaoDto dto)
        {
            var eleicao = await _eleicaoRepository.GetByIdAsync(id);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            if (eleicao.Status != StatusEleicao.Planejamento)
                throw new BusinessException("Não é possível alterar eleição após início das inscrições");

            eleicao.Titulo = dto.Titulo;
            eleicao.Descricao = dto.Descricao;
            eleicao.DataInicioInscricao = dto.DataInicioInscricao;
            eleicao.DataFimInscricao = dto.DataFimInscricao;
            eleicao.DataInicioVotacao = dto.DataInicioVotacao;
            eleicao.DataFimVotacao = dto.DataFimVotacao;
            eleicao.DataApuracao = dto.DataApuracao;
            eleicao.NumeroVagas = dto.NumeroVagas;
            eleicao.NumeroSuplentes = dto.NumeroSuplentes;
            eleicao.QuorumMinimo = dto.QuorumMinimo;

            await _eleicaoRepository.UpdateAsync(eleicao);
            
            return _mapper.Map<EleicaoDto>(eleicao);
        }

        public async Task<IEnumerable<EleicaoDto>> ListarEleicoesAsync(FiltroEleicaoDto filtro)
        {
            var eleicoes = await _eleicaoRepository.GetAllAsync();
            
            if (filtro.Ano.HasValue)
                eleicoes = eleicoes.Where(e => e.Ano == filtro.Ano.Value);
                
            if (filtro.TipoEleicao.HasValue)
                eleicoes = eleicoes.Where(e => e.TipoEleicao == filtro.TipoEleicao.Value);
                
            if (filtro.Status.HasValue)
                eleicoes = eleicoes.Where(e => e.Status == filtro.Status.Value);
                
            if (filtro.FilialId.HasValue)
                eleicoes = eleicoes.Where(e => e.FilialId == filtro.FilialId.Value);
                
            return _mapper.Map<IEnumerable<EleicaoDto>>(eleicoes);
        }

        public async Task<EleicaoDetalhadaDto> ObterEleicaoDetalhadaAsync(int id)
        {
            var eleicao = await _eleicaoRepository.GetByIdComChapasAsync(id);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            var dto = _mapper.Map<EleicaoDetalhadaDto>(eleicao);
            
            // Adicionar estatísticas
            dto.TotalChapasInscritas = eleicao.Chapas.Count;
            dto.TotalChapasHomologadas = eleicao.Chapas.Count(c => c.Status == StatusChapa.Homologada);
            dto.TotalVotosRecebidos = eleicao.Chapas.Sum(c => c.TotalVotos);
            
            return dto;
        }

        public async Task<bool> AbrirInscricoesAsync(int eleicaoId)
        {
            var eleicao = await _eleicaoRepository.GetByIdAsync(eleicaoId);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            eleicao.AbrirInscricoes();
            await _eleicaoRepository.UpdateAsync(eleicao);
            
            // Notificar abertura de inscrições
            await NotificarAberturaInscricoes(eleicao);
            
            return true;
        }

        public async Task<bool> FecharInscricoesAsync(int eleicaoId)
        {
            var eleicao = await _eleicaoRepository.GetByIdAsync(eleicaoId);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            eleicao.FecharInscricoes();
            await _eleicaoRepository.UpdateAsync(eleicao);
            
            return true;
        }

        public async Task<bool> AbrirVotacaoAsync(int eleicaoId)
        {
            var eleicao = await _eleicaoRepository.GetByIdComChapasAsync(eleicaoId);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            // Verificar se há chapas homologadas
            if (!eleicao.Chapas.Any(c => c.Status == StatusChapa.Homologada))
                throw new BusinessException("Não há chapas homologadas para iniciar a votação");
                
            eleicao.AbrirVotacao();
            await _eleicaoRepository.UpdateAsync(eleicao);
            
            // Notificar abertura de votação
            await NotificarAberturaVotacao(eleicao);
            
            return true;
        }

        public async Task<bool> FecharVotacaoAsync(int eleicaoId)
        {
            var eleicao = await _eleicaoRepository.GetByIdAsync(eleicaoId);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            eleicao.FecharVotacao();
            await _eleicaoRepository.UpdateAsync(eleicao);
            
            return true;
        }

        public async Task<ChapaEleicaoDto> InscreverChapaAsync(InscreverChapaDto dto)
        {
            var eleicao = await _eleicaoRepository.GetByIdAsync(dto.EleicaoId);
            
            if (eleicao == null)
                throw new NotFoundException("Eleição não encontrada");
                
            if (!eleicao.PodeInscreverChapa())
                throw new BusinessException("Período de inscrições encerrado ou não iniciado");
                
            // Verificar se o número da chapa já existe
            var numeroExiste = await _chapaRepository.ExisteNumeroAsync(dto.EleicaoId, dto.Numero);
            if (numeroExiste)
                throw new BusinessException($"Já existe uma chapa com o número {dto.Numero}");

            var chapa = new ChapaEleicao
            {
                EleicaoId = dto.EleicaoId,
                Numero = dto.Numero,
                Nome = dto.Nome,
                Slogan = dto.Slogan,
                PropostaResumo = dto.PropostaResumo,
                PropostaCompleta = dto.PropostaCompleta,
                FotoUrl = dto.FotoUrl
            };

            await _chapaRepository.AddAsync(chapa);
            
            // Adicionar membros
            foreach (var membroDto in dto.Membros)
            {
                var membro = new MembroChapa
                {
                    ChapaEleicaoId = chapa.Id,
                    ProfissionalId = membroDto.ProfissionalId,
                    TipoMembro = membroDto.TipoMembro,
                    Ordem = membroDto.Ordem,
                    Cargo = membroDto.Cargo,
                    Titular = membroDto.Titular,
                    MiniCurriculo = membroDto.MiniCurriculo
                };
                
                await _membroRepository.AddAsync(membro);
            }
            
            return _mapper.Map<ChapaEleicaoDto>(chapa);
        }

        public async Task<bool> HomologarChapaAsync(int chapaId)
        {
            var chapa = await _chapaRepository.GetByIdComMembrosAsync(chapaId);
            
            if (chapa == null)
                throw new NotFoundException("Chapa não encontrada");
                
            // Verificar documentação dos membros
            if (chapa.Membros.Any(m => !m.DocumentacaoCompleta))
                throw new BusinessException("Existem membros com documentação pendente");
                
            if (chapa.Membros.Any(m => !m.Elegivel))
                throw new BusinessException("Existem membros inelegíveis na chapa");
                
            chapa.Homologar();
            await _chapaRepository.UpdateAsync(chapa);
            
            // Notificar homologação
            await NotificarHomologacaoChapa(chapa);
            
            return true;
        }

        public async Task<bool> IndeferirChapaAsync(int chapaId, string motivo)
        {
            var chapa = await _chapaRepository.GetByIdAsync(chapaId);
            
            if (chapa == null)
                throw new NotFoundException("Chapa não encontrada");
                
            chapa.Indeferir(motivo);
            await _chapaRepository.UpdateAsync(chapa);
            
            // Notificar indeferimento
            await NotificarIndeferimentoChapa(chapa, motivo);
            
            return true;
        }

        private async Task NotificarNovaEleicao(Eleicao eleicao)
        {
            // Implementar envio de email
            await Task.CompletedTask;
        }

        private async Task NotificarAberturaInscricoes(Eleicao eleicao)
        {
            // Implementar envio de email
            await Task.CompletedTask;
        }

        private async Task NotificarAberturaVotacao(Eleicao eleicao)
        {
            // Implementar envio de email
            await Task.CompletedTask;
        }

        private async Task NotificarHomologacaoChapa(ChapaEleicao chapa)
        {
            // Implementar envio de email
            await Task.CompletedTask;
        }

        private async Task NotificarIndeferimentoChapa(ChapaEleicao chapa, string motivo)
        {
            // Implementar envio de email
            await Task.CompletedTask;
        }
    }
}