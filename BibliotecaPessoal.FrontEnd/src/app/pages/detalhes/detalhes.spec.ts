import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Book } from '../../core/book';
import { Detalhes } from './detalhes';

const ISBN = '9788535902778';

const BOOK: Book = {
  isbn: ISBN,
  title: 'Dom Casmurro',
  author: 'Machado de Assis',
  image: null,
  progress: 46.88,
  pages_read: 120,
  pages_total: 256,
  rating: 9.5,
  review: 'Reler o capitulo dos olhos de ressaca.',
  synopsis: '<p>Bentinho relembra a juventude.</p>',
};

describe('Detalhes', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    const params = convertToParamMap({ isbn: ISBN });
    await TestBed.configureTestingModule({
      imports: [Detalhes],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: params }, paramMap: of(params) } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  function setValue(el: HTMLElement, selector: string, value: string): void {
    const field = el.querySelector(selector) as HTMLInputElement;
    field.value = value;
    field.dispatchEvent(new Event('input'));
  }

  async function load(): Promise<{ el: HTMLElement; done: () => Promise<void> }> {
    const fixture = TestBed.createComponent(Detalhes);
    fixture.detectChanges();
    http.expectOne(`/api/Book/${ISBN}`).flush(BOOK);
    await fixture.whenStable();
    return { el: fixture.nativeElement as HTMLElement, done: () => fixture.whenStable() };
  }

  it('should show the book and strip HTML from the synopsis', async () => {
    const { el } = await load();

    expect(el.querySelector('h1')?.textContent).toContain('Dom Casmurro');
    expect(el.querySelector('.synopsis')?.textContent).toBe('Bentinho relembra a juventude.');
  });

  it('should save the progress and show the confirmation', async () => {
    const { el, done } = await load();

    setValue(el, '#pages-read', '200');
    el.querySelector('#form-progresso')?.dispatchEvent(new Event('submit'));

    const req = http.expectOne(`/api/UpdateProgress/${ISBN}`);
    expect(req.request.body).toEqual({ pages_read: 200 });
    req.flush(null);
    await done();

    expect(el.querySelector('#progresso-status')?.textContent).toContain('Progresso salvo');
    expect(el.querySelector('.summary')?.textContent).toContain('200 de 256');
  });

  it('should not call the API when pages exceed the total', async () => {
    const { el, done } = await load();

    setValue(el, '#pages-read', '999');
    el.querySelector('#form-progresso')?.dispatchEvent(new Event('submit'));
    await done();

    http.expectNone(`/api/UpdateProgress/${ISBN}`);
    expect(el.querySelector('#progresso-status')?.textContent).toContain('256 páginas');
  });

  it('should accept a comma as the decimal separator in the rating', async () => {
    const { el } = await load();

    setValue(el, '#rating', '8,5');
    el.querySelector('#form-nota')?.dispatchEvent(new Event('submit'));

    const req = http.expectOne(`/api/UpdateRating/${ISBN}`);
    expect(req.request.body).toEqual({ rating: 8.5 });
    req.flush(null);
  });
});
