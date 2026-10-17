import {
  Component,
  DestroyRef,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { BookSearchResult, FilterBy } from '../../core/book';
import { BookApi } from '../../core/book-api';

interface SearchOption {
  value: FilterBy;
  label: string;
  placeholder: string;
}

const SEARCH_OPTIONS: readonly SearchOption[] = [
  { value: 'isbn', label: 'ISBN', placeholder: 'Ex.: 978-85-359-0277-8' },
  { value: 'title', label: 'Título', placeholder: 'Ex.: Dom Casmurro' },
  { value: 'author', label: 'Autor', placeholder: 'Ex.: Machado de Assis' },
];

/** A busca automatica comeca daqui; com Enter, qualquer tamanho vale. */
const MIN_CHARS = 3;

/** Espera depois da ultima tecla antes de buscar, para nao disparar um request por letra. */
export const SEARCH_DEBOUNCE_MS = 1000;

type Status = 'idle' | 'loading' | 'done' | 'error';

@Component({
  selector: 'app-busca',
  templateUrl: './busca.html',
  styleUrl: './busca.scss',
})
export class Busca {
  private readonly api = inject(BookApi);

  readonly disabled = input(false);
  readonly picked = output<BookSearchResult>();

  protected readonly options = SEARCH_OPTIONS;
  protected readonly searchBy = signal<FilterBy>('title');
  protected readonly term = signal('');
  protected readonly results = signal<BookSearchResult[]>([]);
  protected readonly status = signal<Status>('idle');
  protected readonly open = signal(false);
  protected readonly activeIndex = signal(-1);

  protected readonly placeholder = computed(
    () => SEARCH_OPTIONS.find((o) => o.value === this.searchBy())?.placeholder ?? '',
  );
  protected readonly expanded = computed(() => this.open() && this.results().length > 0);
  protected readonly activeId = computed(() =>
    this.expanded() && this.activeIndex() >= 0 ? `busca-opcao-${this.activeIndex()}` : null,
  );
  protected readonly showPanel = computed(() => this.open() && this.status() !== 'idle');
  protected readonly message = computed(() => {
    switch (this.status()) {
      case 'loading':
        return 'Buscando livros…';
      case 'error':
        return 'Não foi possível consultar a Google Books nem a Open Library agora. Tente de novo em instantes.';
      case 'done': {
        const count = this.results().length;
        if (count === 0) return 'Nenhum livro encontrado.';
        return count === 1 ? '1 livro encontrado.' : `${count} livros encontrados.`;
      }
      default:
        return '';
    }
  });

  private readonly termInput = viewChild.required<ElementRef<HTMLInputElement>>('termInput');

  private timer: ReturnType<typeof setTimeout> | undefined;
  /** Enter pressionado: busca mesmo com menos de MIN_CHARS. */
  private forced = false;
  private inFlight: Subscription | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      clearTimeout(this.timer);
      this.inFlight?.unsubscribe();
    });
  }

  /** Devolve o foco sem reabrir a lista, para ela nao cobrir o alerta de erro da tela. */
  focus(): void {
    this.termInput().nativeElement.focus();
    this.close();
  }

  protected onSearchByChange(value: string): void {
    this.searchBy.set(value as FilterBy);
    if (this.term().trim().length < MIN_CHARS) {
      this.reset();
      return;
    }
    this.schedule(0);
  }

  protected onInput(value: string): void {
    this.term.set(value);
    // O Enter valia para o texto de antes; digitar de novo volta a exigir MIN_CHARS.
    this.forced = false;
    if (value.trim().length < MIN_CHARS) {
      this.reset();
      return;
    }
    this.schedule();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const count = this.results().length;
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp': {
        if (count === 0) return;
        event.preventDefault();
        this.open.set(true);
        const down = event.key === 'ArrowDown';
        this.activeIndex.update((i) => {
          if (i < 0) return down ? 0 : count - 1;
          return (i + (down ? 1 : -1) + count) % count;
        });
        return;
      }
      case 'Enter': {
        event.preventDefault();
        const active = this.expanded() ? this.results()[this.activeIndex()] : undefined;
        if (active) {
          this.pick(active);
        } else if (this.term().trim()) {
          this.forced = true;
          this.schedule(0);
        }
        return;
      }
      case 'Escape':
        if (this.open()) {
          event.preventDefault();
          this.close();
        }
        return;
    }
  }

  protected reopen(): void {
    if (this.status() !== 'idle') this.open.set(true);
  }

  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (!next || !(event.currentTarget as HTMLElement).contains(next)) this.close();
  }

  protected pick(book: BookSearchResult): void {
    this.close();
    this.picked.emit(book);
  }

  private close(): void {
    this.open.set(false);
    this.activeIndex.set(-1);
  }

  private reset(): void {
    clearTimeout(this.timer);
    this.inFlight?.unsubscribe();
    this.results.set([]);
    this.status.set('idle');
    this.close();
  }

  // Debounce: cada tecla reinicia a espera, e o timer le o texto e o tipo na hora em que
  // dispara. Enter e a troca de tipo sao acoes unicas e buscam na hora (delay 0).
  private schedule(delay = SEARCH_DEBOUNCE_MS): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.run(), delay);
  }

  private run(): void {
    const term = this.term().trim();
    if (!term || (!this.forced && term.length < MIN_CHARS)) return;

    this.forced = false;
    this.inFlight?.unsubscribe();
    this.status.set('loading');
    this.open.set(true);

    this.inFlight = this.api.search(this.searchBy(), term).subscribe({
      next: (books) => {
        this.results.set(books);
        this.activeIndex.set(-1);
        this.status.set('done');
      },
      error: () => {
        this.results.set([]);
        this.status.set('error');
      },
    });
  }
}
