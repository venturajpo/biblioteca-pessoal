import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Adicionar } from './adicionar';

describe('Adicionar', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Adicionar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  function type(el: HTMLElement, value: string): void {
    const input = el.querySelector('input') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('should create', () => {
    const fixture = TestBed.createComponent(Adicionar);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should not call the API when the ISBN is invalid', async () => {
    const fixture = TestBed.createComponent(Adicionar);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    type(el, '123');
    el.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(el.querySelector('#isbn-erro')?.textContent).toContain('10 ou 13');
    http.expectNone('/api/Book');
  });

  it('should send the normalized ISBN and show the confirmation', async () => {
    const fixture = TestBed.createComponent(Adicionar);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    type(el, '978-0-13-468599-1');
    el.querySelector('form')?.dispatchEvent(new Event('submit'));

    const req = http.expectOne('/api/Book');
    expect(req.request.body).toEqual({ isbn: '9780134685991' });
    req.flush({
      isbn: '9780134685991',
      title: 'Effective Java',
      author: 'Joshua Bloch',
      image: null,
      progress: 0,
      pages_read: 0,
      pages_total: 416,
      rating: null,
      review: null,
      synopsis: null,
    });
    await fixture.whenStable();

    expect(el.querySelector('h2')?.textContent).toContain('Livro adicionado');
    expect(el.textContent).toContain('Effective Java');
  });

  it('should show the duplicate message on 409', async () => {
    const fixture = TestBed.createComponent(Adicionar);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    type(el, '9780132350884');
    el.querySelector('form')?.dispatchEvent(new Event('submit'));
    http.expectOne('/api/Book').flush('duplicado', { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('já está na sua biblioteca');
  });
});
