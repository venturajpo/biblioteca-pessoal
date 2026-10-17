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
import { Book, BookSearchResult } from '../../core/book';
import { BookApi } from '../../core/book-api';
import { Busca } from './busca';

interface Problem {
  message: string;
  /** Preenchido no 409, para oferecer o link do livro que ja existe. */
  existingIsbn?: string;
}

@Component({
  selector: 'app-adicionar',
  imports: [RouterLink, Busca],
  templateUrl: './adicionar.html',
  styleUrl: './adicionar.scss',
})
export class Adicionar {
  private readonly api = inject(BookApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Livro escolhido na busca cujo cadastro esta em andamento. */
  protected readonly adding = signal<BookSearchResult | null>(null);
  /** Erro devolvido pelo servidor (mostrado como alerta). */
  protected readonly problem = signal<Problem | null>(null);
  protected readonly added = signal<Book | null>(null);
  protected readonly coverFailed = signal(false);

  private readonly search = viewChild(Busca);
  private readonly successTitle = viewChild<ElementRef<HTMLElement>>('successTitle');

  protected onPick(result: BookSearchResult): void {
    if (this.adding()) return;

    this.problem.set(null);
    this.adding.set(result);

    this.api
      .add(result.isbn)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (book) => {
          this.adding.set(null);
          this.coverFailed.set(false);
          // O POST nao devolve a capa; a miniatura da busca serve ate abrir os detalhes.
          this.added.set({ ...book, image: book.image ?? result.image });
          this.focusAfterRender(() => this.successTitle()?.nativeElement);
        },
        error: (err: unknown) => {
          this.adding.set(null);
          this.problem.set(this.toProblem(err, result.isbn));
          // A busca ficou desabilitada durante o cadastro e perdeu o foco.
          this.focusAfterRender(() => this.search());
        },
      });
  }

  protected addAnother(): void {
    this.added.set(null);
    this.problem.set(null);
    this.focusAfterRender(() => this.search());
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
            'Não encontramos nenhum livro com esse ISBN na Google Books nem na Open Library. Confira os números e tente de novo.',
        };
      case 502:
        return {
          message:
            'Não foi possível consultar a Google Books nem a Open Library agora. Tente de novo em instantes.',
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
  private focusAfterRender(target: () => { focus(): void } | undefined): void {
    afterNextRender(() => target()?.focus(), { injector: this.injector });
  }
}
