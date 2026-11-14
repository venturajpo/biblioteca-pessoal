import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  WritableSignal,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, map } from 'rxjs';
import { BookApi } from '../../core/book-api';
import { Loan } from '../../core/book';

interface SaveState {
  status: 'idle' | 'saving' | 'saved' | 'error';
  message: string;
}

const IDLE: SaveState = { status: 'idle', message: '' };

// Limite da coluna paginas_lidas (SMALLINT UNSIGNED).
const MAX_PAGES = 65535;

// Limite da coluna RA da tabela LoanHistory.
const MAX_RA = 50;

/** Prazo sugerido para a devolucao, em dias. */
const LOAN_DAYS = 14;

/** Palavra que precisa ser digitada para confirmar a exclusao. */
const DELETE_WORD = 'EXCLUIR';

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

// Data no formato do <input type="date"> e do back-end (AAAA-MM-DD), no fuso local.
// O toISOString() usaria UTC e poderia virar o dia a noite.
function isoDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function daysFromToday(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return isoDate(date);
}

// "2026-10-31" -> "31/10/2026". Sem passar por Date, para nao haver troca de fuso.
function brDate(iso: string): string {
  const [year, month, day] = iso.split('-');
  return `${day}/${month}/${year}`;
}

// Mesma regra do Book.Progress do back-end.
function percentOf(pagesRead: number, pagesTotal: number | null): number {
  return pagesTotal && pagesTotal > 0 ? Math.round((pagesRead / pagesTotal) * 10000) / 100 : 0;
}

@Component({
  selector: 'app-detalhes',
  imports: [RouterLink],
  templateUrl: './detalhes.html',
  styleUrl: './detalhes.scss',
})
export class Detalhes {
  private readonly api = inject(BookApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
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

  protected readonly deleteWord = DELETE_WORD;
  protected readonly deleteConfirm = signal('');
  protected readonly deleteState = signal<SaveState>(IDLE);
  protected readonly canDelete = computed(
    () => this.deleteConfirm().trim() === DELETE_WORD && this.deleteState().status !== 'saving',
  );
  // Emprestimos vem de outro endpoint: se falharem, o resto da tela continua funcionando.
  protected readonly loansResource = rxResource({
    params: () => this.isbn(),
    stream: ({ params }) => this.api.loans(params),
  });
  protected readonly loans = this.loansResource.value;
  protected readonly openLoan = computed(
    () => this.loans()?.find((loan) => loan.return_date === null) ?? null,
  );

  protected readonly today = isoDate(new Date());
  protected readonly maxRa = MAX_RA;
  protected readonly brDate = brDate;
  protected readonly overdue = computed(() => {
    const loan = this.openLoan();
    return loan !== null && loan.due_date < this.today;
  });

  protected readonly raDraft = signal('');
  protected readonly dueDraft = signal(daysFromToday(LOAN_DAYS));
  protected readonly loanState = signal<SaveState>(IDLE);

  private readonly deleteDialog = viewChild<ElementRef<HTMLDialogElement>>('deleteDialog');
  private readonly historyDialog = viewChild<ElementRef<HTMLDialogElement>>('historyDialog');

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

  // ---- Emprestimo ---------------------------------------------------------

  protected onRaInput(value: string): void {
    this.raDraft.set(value);
    this.loanState.set(IDLE);
  }

  protected onDueInput(value: string): void {
    this.dueDraft.set(value);
    this.loanState.set(IDLE);
  }

  protected saveLoan(event: Event): void {
    event.preventDefault();
    const book = this.book();
    if (!book || this.loanState().status === 'saving') return;

    const ra = this.raDraft().trim();
    if (!ra) {
      this.fail(this.loanState, 'Informe o RA de quem está pegando o livro.');
      return;
    }
    if (ra.length > MAX_RA) {
      this.fail(this.loanState, `O RA pode ter no máximo ${MAX_RA} caracteres.`);
      return;
    }

    // O <input type="date"> entrega "" quando a data esta incompleta.
    const due = this.dueDraft();
    if (!/^\d{4}-\d{2}-\d{2}$/.test(due)) {
      this.fail(this.loanState, 'Informe a data prevista para a devolução.');
      return;
    }
    if (due < this.today) {
      this.fail(this.loanState, 'A data de devolução não pode ser antes de hoje.');
      return;
    }

    this.save(
      this.loanState,
      this.api.lend(book.isbn, ra, due),
      'Empréstimo registrado.',
      (loan: Loan) => {
        this.loans.update((list) => [loan, ...(list ?? [])]);
        this.raDraft.set('');
        this.dueDraft.set(daysFromToday(LOAN_DAYS));
      },
    );
  }

  protected returnBook(): void {
    const book = this.book();
    if (!book || !this.openLoan() || this.loanState().status === 'saving') return;

    this.save(
      this.loanState,
      this.api.giveBack(book.isbn),
      'Devolução registrada.',
      (returned: Loan) =>
        this.loans.update((list) =>
          list?.map((loan) => (loan.id === returned.id ? returned : loan)),
        ),
    );
  }

  protected openHistory(): void {
    this.historyDialog()?.nativeElement.showModal();
  }

  protected closeHistory(): void {
    this.historyDialog()?.nativeElement.close();
  }

  // ---- Exclusao -----------------------------------------------------------

  // O <dialog> nativo (showModal) prende o foco, fecha com Esc e devolve o foco ao
  // botao que o abriu.
  protected openDelete(): void {
    this.deleteConfirm.set('');
    this.deleteState.set(IDLE);
    this.deleteDialog()?.nativeElement.showModal();
  }

  protected closeDelete(): void {
    this.deleteDialog()?.nativeElement.close();
  }

  protected onDeleteInput(value: string): void {
    this.deleteConfirm.set(value);
    if (this.deleteState().status === 'error') this.deleteState.set(IDLE);
  }

  protected confirmDelete(event: Event): void {
    event.preventDefault();
    const book = this.book();
    if (!book || !this.canDelete()) return;

    this.deleteState.set({ status: 'saving', message: 'Excluindo…' });

    this.api
      .remove(book.isbn)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeDelete();
          void this.router.navigate(['/']);
        },
        error: (err: unknown) => {
          const status = statusOf(err);
          const message =
            status === 404 || status === 0
              ? this.errorMessage(err)
              : 'Não foi possível excluir o livro. Tente de novo.';
          this.deleteState.set({ status: 'error', message });
        },
      });
  }

  // ---- Apoio --------------------------------------------------------------

  private fail(state: WritableSignal<SaveState>, message: string): void {
    state.set({ status: 'error', message });
  }

  private save<T>(
    state: WritableSignal<SaveState>,
    request: Observable<T>,
    successMessage: string,
    onSuccess: (value: T) => void,
  ): void {
    state.set({ status: 'saving', message: 'Salvando…' });

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (value) => {
        onSuccess(value);
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
      case 409:
        return 'Este livro já está emprestado. Atualize a página.';
      case 0:
        return 'Não foi possível falar com o servidor. Verifique se o back-end está no ar.';
      default:
        return 'Não foi possível salvar. Tente de novo.';
    }
  }
}
