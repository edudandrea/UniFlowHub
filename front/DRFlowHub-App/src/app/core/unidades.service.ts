import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Unidade, UnidadePayload } from './models';

const API_URL = '/api';

@Injectable({ providedIn: 'root' })
export class UnidadesService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<Unidade[]> {
    return this.http.get<Unidade[]>(`${API_URL}/unidades`);
  }

  create(payload: UnidadePayload): Observable<Unidade> {
    return this.http.post<Unidade>(`${API_URL}/unidades`, payload);
  }

  update(id: number, payload: UnidadePayload): Observable<Unidade> {
    return this.http.put<Unidade>(`${API_URL}/unidades/${id}`, payload);
  }
}
