import { NgOptimizedImage } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  WritableSignal,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, map } from 'rxjs';
import { BookApi } from '../../core/book-api';

interface SaveState {
  status: 'idle' | 'saving' | 'saved' | 'error';
  message: string;
}

const IDLE: SaveState = { status: 'idle', message: '' };

// Limite da coluna paginas_lidas (SMALLINT UNSIGNED).
const MAX_PAGES = 65535;

// O erro de um resource pode chegar embrulhado (com o original em `cause`),
// entao olhamos nos dois lugares.
function statusOf(err: unknown): number {
  const source =
    err instanceof HttpErrorResponse ? err : (err as { cause?: unknown } | undefined)?.cause;
  return source instanceof HttpErrorResponse ? source.status : -1;
}

// A Google Books devolve a sinopse em HTML. Tiramos as tags e mostramos como texto puro.
function plainText(html: string | null | undefined): string {
  if (!html) return '';
  return html
    .replace(/<br\s*\/?>|<\/p>|<\/li>/gi, '\n')
    .replace(/<[^>]*>/g, '')
    .replace(/&nbsp;/g, ' ')
    .replace(/&quot;/g, '"')
    .replace(/&#39;|&apos;/g, "'")
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&amp;/g, '&')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

// Mesma regra do Book.Progress do back-end.
function percentOf(pagesRead: number, pagesTotal: number | null): number {
  return pagesTotal && pagesTotal > 0 ? Math.round((pagesRead / pagesTotal) * 10000) / 100 : 0;
}

@Component({
  selector: 'app-detalhes',
  imports: [RouterLink, NgOptimizedImage],
  templateUrl: './detalhes.html',
  styleUrl: './detalhes.scss',
})
export class Detalhes {
  private readonly api = inject(BookApi);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pageTitle = inject(Title);

  protected readonly isbn = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('isbn') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('isbn') ?? '' },
  );

  protected readonly resource = rxResource({
    params: () => this.isbn(),
    stream: ({ params }) => this.api.get(params),
  });

  /** O valor do resource e gravavel: e assim que refletimos o que foi salvo. */
  protected readonly book = this.resource.value;

  protected readonly notFound = computed(() => statusOf(this.resource.error()) === 404);
  protected readonly percent = computed(() => Math.round(this.book()?.progress ?? 0));
  protected readonly synopsis = computed(() => plainText(this.book()?.synopsis));

  protected readonly progressSummary = computed(() => {
    const book = this.book();
    if (!book) return '';
    return book.pages_total
      ? `${this.percent()}% concluído · ${book.pages_read} de ${book.pages_total} páginas`
      : `${book.pages_read} páginas lidas (total de páginas desconhecido)`;
  });

  protected readonly coverFailed = linkedSignal({
    source: this.isbn,
    computation: () => false,
  });

  // Rascunhos dos campos. Voltam ao valor salvo quando o valor do livro muda,
  // mas nao sao apagados quando OUTRO campo e salvo (a `source` e por campo).
  protected readonly progressDraft = linkedSignal({
    source: () => this.book()?.pages_read,
    computation: (value) => String(value ?? ''),
  });
  protected readonly ratingDraft = linkedSignal({
    source: () => this.book()?.rating,
    computation: (value) => (value == null ? '' : String(value).replace('.', ',')),
  });
  protected readonly reviewDraft = linkedSignal({
    source: () => this.book()?.review,
    computation: (value) => value ?? '',
  });

  protected readonly progressState = signal<SaveState>(IDLE);
  protected readonly ratingState = signal<SaveState>(IDLE);
  protected readonly reviewState = signal<SaveState>(IDLE);

  constructor() {
    effect(() => {
      const book = this.book();
      if (book) this.pageTitle.setTitle(`${book.title} – Biblioteca Pessoal`);
    });
  }

  // ---- Progresso ----------------------------------------------------------

  protected onProgressInput(value: string): void {
    this.progressDraft.set(value);
    this.progressState.set(IDLE);
  }

  protected saveProgress(event: Event): void {
    event.preventDefault();
    const book = this.book();
    if (!book || this.progressState().status === 'saving') return;

    const text = this.progressDraft().trim();
    if (!/^\d{1,5}$/.test(text)) {
      this.fail(this.progressState, 'Informe um número inteiro de páginas (0 ou mais).');
      return;
    }

    const pages = Number(text);
    if (pages > MAX_PAGES) {
      this.fail(this.progressState, `O valor máximo é ${MAX_PAGES} páginas.`);
      return;
    }
    if (book.pages_total != null && pages > book.pages_total) {
      this.fail(this.progressState, `Este livro tem ${book.pages_total} páginas no total.`);
      return;
    }

    this.save(
      this.progressState,
      this.api.updateProgress(book.isbn, pages),
      'Progresso salvo.',
      () =>
        this.book.update(
          (current) =>
            current && {
              ...current,
              pages_read: pages,
              progress: percentOf(pages, current.pages_total),
            },
        ),
    );
  }

  protected finish(): void {
    const total = this.book()?.pages_total;
    if (!total) return;
    this.progressDraft.set(String(total));
    this.saveProgress(new Event('submit'));
  }

  // ---- Nota ---------------------------------------------------------------

  protected onRatingInput(value: string): void {
    this.ratingDraft.set(value);
    this.ratingState.set(IDLE);
  }

  protected saveRating(event: Event): void {
    event.preventDefault();
    const book = this.book();
    if (!book || this.ratingState().status === 'saving') return;

    // Aceita "8,5" e "8.5". O banco guarda DECIMAL(3,1): no maximo uma casa decimal.
    const text = this.ratingDraft().trim().replace(',', '.');
    if (!/^\d{1,2}(\.\d)?$/.test(text) || Number(text) > 10) {
      this.fail(this.ratingState, 'Informe uma nota de 0 a 10, com no máximo uma casa decimal.');
      return;
    }

    const rating = Number(text);
    this.save(this.ratingState, this.api.updateRating(book.isbn, rating), 'Nota salva.', () =>
      this.book.update((current) => current && { ...current, rating }),
    );
  }

  // ---- Anotacao -----------------------------------------------------------

  protected onReviewInput(value: string): void {
    this.reviewDraft.set(value);
    this.reviewState.set(IDLE);
  }

  protected saveReview(event: Event): void {
    event.preventDefault();
    const book = this.book();
    if (!book || this.reviewState().status === 'saving') return;

    const review = this.reviewDraft().trim() || null;
    this.save(this.reviewState, this.api.updateReview(book.isbn, review), 'Anotação salva.', () =>
      this.book.update((current) => current && { ...current, review }),
    );
  }

  // ---- Apoio --------------------------------------------------------------

  private fail(state: WritableSignal<SaveState>, message: string): void {
    state.set({ status: 'error', message });
  }

  private save(
    state: WritableSignal<SaveState>,
    request: Observable<void>,
    successMessage: string,
    onSuccess: () => void,
  ): void {
    state.set({ status: 'saving', message: 'Salvando…' });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        onSuccess();
        state.set({ status: 'saved', message: successMessage });
      },
      error: (err: unknown) => state.set({ status: 'error', message: this.errorMessage(err) }),
    });
  }

  private errorMessage(err: unknown): string {
    switch (statusOf(err)) {
      case 400:
        return 'O servidor recusou o valor informado. Confira e tente de novo.';
      case 404:
        return 'Este livro não existe mais na biblioteca.';
      case 0:
        return 'Não foi possível falar com o servidor. Verifique se o back-end está no ar.';
      default:
        return 'Não foi possível salvar. Tente de novo.';
    }
  }
}
