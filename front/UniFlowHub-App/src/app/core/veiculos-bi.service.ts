import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

const API_URL = '/api';

export interface VeiculoAcessorioRanking {
  codigo: string;
  nome: string;
  categoria: string;
  quantidade: number;
  faturamento: number;
  margemPercentual: number;
  rentabilidade: number;
}

@Injectable({ providedIn: 'root' })
export class VeiculosBiService {
  constructor(private readonly http: HttpClient) {}

  loadAcessorios(filter: { dataInicio?: string; dataFim?: string; empresa?: number | null; revenda?: number[] | null } = {}): Observable<VeiculoAcessorioRanking[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value === undefined || value === null || value === '' || (Array.isArray(value) && !value.length)) {
        continue;
      }

      params = params.set(key, Array.isArray(value) ? value.join(',') : String(value));
    }

    return this.http.get<VeiculoAcessorioRanking[]>(`${API_URL}/veiculos-bi/acessorios`, { params });
  }
}
