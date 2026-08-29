import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
  });

  it('deve criar o componente raiz', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('deve exibir o nome do sistema no título principal', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const titulo = (fixture.nativeElement as HTMLElement).querySelector('h1');

    expect(titulo?.textContent).toContain('Biblioteca Pessoal');
  });

  it('deve oferecer um link de atalho para o conteúdo principal', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const atalho = (fixture.nativeElement as HTMLElement).querySelector('.pular-para-conteudo');

    expect(atalho?.getAttribute('href')).toBe('#conteudo-principal');
  });
});
