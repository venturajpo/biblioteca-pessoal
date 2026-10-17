import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Adicionar } from './adicionar';
import { SEARCH_DEBOUNCE_MS } from './busca';

const EFFECTIVE_JAVA = {
  isbn: '9780134685991',
  title: 'Effective Java',
  author: 'Joshua Bloch',
  image: null,
};

describe('Adicionar', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<Adicionar>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Adicionar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Adicionar);
    el = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
    // So depois de montar: o debounce da busca usa setTimeout.
    vi.useFakeTimers();
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  function input(): HTMLInputElement {
    return el.querySelector('#busca-termo') as HTMLInputElement;
  }

  function type(value: string): void {
    input().value = value;
    input().dispatchEvent(new Event('input'));
  }

  function press(key: string): void {
    input().dispatchEvent(new KeyboardEvent('keydown', { key }));
    fixture.detectChanges();
  }

  function expectSearch(filter: string, filterBy = 'title'): TestRequest {
    return http.expectOne(
      (r) =>
        r.url === '/api/Book/search' &&
        r.params.get('filter') === filter &&
        r.params.get('filter_by') === filterBy,
    );
  }

  function searchAndShow(term: string): void {
    type(term);
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);
    expectSearch(term).flush({ books: [EFFECTIVE_JAVA] });
    fixture.detectChanges();
  }

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should only search on its own from 3 characters', () => {
    type('ef');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);
    http.expectNone((r) => r.url === '/api/Book/search');

    searchAndShow('eff');

    const option = el.querySelector('[role="option"]') as HTMLElement;
    expect(option.querySelector('.author')?.textContent).toContain('Joshua Bloch');
    expect(option.querySelector('.title')?.textContent).toContain('Effective Java');
    // Autor em cima, titulo embaixo.
    expect(option.textContent!.indexOf('Joshua')).toBeLessThan(option.textContent!.indexOf('Effective'));
  });

  it('should search with any length on Enter', () => {
    type('ef');
    press('Enter');
    vi.advanceTimersByTime(0);
    expectSearch('ef').flush({ books: [] });
    fixture.detectChanges();

    expect(el.querySelector('.panel-message')?.textContent).toContain('Nenhum livro encontrado');
  });

  it('should wait 1 second after the last key and send only the latest text', () => {
    type('dom');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 1);
    // Cada tecla reinicia a espera: "dom" nunca chega a ser buscado.
    type('dom c');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 1);
    http.expectNone((r) => r.url === '/api/Book/search');

    vi.advanceTimersByTime(1);
    expectSearch('dom c').flush({ books: [] });
  });

  it('should search by the field chosen in the dropdown', () => {
    const select = el.querySelector('#busca-tipo') as HTMLSelectElement;
    select.value = 'author';
    select.dispatchEvent(new Event('change'));

    type('machado');
    vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);
    expectSearch('machado', 'author').flush({ books: [] });
  });

  it('should add the clicked book and show the confirmation', () => {
    searchAndShow('effective');

    (el.querySelector('[role="option"]') as HTMLElement).click();
    fixture.detectChanges();

    const req = http.expectOne('/api/Book');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ isbn: '9780134685991' });
    req.flush({
      ...EFFECTIVE_JAVA,
      progress: 0,
      pages_read: 0,
      pages_total: 416,
      rating: null,
      review: null,
      synopsis: null,
    });
    fixture.detectChanges();

    expect(el.querySelector('h2')?.textContent).toContain('Livro adicionado');
    expect(el.textContent).toContain('Effective Java');
  });

  it('should pick with the keyboard and show the duplicate message on 409', () => {
    searchAndShow('effective');

    press('ArrowDown');
    expect(input().getAttribute('aria-activedescendant')).toBe('busca-opcao-0');

    press('Enter');
    http.expectOne('/api/Book').flush('duplicado', { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('já está na sua biblioteca');
  });
});
