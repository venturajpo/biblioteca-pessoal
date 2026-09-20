import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Minha biblioteca',
    loadComponent: () => import('./pages/biblioteca/biblioteca').then((m) => m.Biblioteca),
  },
  {
    path: 'adicionar',
    title: 'Adicionar livro',
    loadComponent: () => import('./pages/adicionar/adicionar').then((m) => m.Adicionar),
  },
  {
    path: 'livro/:isbn',
    title: 'Detalhes do livro',
    loadComponent: () => import('./pages/detalhes/detalhes').then((m) => m.Detalhes),
  },
  { path: '**', redirectTo: '' },
];
