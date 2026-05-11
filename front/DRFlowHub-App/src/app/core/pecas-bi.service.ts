import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface PecaVendaMensal {
  mes: string;
  faturamento: number;
  margem: number;
  quantidade: number;
}

export interface PecaCategoria {
  nome: string;
  faturamento: number;
  margemPercentual: number;
}

export interface PecaRanking {
  nome: string;
  codigo: string;
  categoria: string;
  quantidade: number;
  faturamento: number;
  margemPercentual: number;
  giroDias: number;
}

export interface PecaVendedor {
  nome: string;
  faturamento: number;
  pedidos: number;
  conversaoPercentual: number;
}

export interface PecaCanal {
  nome: string;
  faturamento: number;
}

export interface PecasBiData {
  atualizadoEm: string;
  vendasMensais: PecaVendaMensal[];
  categorias: PecaCategoria[];
  pecas: PecaRanking[];
  vendedores: PecaVendedor[];
  canais: PecaCanal[];
}

@Injectable({ providedIn: 'root' })
export class PecasBiService {
  load(): Observable<PecasBiData> {
    return of({
      atualizadoEm: new Date().toISOString(),
      vendasMensais: [
        { mes: 'Jan', faturamento: 182400, margem: 51200, quantidade: 840 },
        { mes: 'Fev', faturamento: 196800, margem: 56600, quantidade: 910 },
        { mes: 'Mar', faturamento: 214300, margem: 63100, quantidade: 984 },
        { mes: 'Abr', faturamento: 238900, margem: 68400, quantidade: 1048 },
        { mes: 'Mai', faturamento: 251600, margem: 74200, quantidade: 1126 },
        { mes: 'Jun', faturamento: 246100, margem: 71800, quantidade: 1088 },
      ],
      categorias: [
        { nome: 'Filtros e lubrificacao', faturamento: 218500, margemPercentual: 31.4 },
        { nome: 'Freios', faturamento: 184900, margemPercentual: 28.2 },
        { nome: 'Suspensao', faturamento: 172300, margemPercentual: 25.7 },
        { nome: 'Motor', faturamento: 141200, margemPercentual: 22.9 },
        { nome: 'Eletrica', faturamento: 118600, margemPercentual: 29.1 },
      ],
      pecas: [
        { nome: 'Filtro de oleo premium', codigo: 'FO-1842', categoria: 'Filtros', quantidade: 328, faturamento: 52480, margemPercentual: 34.6, giroDias: 12 },
        { nome: 'Pastilha de freio dianteira', codigo: 'PF-2209', categoria: 'Freios', quantidade: 214, faturamento: 49220, margemPercentual: 27.8, giroDias: 18 },
        { nome: 'Amortecedor traseiro', codigo: 'AM-7741', categoria: 'Suspensao', quantidade: 96, faturamento: 44160, margemPercentual: 24.2, giroDias: 31 },
        { nome: 'Bateria 60Ah selada', codigo: 'BT-6012', categoria: 'Eletrica', quantidade: 72, faturamento: 39600, margemPercentual: 21.4, giroDias: 24 },
        { nome: 'Kit correia dentada', codigo: 'KC-1190', categoria: 'Motor', quantidade: 82, faturamento: 37310, margemPercentual: 26.9, giroDias: 27 },
        { nome: 'Oleo sintetico 5W30', codigo: 'OL-530S', categoria: 'Lubrificacao', quantidade: 410, faturamento: 34850, margemPercentual: 32.1, giroDias: 9 },
      ],
      vendedores: [
        { nome: 'Ana Paula', faturamento: 286400, pedidos: 312, conversaoPercentual: 41.6 },
        { nome: 'Marcos Lima', faturamento: 247900, pedidos: 286, conversaoPercentual: 38.4 },
        { nome: 'Renata Alves', faturamento: 221300, pedidos: 251, conversaoPercentual: 35.8 },
        { nome: 'Carlos Souza', faturamento: 198700, pedidos: 226, conversaoPercentual: 32.7 },
      ],
      canais: [
        { nome: 'Balcao', faturamento: 428600 },
        { nome: 'Oficina', faturamento: 312400 },
        { nome: 'Televendas', faturamento: 219800 },
        { nome: 'E-commerce', faturamento: 154300 },
      ],
    });
  }
}
