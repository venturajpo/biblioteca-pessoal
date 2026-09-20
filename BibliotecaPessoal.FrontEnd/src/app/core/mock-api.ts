import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { map, timer } from 'rxjs';
import { Book } from './book';

interface MockBook extends Book {
  registered_at: string;
}

const SORT_KEYS = [
  'title',
  'isbn',
  'author',
  'rating',
  'pages_read',
  'pages_total',
  'registered_at',
] as const;
type SortKey = (typeof SORT_KEYS)[number];

const FILTER_KEYS = ['title', 'author', 'isbn'] as const;
type FilterKey = (typeof FILTER_KEYS)[number];

const cover = (isbn: string): string =>
  `https://covers.openlibrary.org/b/isbn/${isbn}-L.jpg?default=false`;
// Estado em memoria: some ao recarregar a pagina.
const store: MockBook[] = [
  {
    isbn: '9788535902778',
    title: 'Dom Casmurro',
    author: 'Machado de Assis',
    image: cover('9788535902778'),
    progress: 0,
    pages_read: 120,
    pages_total: 256,
    rating: 9.5,
    review: 'Reler o capitulo dos olhos de ressaca.',
    synopsis:
      'Bentinho relembra a juventude e o casamento com Capitu, remoendo a suspeita de uma traicao que nunca se confirma.',
    registered_at: '2026-09-01',
  },
  {
    isbn: '9788533613379',
    title: 'O Senhor dos Aneis: A Sociedade do Anel',
    author: 'J. R. R. Tolkien',
    image: null,
    progress: 0,
    pages_read: 40,
    pages_total: 576,
    rating: null,
    review: null,
    synopsis:
      'Frodo herda um anel magico e parte de sua terra natal para destrui-lo antes que caia nas maos do inimigo.',
    registered_at: '2026-09-10',
  },
  {
    isbn: '9788575225103',
    title: 'Estruturas de Dados e Algoritmos',
    author: null,
    image: null,
    progress: 0,
    pages_read: 0,
    pages_total: null,
    rating: null,
    review: null,
    synopsis: null,
    registered_at: '2026-09-17',
  },
  {
    isbn: '9780132350884',
    title: 'Clean Code',
    author: 'Robert C. Martin',
    image: null,
    progress: 0,
    pages_read: 464,
    pages_total: 464,
    rating: 8,
    review: 'Util, mas repetitivo em alguns capitulos.',
    synopsis: 'Um manual de boas praticas para escrever codigo legivel e sustentavel.',
    registered_at: '2026-08-20',
  },
  {
    isbn: '9788525406958',
    title: 'Memorias Postumas de Bras Cubas',
    author: 'Machado de Assis',
    image: null,
    progress: 0,
    pages_read: 30,
    pages_total: 208,
    rating: 7.5,
    review: null,
    synopsis: 'Um defunto autor narra a propria vida com ironia e pessimismo.',
    registered_at: '2026-08-05',
  },
];

const normalizeIsbn = (isbn: string): string =>
  decodeURIComponent(isbn).replaceAll('-', '').replaceAll(' ', '');

const progressOf = (b: MockBook): number =>
  b.pages_total && b.pages_total > 0
    ? Math.round((b.pages_read / b.pages_total) * 10000) / 100
    : 0;

const fail = (req: HttpRequest<unknown>, status: number, error?: string): never => {
  throw new HttpErrorResponse({ status, error, url: req.url });
};

const ok = (body?: unknown): HttpResponse<unknown> =>
  new HttpResponse({ status: 200, body: body ?? null });

// Mesmo comportamento do MySQL: NULL vem antes de qualquer valor em ordem crescente.
function compare(a: unknown, b: unknown): number {
  if (a == null && b == null) return 0;
  if (a == null) return -1;
  if (b == null) return 1;
  if (typeof a === 'string' && typeof b === 'string') {
    return a.localeCompare(b, 'pt-BR', { sensitivity: 'base' });
  }
  return (a as number) - (b as number);
}

function toDto(b: MockBook, includeText: boolean): Book {
  const { registered_at: _registeredAt, ...rest } = b;
  return {
    ...rest,
    progress: progressOf(b),
    synopsis: includeText ? b.synopsis : null,
    review: includeText ? b.review : null,
  };
}

function handle(req: HttpRequest<unknown>, path: string): HttpResponse<unknown> {
  // GET /Book
  if (req.method === 'GET' && path === '/Book') {
    const sortParam = req.params.get('sort_by') as SortKey | null;
    const sortKey: SortKey = sortParam && SORT_KEYS.includes(sortParam) ? sortParam : 'title';
    const desc = req.params.get('sort_direction')?.toLowerCase() === 'desc';

    const filterText = req.params.get('filter')?.trim().toLowerCase() ?? '';
    const filterParam = req.params.get('filter_by') as FilterKey | null;
    const filterKey: FilterKey =
      filterParam && FILTER_KEYS.includes(filterParam) ? filterParam : 'title';

    const page = Math.max(Number(req.params.get('page') ?? 1) || 1, 1);
    const pageSize = Math.min(Math.max(Number(req.params.get('page_size') ?? 20) || 20, 1), 100);

    const filtered = store.filter(
      (b) => !filterText || (b[filterKey] ?? '').toString().toLowerCase().includes(filterText),
    );
    const sorted = [...filtered].sort(
      (a, b) => compare(a[sortKey], b[sortKey]) * (desc ? -1 : 1),
    );
    const items = sorted.slice((page - 1) * pageSize, page * pageSize);

    return ok({
      books: items.map((b) => toDto(b, false)),
      count: filtered.length,
      page_count: Math.ceil(filtered.length / pageSize),
    });
  }

  // GET /Book/{isbn}
  const one = path.match(/^\/Book\/([^/]+)$/);
  if (req.method === 'GET' && one) {
    const book = store.find((b) => b.isbn === normalizeIsbn(one[1]));
    return book ? ok(toDto(book, true)) : fail(req, 404);
  }

  // POST /Book
  if (req.method === 'POST' && path === '/Book') {
    const raw = (req.body as { isbn?: string } | null)?.isbn ?? '';
    if (!raw.trim()) return fail(req, 400, 'ISBN cannot be empty.');

    const isbn = normalizeIsbn(raw);
    if (isbn === '9999999999999') return fail(req, 422, `No book found with ISBN: ${isbn}`);
    if (store.some((b) => b.isbn === isbn)) {
      return fail(req, 409, `Book with ISBN ${isbn} is already registered.`);
    }

    const created: MockBook = {
      isbn,
      title: `Livro ${isbn}`,
      author: 'Autor de Teste',
      image: null,
      progress: 0,
      pages_read: 0,
      pages_total: 300,
      rating: null,
      review: null,
      synopsis: 'Livro criado pelo mock da API.',
      registered_at: new Date().toISOString().slice(0, 10),
    };
    store.push(created);
    return ok(toDto(created, true));
  if (isbn === '9999999999999') return fail(req, 422, `No book found with ISBN: ${isbn}`);
  if (isbn === '9990000000000') return fail(req, 502, 'Simulated Google Books failure.');
  }

  // POST /UpdateProgress|UpdateRating|UpdateReview/{isbn}
  const update = path.match(/^\/Update(Progress|Rating|Review)\/([^/]+)$/);
  if (req.method === 'POST' && update) {
    const book = store.find((b) => b.isbn === normalizeIsbn(update[2]));
    if (!book) return fail(req, 404);

    if (update[1] === 'Progress') {
      const pages = (req.body as { pages_read: number }).pages_read;
      if (pages < 0) return fail(req, 400, 'pages_read cannot be negative.');
      if (book.pages_total != null && pages > book.pages_total) {
        return fail(req, 400, `pages_read cannot exceed pages_total (${book.pages_total}).`);
      }
      book.pages_read = pages;
    } else if (update[1] === 'Rating') {
      const rating = (req.body as { rating: number }).rating;
      if (rating < 0 || rating > 10) return fail(req, 400, 'rating must be between 0 and 10.');
      book.rating = rating;
    } else {
      book.review = (req.body as { review: string | null }).review;
    }
    return ok();
  }

  return fail(req, 404);
}

export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }
  const path = req.url.slice('/api'.length).split('?')[0];
  // Latencia artificial para exercitar os estados de "carregando".
  return timer(300).pipe(map(() => handle(req, path)));
};
