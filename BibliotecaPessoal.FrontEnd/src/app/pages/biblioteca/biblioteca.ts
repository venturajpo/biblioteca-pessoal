import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Book, BookQuery, FilterBy, SortBy } from '../../core/book';
import { BookApi } from '../../core/book-api';

interface SortOption {
  value: string;
  label: string;
  sort_by: SortBy;
  sort_direction: 'asc' | 'desc';
}

const SORT_OPTIONS: readonly SortOption[] = [
  { value: 'title:asc', label: 'Título (A–Z)', sort_by: 'title', sort_direction: 'asc' },
  { value: 'title:desc', label: 'Título (Z–A)', sort_by: 'title', sort_direction: 'desc' },
  { value: 'author:asc', label: 'Autor (A–Z)', sort_by: 'author', sort_direction: 'asc' },
  { value: 'author:desc', label: 'Autor (Z–A)', sort_by: 'author', sort_direction: 'desc' },
  {
    value: 'registered_at:desc',
    label: 'Adicionados recentemente',
    sort_by: 'registered_at',
    sort_direction: 'desc',
  },
  { value: 'rating:desc', label: 'Maior nota', sort_by: 'rating', sort_direction: 'desc' },
  {
    value: 'pages_read:desc',
    label: 'Mais páginas lidas',
    sort_by: 'pages_read',
    sort_direction: 'desc',
  },
];

/** A lista do back-end filtra por titulo e por autor (busca parcial nos dois). */
type LibraryFilter = Extract<FilterBy, 'title' | 'author'>;

interface FilterOption {
  value: LibraryFilter;
  label: string;
  placeholder: string;
}

const FILTER_OPTIONS: readonly FilterOption[] = [
  { value: 'title', label: 'Título', placeholder: 'Título do livro' },
  { value: 'author', label: 'Autor', placeholder: 'Nome do autor' },
];

const PAGE_SIZE = 12;

@Component({
  selector: 'app-biblioteca',
  imports: [RouterLink],
  templateUrl: './biblioteca.html',
  styleUrl: './biblioteca.scss',
})
export class Biblioteca {
  private readonly api = inject(BookApi);
  private debounceId: ReturnType<typeof setTimeout> | undefined;

  protected readonly sortOptions = SORT_OPTIONS;
  protected readonly filterOptions = FILTER_OPTIONS;

  /** Texto do campo de busca, atualizado a cada tecla. */
  protected readonly search = signal('');
  /** Texto efetivamente enviado a API (depois do debounce). */
  protected readonly filter = signal('');
  /** Campo em que o texto e procurado. */
  protected readonly filterBy = signal<LibraryFilter>('title');
  protected readonly sort = signal(SORT_OPTIONS[0].value);
  protected readonly page = signal(1);
  protected readonly failedCovers = signal<ReadonlySet<string>>(new Set());

  protected readonly filterPlaceholder = computed(
    () => FILTER_OPTIONS.find((o) => o.value === this.filterBy())?.placeholder ?? '',
  );

  protected readonly books = rxResource({
    params: () => {
      const filter = this.filter();
      // Sem texto, o campo nao importa: trocar o seletor nao recarrega a lista.
      const filterBy = filter ? this.filterBy() : 'title';
      return { page: this.page(), filter, filterBy, sort: this.sort() };
    },
    stream: ({ params }) => {
      const option = SORT_OPTIONS.find((o) => o.value === params.sort) ?? SORT_OPTIONS[0];
      const query: BookQuery = {
        page: params.page,
        page_size: PAGE_SIZE,
        sort_by: option.sort_by,
        sort_direction: option.sort_direction,
        filter_by: params.filterBy,
        filter: params.filter,
      };
      return this.api.list(query);
    },
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.debounceId));
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.debounceId);
    this.debounceId = setTimeout(() => {
      this.filter.set(value.trim());
      this.page.set(1);
    }, 300);
  }

  protected onFilterBy(value: string): void {
    this.filterBy.set(value as LibraryFilter);
    if (this.filter()) this.page.set(1);
  }

  protected onSort(value: string): void {
    this.sort.set(value);
    this.page.set(1);
  }

  protected goTo(page: number): void {
    this.page.set(page);
  }

  protected onCoverError(isbn: string): void {
    this.failedCovers.update((set) => new Set(set).add(isbn));
  }

  protected progressLabel(book: Book): string {
    return book.pages_total
      ? `${book.pages_read} / ${book.pages_total} páginas`
      : `${book.pages_read} páginas lidas`;
  }
}
