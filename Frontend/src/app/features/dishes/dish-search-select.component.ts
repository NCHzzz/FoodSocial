import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { DishApi } from '../../core/dishes/infrastructure/dish.api';
import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type DishSearchState = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';

@Component({
  selector: 'app-dish-search-select',
  imports: [UiButtonDirective, UiSurfaceDirective],
  template: `
    <section appUiSurface="default" appUiSurfacePadding="spacious" class="space-y-5">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-orange-600">Canonical dish</p>
        <h2 class="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Choose the dish identity first</h2>
        <p class="mt-2 text-sm leading-6 text-slate-600">
          Food posts need a stable dish identity so search, saves, and recommendations can grow without guessing from captions.
        </p>
      </div>

      <form class="flex flex-col gap-3 sm:flex-row" (submit)="search($event)">
        <label class="min-w-0 flex-1">
          <span class="sr-only">Search dishes</span>
          <input
            class="min-h-12 w-full rounded-full border border-slate-200 bg-white px-5 text-base text-slate-950 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-orange-400 focus:ring-4 focus:ring-orange-100"
            type="search"
            name="dishQuery"
            [value]="query()"
            (input)="updateQuery($event)"
            placeholder="Search phở, bún chả, cơm tấm..."
            autocomplete="off"
          />
        </label>
        <button appUiButton="accent" type="submit" [disabled]="state() === 'loading'">Search dishes</button>
      </form>

      @if (state() === 'loading') {
        <section appUiSurface="subtle">
          <p class="text-sm font-semibold text-slate-950">Searching canonical dishes...</p>
          <p class="mt-1 text-sm text-slate-600">Checking the Dish catalog through the BFF.</p>
        </section>
      }

      @if (state() === 'error') {
        <section class="rounded-2xl border border-red-200 bg-red-50 p-4">
          <p class="text-sm font-semibold text-red-950">Dish search could not load</p>
          <p class="mt-1 text-sm text-red-700">Try again in a moment. The post form can stay open while the catalog recovers.</p>
        </section>
      }

      @if (state() === 'empty') {
        <section appUiSurface="soft">
          <p class="text-sm font-semibold text-slate-950">No dishes found</p>
          <p class="mt-1 text-sm text-slate-600">Try a broader dish name or Vietnamese spelling variant.</p>
        </section>
      }

      @if (selectedDish()) {
        <section class="rounded-2xl border border-orange-200 bg-orange-50 p-4">
          <p class="text-sm font-semibold text-orange-950">Selected dish</p>
          <p class="mt-1 text-sm text-orange-800" [textContent]="selectedDish()?.name"></p>
        </section>
      }

      @if (results().length > 0) {
        <div class="grid gap-3">
          @for (dish of results(); track dish.id) {
            <article appUiSurface="subtle" class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div class="min-w-0">
                <h3 class="text-lg font-semibold tracking-tight text-slate-950" [textContent]="dish.name"></h3>
                <p class="mt-1 text-sm text-slate-600">
                  <span [textContent]="dish.category ?? 'Uncategorized'"></span>
                  <span aria-hidden="true"> · </span>
                  <span [textContent]="dish.cuisineType ?? 'Cuisine pending'"></span>
                </p>
                @if (dish.aliases.length > 0) {
                  <p class="mt-2 text-xs font-medium uppercase tracking-[0.18em] text-slate-500" [textContent]="dish.aliases.join(' / ')"></p>
                }
              </div>
              <button appUiButton="secondary" type="button" (click)="selectDish(dish)">Select dish</button>
            </article>
          }
        </div>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DishSearchSelectComponent {
  readonly initialQuery = input<string>('');
  readonly dishSelected = output<DishSummaryResponse>();

  protected readonly query = signal('');
  protected readonly state = signal<DishSearchState>('idle');
  protected readonly results = signal<DishSummaryResponse[]>([]);
  protected readonly selectedDish = signal<DishSummaryResponse | null>(null);
  private searchSequence = 0;

  constructor(private readonly dishApi: DishApi) {}

  ngOnInit(): void {
    this.query.set(this.initialQuery());
    if (this.query().trim().length > 0) {
      this.loadDishes();
    }
  }

  protected updateQuery(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.query.set(input.value);
  }

  protected search(event: SubmitEvent): void {
    event.preventDefault();
    this.loadDishes();
  }

  protected selectDish(dish: DishSummaryResponse): void {
    this.selectedDish.set(dish);
    this.dishSelected.emit(dish);
  }

  private loadDishes(): void {
    const requestSequence = ++this.searchSequence;
    this.state.set('loading');
    this.results.set([]);

    this.dishApi.searchDishes({ query: this.query(), limit: 10 }).subscribe({
      next: (response) => {
        if (requestSequence !== this.searchSequence) {
          return;
        }

        this.results.set(response.items);
        this.state.set(response.items.length > 0 ? 'loaded' : 'empty');
      },
      error: () => {
        if (requestSequence !== this.searchSequence) {
          return;
        }

        this.results.set([]);
        this.state.set('error');
      },
    });
  }
}
