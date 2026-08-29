import { Routes } from '@angular/router';

/**
 * Rotas da aplicação. Cada área do sistema entra aqui como carregamento
 * sob demanda (lazy loading), mantendo o pacote inicial pequeno.
 *
 * O `title` de cada rota é anunciado por leitores de tela a cada navegação.
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Início | Biblioteca Pessoal',
    loadComponent: () => import('./features/inicio/inicio').then((m) => m.Inicio),
  },
  // { path: 'livros', ... }        -> minha estante, cadastro e busca no catálogo externo
  // { path: 'estatisticas', ... }  -> análise dos dados de leitura
  { path: '**', redirectTo: '' },
];
