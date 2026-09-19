export interface Book {
  isbn: string;
  title: string;
  author: string | null;
  image: string | null;
  progress: number;
  pages_read: number;
  pages_total: number | null;
  rating: number | null;
  review: string | null;
  synopsis: string | null;
}

export interface BookList {
  count: number;
  page_count: number;
  books: Book[];
}

export type SortBy =
  | 'title'
  | 'isbn'
  | 'author'
  | 'rating'
  | 'pages_read'
  | 'pages_total'
  | 'registered_at';

export type FilterBy = 'title' | 'author' | 'isbn';

export interface BookQuery {
  page?: number;
  page_size?: number;
  sort_by?: SortBy;
  sort_direction?: 'asc' | 'desc';
  filter_by?: FilterBy;
  filter?: string;
}
