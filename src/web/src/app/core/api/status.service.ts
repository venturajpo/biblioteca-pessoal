import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

/** Resposta do endpoint GET /api/status da API. */
export interface StatusDaApi {
  aplicacao: string;
  versao: string;
  ambiente: string;
  consultadoEm: string;
}

/**
 * Acesso ao endpoint de diagnóstico da API. Serve de modelo para os demais
 * serviços de dados: o componente nunca chama o HttpClient diretamente.
 */
@Injectable({ providedIn: 'root' })
export class StatusService {
  private readonly http = inject(HttpClient);

  obterStatus(): Observable<StatusDaApi> {
    return this.http.get<StatusDaApi>(`${environment.urlDaApi}/status`);
  }
}
