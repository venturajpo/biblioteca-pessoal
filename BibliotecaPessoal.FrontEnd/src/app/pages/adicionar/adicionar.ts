import { NgOptimizedImage } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Book } from '../../core/book';
import { BookApi } from '../../core/book-api';

interface Problem {
  message: string;
  /** Preenchido no 409, para oferecer o link do livro que ja existe. */
  existingIsbn?: string;
}

// 13 digitos, ou 9 digitos + (digito ou X) no ISBN-10.
const ISBN_PATTERN = /^(\d{13}|\d{9}[\dX])$/;

const normalizeIsbn = (value: string): string => value.replace(/[-\s]/g, '').toUpperCase();

@Component({
  selector: 'app-adicionar',
  imports: [RouterLink, NgOptimizedImage],
  templateUrl: './adicionar.html',
  styleUrl: './adicionar.scss',
})
export class Adicionar {
  private readonly api = inject(BookApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  protected readonly isbn = signal('');
  protected readonly submitting = signal(false);
  /** Erro de validacao do campo (mostrado abaixo do input). */
  protected readonly validationError = signal<string | null>(null);
  /** Erro devolvido pelo servidor (mostrado como alerta). */
  protected readonly problem = signal<Problem | null>(null);
  protected readonly added = signal<Book | null>(null);
  protected readonly coverFailed = signal(false);

  private readonly isbnInput = viewChild<ElementRef<HTMLInputElement>>('isbnInput');
  private readonly successTitle = viewChild<ElementRef<HTMLElement>>('successTitle');

  protected onInput(value: string): void {
    this.isbn.set(value);
    this.validationError.set(null);
    this.problem.set(null);
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) return;

    const isbn = normalizeIsbn(this.isbn());
    const invalid = this.validate(isbn);
    if (invalid) {
      this.validationError.set(invalid);
      this.isbnInput()?.nativeElement.focus();
      return;
    }

    this.problem.set(null);
    this.submitting.set(true);

    this.api
      .add(isbn)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (book) => {
          this.submitting.set(false);
          this.coverFailed.set(false);
          this.added.set(book);
          this.focusAfterRender(() => this.successTitle());
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.problem.set(this.toProblem(err, isbn));
        },
      });
  }

  protected addAnother(): void {
    this.added.set(null);
    this.isbn.set('');
    this.problem.set(null);
    this.validationError.set(null);
    this.focusAfterRender(() => this.isbnInput());
  }

  private validate(isbn: string): string | null {
    if (!isbn) return 'Informe o ISBN do livro.';
    if (!ISBN_PATTERN.test(isbn)) {
      return 'O ISBN deve ter 10 ou 13 dígitos (no ISBN-10, o último pode ser X).';
    }
    return null;
  }

  private toProblem(err: unknown, isbn: string): Problem {
    const status = err instanceof HttpErrorResponse ? err.status : -1;
    switch (status) {
      case 400:
        return { message: 'Informe um ISBN válido.' };
      case 409:
        return { message: 'Este livro já está na sua biblioteca.', existingIsbn: isbn };
      case 422:
        return {
          message:
            'Não encontramos nenhum livro com esse ISBN na Google Books. Confira os números e tente de novo.',
        };
      case 502:
        return {
          message: 'Não foi possível consultar a Google Books agora. Tente de novo em instantes.',
        };
      case 0:
        return {
          message: 'Não foi possível falar com o servidor. Verifique se o back-end está no ar.',
        };
      default:
        return { message: 'Algo deu errado ao adicionar o livro. Tente de novo.' };
    }
  }

  // O elemento so existe depois que o Angular renderiza o @if, entao o foco
  // precisa esperar a proxima renderizacao.
  private focusAfterRender(target: () => ElementRef<HTMLElement> | undefined): void {
    afterNextRender(() => target()?.nativeElement.focus(), { injector: this.injector });
  }
}
