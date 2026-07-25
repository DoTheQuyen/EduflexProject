export class TablePagerState {
  pageNumber = 1;
  pageSize = 20;
  totalCount = 0;
  searchTerm = '';

  goToPage(page: number): void {
    this.pageNumber = page;
  }

  search(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
  }
}
