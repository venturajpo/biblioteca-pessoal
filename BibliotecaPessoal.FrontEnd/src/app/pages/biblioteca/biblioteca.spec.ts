import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Biblioteca } from './biblioteca';

const EMPTY = { count: 0, page_count: 0, books: [] };

describe('Biblioteca', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Biblioteca],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Biblioteca);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should filter by author when "Autor" is selected', async () => {
    const fixture = TestBed.createComponent(Biblioteca);
    const http = TestBed.inject(HttpTestingController);
    const el = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
    http.expectOne((r) => r.url === '/api/Book').flush(EMPTY);
    await fixture.whenStable();

    // Sem texto, trocar o seletor nao recarrega a lista.
    const select = el.querySelector('#busca-tipo') as HTMLSelectElement;
    select.value = 'author';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    http.expectNone((r) => r.url === '/api/Book');

    const input = el.querySelector('#busca') as HTMLInputElement;
    input.value = 'machado';
    input.dispatchEvent(new Event('input'));
    await new Promise((resolve) => setTimeout(resolve, 350)); // debounce de 300 ms
    fixture.detectChanges();

    const req = http.expectOne((r) => r.url === '/api/Book');
    expect(req.request.params.get('filter_by')).toBe('author');
    expect(req.request.params.get('filter')).toBe('machado');
    req.flush(EMPTY);
    await fixture.whenStable();

    expect(el.querySelector('.empty')?.textContent).toContain('autor “machado”');
  });
});
