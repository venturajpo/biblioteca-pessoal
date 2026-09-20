import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { Book, BookList, BookQuery } from './book';

// Google Books devolve http:// (bloqueado como conteudo misto) e uma borda de "pagina dobrada".
function fixCover(book: Book): Book {
  if (!book.image) return book;
  const image = book.image.replace(/^http:/, 'https:').replace('&edge=curl', '');
  return { ...book, image };
}

@Injectable({ providedIn: 'root' })
export class BookApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  list(query: BookQuery = {}): Observable<BookList> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
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
}
