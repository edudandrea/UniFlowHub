export type Role = 'Admin' | 'RH' | 'TI' | 'Diretoria' | 'Compras' | 'Gestor' | 'Usuario';

export interface User {
  id: number;
  nome: string;
  cpf: string;
  email: string;
  role: Role;
  departamento: string;
  cargo: string;
  ativo: boolean;
  unidadeId?: number | null;
  unidadeNome: string;
  dataNascimento: string;
}

export type UserCreatePayload = Omit<User, 'id' | 'unidadeNome'> & { senha: string };
export type UserUpdatePayload = Omit<User, 'id' | 'unidadeNome'> & { senha?: string };
export type UserProfileUpdatePayload = Pick<User, 'nome' | 'cpf' | 'departamento' | 'cargo' | 'dataNascimento'>;

export interface Unidade {
  id: number;
  nome: string;
  cnpj: string;
  endereco: string;
  dataCadastro: string;
}

export type UnidadePayload = Pick<Unidade, 'nome' | 'cnpj' | 'endereco'>;

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface SolicitacaoRH {
  id: number;
  unidade: string;
  titulo: string;
  tipoSolicitacao: string;
  solicitante: string;
  departamento: string;
  descricao: string;
  anexossUrl: string;
  prioridade: string;
  responsavel: string;
  dataSolicitacao: string;
  dataEncerramento?: string | null;
  status: string;
  observacoes: string;
  observacoesEncerramento: string;
  satisfacaoNota?: number | null;
  satisfacaoComentario: string;
  dataAvaliacao?: string | null;
  avaliacaoPendente: boolean;
  userid: number;
}

export type SolicitacaoPayload = Omit<
  SolicitacaoRH,
  | 'id'
  | 'dataSolicitacao'
  | 'dataEncerramento'
  | 'observacoesEncerramento'
  | 'satisfacaoNota'
  | 'satisfacaoComentario'
  | 'dataAvaliacao'
  | 'avaliacaoPendente'
>;

export interface ChamadoTI {
  id: number;
  titulo: string;
  categoria: string;
  descricao: string;
  solicitante: string;
  unidade: string;
  departamento: string;
  prioridade: string;
  status: string;
  responsavel: string;
  acessoRemotoUrl: string;
  anexoImagemUrl: string;
  observacoes: string;
  observacoesEncerramento: string;
  satisfacaoNota?: number | null;
  satisfacaoComentario: string;
  dataAvaliacao?: string | null;
  avaliacaoPendente: boolean;
  dataAbertura: string;
  dataPrimeiroEncerramento?: string | null;
  dataReabertura?: string | null;
  dataEncerramento?: string | null;
  ultimaMovimentacao: string;
  reaberto: boolean;
  userid: number;
}

export interface ChamadoTIComunicacao {
  id: number;
  chamadoTIId: number;
  mensagem: string;
  autorNome: string;
  autorRole: string;
  autorUserId: number;
  dataCriacao: string;
}

export interface SolicitacaoRHComunicacao {
  id: number;
  solicitacaoRHId: number;
  mensagem: string;
  autorNome: string;
  autorRole: string;
  autorUserId: number;
  dataCriacao: string;
}

export interface SolicitacaoCompraComunicacao {
  id: number;
  solicitacaoCompraId: number;
  mensagem: string;
  autorNome: string;
  autorRole: string;
  autorUserId: number;
  dataCriacao: string;
}

export type ChamadoTIPayload = Omit<
  ChamadoTI,
  | 'id'
  | 'dataAbertura'
  | 'dataPrimeiroEncerramento'
  | 'dataReabertura'
  | 'dataEncerramento'
  | 'reaberto'
  | 'anexoImagemUrl'
  | 'observacoesEncerramento'
  | 'satisfacaoNota'
  | 'satisfacaoComentario'
  | 'dataAvaliacao'
  | 'avaliacaoPendente'
  | 'ultimaMovimentacao'
>;

export interface EquipamentoTI {
  id: number;
  tipo: string;
  patrimonio: string;
  modelo: string;
  serial: string;
  status: string;
  origem: string;
  destino: string;
  responsavel: string;
  dataMovimentacao: string;
  dataPrevistaRetorno?: string | null;
  observacoes: string;
  documentoUrl: string;
  userid: number;
}

export type EquipamentoTIPayload = Omit<EquipamentoTI, 'id' | 'dataMovimentacao' | 'documentoUrl' | 'userid'>;

export interface SolicitacaoCompra {
  id: number;
  titulo: string;
  categoria: string;
  descricao: string;
  solicitante: string;
  unidade: string;
  departamento: string;
  valorEstimado: number;
  fornecedorSugerido: string;
  prioridade: string;
  status: string;
  justificativa: string;
  observacoes: string;
  documentoUrl: string;
  dataSolicitacao: string;
  dataAprovacao?: string | null;
  dataEnvioCompras?: string | null;
  dataConclusao?: string | null;
  aprovador: string;
  comprador: string;
  observacoesAprovacao: string;
  observacoesCompras: string;
  userid: number;
}

export type SolicitacaoCompraPayload = Omit<
  SolicitacaoCompra,
  | 'id'
  | 'status'
  | 'documentoUrl'
  | 'dataSolicitacao'
  | 'dataAprovacao'
  | 'dataEnvioCompras'
  | 'dataConclusao'
  | 'aprovador'
  | 'comprador'
  | 'observacoesAprovacao'
  | 'observacoesCompras'
>;

export type SolicitacaoCompraUpdatePayload = Omit<
  SolicitacaoCompra,
  | 'id'
  | 'documentoUrl'
  | 'dataSolicitacao'
  | 'dataAprovacao'
  | 'dataEnvioCompras'
  | 'dataConclusao'
  | 'aprovador'
  | 'userid'
>;
