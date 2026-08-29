import { Component, OnInit, inject, signal } from '@angular/core';

import { StatusDaApi, StatusService } from '../../core/api/status.service';

@Component({
  selector: 'app-inicio',
  templateUrl: './inicio.html',
  styleUrl: './inicio.css',
})
export class Inicio implements OnInit {
  private readonly statusService = inject(StatusService);

  protected readonly status = signal<StatusDaApi | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly carregando = signal(true);

  ngOnInit(): void {
    this.statusService.obterStatus().subscribe({
      next: (status) => {
        this.status.set(status);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Não foi possível conectar à API. Verifique se o back-end está no ar.');
        this.carregando.set(false);
      },
    });
  }
}
