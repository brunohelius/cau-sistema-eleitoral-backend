-- Migration Inicial do Sistema Eleitoral CAU
-- Data: 2025-08-06
-- Descrição: Criação das tabelas principais para o sistema eleitoral

-- =======================
-- TABELAS DE CONFIGURAÇÃO
-- =======================

-- Estados/UFs
CREATE TABLE IF NOT EXISTS ufs (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(2) NOT NULL UNIQUE,
    nome VARCHAR(100) NOT NULL,
    regiao VARCHAR(20),
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

-- Eleições
CREATE TABLE IF NOT EXISTS eleicoes (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    nome VARCHAR(200) NOT NULL,
    descricao TEXT,
    ano INTEGER NOT NULL,
    status INTEGER DEFAULT 1, -- 1=Planejada, 2=Ativa, 3=EmAndamento, 4=Encerrada, 5=Homologada
    data_inicio DATE NOT NULL,
    data_fim DATE NOT NULL,
    data_votacao_inicio TIMESTAMP,
    data_votacao_fim TIMESTAMP,
    data_posse DATE,
    resolucao_normativa VARCHAR(100),
    configuracao_json JSONB,
    permite_voto_online BOOLEAN DEFAULT true,
    permite_voto_presencial BOOLEAN DEFAULT false,
    total_eleitores INTEGER,
    total_votantes INTEGER,
    percentual_participacao DECIMAL(5,2),
    data_homologacao TIMESTAMP,
    motivo_anulacao TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- Calendário Eleitoral
CREATE TABLE IF NOT EXISTS calendarios_eleitorais (
    id SERIAL PRIMARY KEY,
    eleicao_id INTEGER REFERENCES eleicoes(id),
    uf_id INTEGER REFERENCES ufs(id),
    nome VARCHAR(200) NOT NULL,
    descricao TEXT,
    data_inicio DATE NOT NULL,
    data_fim DATE NOT NULL,
    prazo_dias INTEGER,
    ordem_execucao INTEGER,
    obrigatorio BOOLEAN DEFAULT true,
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- =======================
-- TABELAS DE USUÁRIOS
-- =======================

-- Usuários
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    cpf VARCHAR(14),
    telefone VARCHAR(20),
    usuario_cau_id INTEGER,
    numero_registro VARCHAR(50),
    data_ultimo_login TIMESTAMP,
    email_verificado BOOLEAN DEFAULT false,
    data_criacao TIMESTAMP DEFAULT NOW(),
    data_atualizacao TIMESTAMP,
    ativo BOOLEAN DEFAULT true
);

-- Permissões
CREATE TABLE IF NOT EXISTS permissoes (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    descricao VARCHAR(200),
    codigo VARCHAR(50) NOT NULL UNIQUE,
    data_criacao TIMESTAMP DEFAULT NOW(),
    data_atualizacao TIMESTAMP,
    ativo BOOLEAN DEFAULT true
);

-- Relacionamento Usuários-Permissões
CREATE TABLE IF NOT EXISTS usuario_permissoes (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id),
    permissao_id INTEGER NOT NULL REFERENCES permissoes(id),
    data_expiracao TIMESTAMP,
    data_criacao TIMESTAMP DEFAULT NOW(),
    data_atualizacao TIMESTAMP,
    ativo BOOLEAN DEFAULT true,
    UNIQUE(usuario_id, permissao_id)
);

-- Logs de Usuário
CREATE TABLE IF NOT EXISTS logs_usuario (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id),
    acao VARCHAR(50) NOT NULL,
    detalhes VARCHAR(500),
    endereco_ip VARCHAR(50),
    user_agent VARCHAR(200),
    data_criacao TIMESTAMP DEFAULT NOW(),
    data_atualizacao TIMESTAMP,
    ativo BOOLEAN DEFAULT true
);

-- =======================
-- TABELAS DE PROFISSIONAIS
-- =======================

-- Profissionais
CREATE TABLE IF NOT EXISTS profissionais (
    id SERIAL PRIMARY KEY,
    cpf VARCHAR(14) NOT NULL UNIQUE,
    nome VARCHAR(200) NOT NULL,
    email VARCHAR(150) NOT NULL,
    telefone VARCHAR(20),
    telefone_celular VARCHAR(20),
    numero_registro VARCHAR(50) NOT NULL,
    uf_registro VARCHAR(2) NOT NULL,
    data_registro DATE,
    data_formatura DATE,
    instituicao_formacao VARCHAR(200),
    registro_ativo BOOLEAN DEFAULT true,
    adimplente_situacao_financeira BOOLEAN DEFAULT true,
    adimplente_situacao_etica BOOLEAN DEFAULT true,
    data_nascimento DATE,
    genero VARCHAR(20),
    etnia VARCHAR(50),
    lgbtqi BOOLEAN DEFAULT false,
    possui_deficiencia BOOLEAN DEFAULT false,
    tipo_deficiencia VARCHAR(100),
    endereco_completo TEXT,
    cidade VARCHAR(100),
    estado VARCHAR(2),
    cep VARCHAR(10),
    foto_url VARCHAR(500),
    ultimo_acesso TIMESTAMP,
    email_verificado BOOLEAN DEFAULT false,
    telefone_verificado BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- =======================
-- TABELAS DE CHAPAS
-- =======================

-- Chapas de Eleição
CREATE TABLE IF NOT EXISTS chapas_eleicao (
    id SERIAL PRIMARY KEY,
    eleicao_id INTEGER NOT NULL REFERENCES eleicoes(id),
    uf_id INTEGER REFERENCES ufs(id),
    numero INTEGER,
    nome VARCHAR(200) NOT NULL,
    slogan VARCHAR(500),
    plataforma TEXT,
    status INTEGER DEFAULT 1, -- Status da chapa
    data_inscricao TIMESTAMP DEFAULT NOW(),
    data_confirmacao TIMESTAMP,
    data_homologacao TIMESTAMP,
    motivo_indeferimento TEXT,
    votos_recebidos INTEGER DEFAULT 0,
    percentual_votos DECIMAL(5,2),
    posicao_final INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- Membros das Chapas
CREATE TABLE IF NOT EXISTS membros_chapa (
    id SERIAL PRIMARY KEY,
    chapa_id INTEGER NOT NULL REFERENCES chapas_eleicao(id),
    profissional_id INTEGER NOT NULL REFERENCES profissionais(id),
    cargo VARCHAR(50) NOT NULL, -- coordenador, vice-coordenador, conselheiro, suplente
    ordem INTEGER,
    situacao VARCHAR(50) DEFAULT 'pendente', -- pendente, aceito, recusado
    data_convite TIMESTAMP DEFAULT NOW(),
    data_resposta TIMESTAMP,
    motivo_recusa TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100),
    UNIQUE(chapa_id, profissional_id)
);

-- =======================
-- TABELAS DE COMISSÃO ELEITORAL
-- =======================

-- Comissão Eleitoral
CREATE TABLE IF NOT EXISTS comissoes_eleitorais (
    id SERIAL PRIMARY KEY,
    eleicao_id INTEGER NOT NULL REFERENCES eleicoes(id),
    uf_id INTEGER REFERENCES ufs(id),
    tipo VARCHAR(20) NOT NULL, -- nacional, estadual
    nome VARCHAR(200) NOT NULL,
    status INTEGER DEFAULT 1,
    data_constituicao DATE,
    data_dissolucao DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- Membros da Comissão Eleitoral
CREATE TABLE IF NOT EXISTS membros_comissao (
    id SERIAL PRIMARY KEY,
    comissao_id INTEGER NOT NULL REFERENCES comissoes_eleitorais(id),
    profissional_id INTEGER NOT NULL REFERENCES profissionais(id),
    cargo VARCHAR(50) NOT NULL, -- coordenador, membro, suplente
    situacao VARCHAR(50) DEFAULT 'ativo',
    data_designacao DATE NOT NULL,
    data_posse DATE,
    data_exoneracao DATE,
    motivo_exoneracao TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- =======================
-- TABELAS DE DENÚNCIAS
-- =======================

-- Denúncias
CREATE TABLE IF NOT EXISTS denuncias (
    id SERIAL PRIMARY KEY,
    eleicao_id INTEGER NOT NULL REFERENCES eleicoes(id),
    denunciante_id INTEGER REFERENCES profissionais(id),
    protocolo VARCHAR(50) UNIQUE NOT NULL,
    tipo VARCHAR(50) NOT NULL,
    assunto VARCHAR(200) NOT NULL,
    descricao TEXT NOT NULL,
    status INTEGER DEFAULT 1,
    data_protocolo TIMESTAMP DEFAULT NOW(),
    data_admissibilidade TIMESTAMP,
    parecer_admissibilidade TEXT,
    admitida BOOLEAN,
    prazo_defesa_dias INTEGER DEFAULT 10,
    data_limite_defesa DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- Impugnações
CREATE TABLE IF NOT EXISTS impugnacoes (
    id SERIAL PRIMARY KEY,
    eleicao_id INTEGER NOT NULL REFERENCES eleicoes(id),
    chapa_id INTEGER REFERENCES chapas_eleicao(id),
    profissional_id INTEGER REFERENCES profissionais(id),
    requerente_id INTEGER NOT NULL REFERENCES profissionais(id),
    protocolo VARCHAR(50) UNIQUE NOT NULL,
    tipo VARCHAR(50) NOT NULL,
    motivo TEXT NOT NULL,
    fundamentacao TEXT,
    status INTEGER DEFAULT 1,
    data_protocolo TIMESTAMP DEFAULT NOW(),
    prazo_defesa_dias INTEGER DEFAULT 3,
    data_limite_defesa DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- =======================
-- ÍNDICES
-- =======================

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_cpf ON usuarios(cpf);
CREATE INDEX IF NOT EXISTS idx_profissionais_cpf ON profissionais(cpf);
CREATE INDEX IF NOT EXISTS idx_profissionais_registro ON profissionais(numero_registro, uf_registro);
CREATE INDEX IF NOT EXISTS idx_chapas_eleicao ON chapas_eleicao(eleicao_id);
CREATE INDEX IF NOT EXISTS idx_membros_chapa ON membros_chapa(chapa_id, profissional_id);
CREATE INDEX IF NOT EXISTS idx_denuncias_eleicao ON denuncias(eleicao_id);
CREATE INDEX IF NOT EXISTS idx_impugnacoes_eleicao ON impugnacoes(eleicao_id);

-- =======================
-- DADOS INICIAIS
-- =======================

-- Inserir UFs
INSERT INTO ufs (codigo, nome, regiao) VALUES
('AC', 'Acre', 'Norte'),
('AL', 'Alagoas', 'Nordeste'),
('AP', 'Amapá', 'Norte'),
('AM', 'Amazonas', 'Norte'),
('BA', 'Bahia', 'Nordeste'),
('CE', 'Ceará', 'Nordeste'),
('DF', 'Distrito Federal', 'Centro-Oeste'),
('ES', 'Espírito Santo', 'Sudeste'),
('GO', 'Goiás', 'Centro-Oeste'),
('MA', 'Maranhão', 'Nordeste'),
('MT', 'Mato Grosso', 'Centro-Oeste'),
('MS', 'Mato Grosso do Sul', 'Centro-Oeste'),
('MG', 'Minas Gerais', 'Sudeste'),
('PA', 'Pará', 'Norte'),
('PB', 'Paraíba', 'Nordeste'),
('PR', 'Paraná', 'Sul'),
('PE', 'Pernambuco', 'Nordeste'),
('PI', 'Piauí', 'Nordeste'),
('RJ', 'Rio de Janeiro', 'Sudeste'),
('RN', 'Rio Grande do Norte', 'Nordeste'),
('RS', 'Rio Grande do Sul', 'Sul'),
('RO', 'Rondônia', 'Norte'),
('RR', 'Roraima', 'Norte'),
('SC', 'Santa Catarina', 'Sul'),
('SP', 'São Paulo', 'Sudeste'),
('SE', 'Sergipe', 'Nordeste'),
('TO', 'Tocantins', 'Norte')
ON CONFLICT (codigo) DO NOTHING;

-- Inserir permissões básicas
INSERT INTO permissoes (nome, codigo, descricao) VALUES
('Administrador Geral', 'ADMIN_GERAL', 'Acesso total ao sistema'),
('Comissão Eleitoral Nacional', 'COMISSAO_NACIONAL', 'Membro da comissão eleitoral nacional'),
('Comissão Eleitoral Estadual', 'COMISSAO_ESTADUAL', 'Membro da comissão eleitoral estadual'),
('Profissional', 'PROFISSIONAL', 'Profissional habilitado para participar das eleições'),
('Visualizar Eleições', 'VIEW_ELEICOES', 'Visualizar informações das eleições'),
('Gerenciar Chapas', 'MANAGE_CHAPAS', 'Criar e gerenciar chapas eleitorais'),
('Processar Denúncias', 'PROCESS_DENUNCIAS', 'Processar denúncias eleitorais'),
('Processar Impugnações', 'PROCESS_IMPUGNACOES', 'Processar impugnações eleitorais')
ON CONFLICT (codigo) DO NOTHING;

COMMIT;