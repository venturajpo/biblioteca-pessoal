import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { Book, BookList, BookQuery, BookSearchResult, FilterBy } from './book';

// O back-end manda a capa em base64 puro (e nao uma URL) para o sistema funcionar sem
// internet. O <img> precisa de um data URL, e o tipo da imagem sai dos primeiros bytes (magic number).
function coverMime(base64: string): string {
  if (base64.startsWith('iVBOR')) return 'image/png';
  if (base64.startsWith('R0lGOD')) return 'image/gif';
  if (base64.startsWith('UklGR')) return 'image/webp';
  return 'image/jpeg';
}

function fixCover<T extends { image: string | null }>(book: T): T {
  if (!book.image) return book;
  return { ...book, image: `data:${coverMime(book.image)};base64,${book.image}` };
}

@Injectable({ providedIn: 'root' })
export class BookApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  list(query: BookQuery = {}): Observable<BookList> {
    // O back-end recusa filter sem filter_by e vice-versa: ou vao os dois, ou nenhum.
    const { filter, filter_by, ...rest } = query;
    const sent = filter && filter_by ? { ...rest, filter, filter_by } : rest;
    let params = new HttpParams();
    for (const [key, value] of Object.entries(sent)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http
      .get<BookList>(`${this.base}/Book`, { params })
      .pipe(map((result) => ({ ...result, books: result.books.map(fixCover) })));
  }

  get(isbn: string): Observable<Book> {
    return this.http
      .get<Book>(`${this.base}/Book/${encodeURIComponent(isbn)}`)
      .pipe(map(fixCover));
  }

  /** Busca nos catálogos externos (via back-end) os livros que ainda podem ser adicionados. */
  search(filterBy: FilterBy, filter: string): Observable<BookSearchResult[]> {
    const params = new HttpParams().set('filter_by', filterBy).set('filter', filter);
    return this.http
      .get<{ books: BookSearchResult[] }>(`${this.base}/Book/search`, { params })
      .pipe(map((result) => result.books.map(fixCover)));
  }

  add(isbn: string): Observable<Book> {
    return this.http.post<Book>(`${this.base}/Book`, { isbn }).pipe(map(fixCover));
  }

  updateProgress(isbn: string, pagesRead: number): Observable<void> {
    return this.http.post<void>(`${this.base}/UpdateProgress/${encodeURIComponent(isbn)}`, {
      pages_read: pagesRead,
    });
  }

  updateRating(isbn: string, rating: number): Observable<void> {
    return this.http.post<void>(`${this.base}/UpdateRating/${encodeURIComponent(isbn)}`, {
      rating,
    });
  }

  updateReview(isbn: string, review: string | null): Observable<void> {
    return this.http.post<void>(`${this.base}/UpdateReview/${encodeURIComponent(isbn)}`, {
      review,
    });
  }

  remove(isbn: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/Book/${encodeURIComponent(isbn)}`);
  }
}
