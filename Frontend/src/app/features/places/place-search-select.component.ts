import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { PlaceApi } from '../../core/places/infrastructure/place.api';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

type PlaceSearchState = 'idle' | 'loading' | 'loaded' | 'empty' | 'error';

@Component({
  selector: 'app-place-search-select',
  imports: [UiButtonDirective, UiSurfaceDirective],
  template: `
    <section appUiSurface="default" appUiSurfacePadding="spacious" class="space-y-5">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-[#c96442]">Optional place context</p>
        <h2 class="mt-2 text-2xl font-semibold tracking-tight text-[#141413]">Attach a restaurant or area when it helps</h2>
        <p class="mt-2 text-sm leading-6 text-[#5e5d59]">
          Places enrich food posts with restaurant and location context, while keeping dish identity as the primary decision layer.
        </p>
      </div>

      <form class="flex flex-col gap-3 sm:flex-row" (submit)="search($event)">
        <label class="min-w-0 flex-1">
          <span class="sr-only">Search places</span>
          <input
            class="min-h-12 w-full rounded-full border border-[#e8e6dc] bg-[#faf9f5] px-5 text-base text-[#141413] shadow-[0_0_0_1px_#f0eee6] outline-none transition placeholder:text-[#87867f] focus:border-[#3898ec] focus:ring-4 focus:ring-[#3898ec]/20"
            type="search"
            name="placeQuery"
            [value]="query()"
            (input)="updateQuery($event)"
            placeholder="Search restaurants, cafés, districts..."
            autocomplete="off"
          />
        </label>
        <button appUiButton="accent" type="submit" [disabled]="state() === 'loading'">Search places</button>
      </form>

      @if (state() === 'loading') {
        <section appUiSurface="subtle">
          <p class="text-sm font-semibold text-[#141413]">Searching canonical places...</p>
          <p class="mt-1 text-sm text-[#5e5d59]">Checking the Place catalog through the BFF.</p>
        </section>
      }

      @if (state() === 'error') {
        <section class="rounded-2xl border border-red-200 bg-red-50 p-4">
          <p class="text-sm font-semibold text-red-950">Place search could not load</p>
          <p class="mt-1 text-sm text-red-700">Try again in a moment. You can still keep the dish selection and caption ready.</p>
        </section>
      }

      @if (state() === 'empty') {
        <section appUiSurface="soft">
          <p class="text-sm font-semibold text-[#141413]">No places found</p>
          <p class="mt-1 text-sm text-[#5e5d59]">Try a broader restaurant name, city, or district.</p>
        </section>
      }

      @if (selectedPlace()) {
        <section class="rounded-2xl border border-[#e8e6dc] bg-[#f0eee6] p-4">
          <p class="text-sm font-semibold text-[#141413]">Selected place</p>
          <p class="mt-1 text-sm text-[#4d4c48]" [textContent]="selectedPlace()?.name"></p>
        </section>
      }

      @if (results().length > 0) {
        <div class="grid gap-3">
          @for (place of results(); track place.id) {
            <article appUiSurface="subtle" class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div class="min-w-0">
                <h3 class="text-lg font-semibold tracking-tight text-[#141413]" [textContent]="place.name"></h3>
                <p class="mt-1 text-sm text-[#5e5d59]">
                  <span [textContent]="place.area"></span>
                  <span aria-hidden="true"> · </span>
                  <span [textContent]="place.city"></span>
                </p>
                <p class="mt-1 text-sm text-[#87867f]" [textContent]="place.addressText"></p>
                @if (place.cuisineTypes.length > 0 || place.priceRange) {
                  <p class="mt-2 text-xs font-medium uppercase tracking-[0.18em] text-[#87867f]">
                    <span [textContent]="place.cuisineTypes.length > 0 ? place.cuisineTypes.join(' / ') : 'Cuisine pending'"></span>
                    @if (place.priceRange) {
                      <span aria-hidden="true"> · </span>
                      <span [textContent]="place.priceRange"></span>
                    }
                  </p>
                }
              </div>
              <button appUiButton="secondary" type="button" (click)="selectPlace(place)">Select place</button>
            </article>
          }
        </div>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceSearchSelectComponent {
  readonly initialQuery = input<string>('');
  readonly placeSelected = output<PlaceSummaryResponse>();

  protected readonly query = signal('');
  protected readonly state = signal<PlaceSearchState>('idle');
  protected readonly results = signal<PlaceSummaryResponse[]>([]);
  protected readonly selectedPlace = signal<PlaceSummaryResponse | null>(null);
  private searchSequence = 0;

  constructor(private readonly placeApi: PlaceApi) {}

  ngOnInit(): void {
    this.query.set(this.initialQuery());
    if (this.query().trim().length > 0) {
      this.loadPlaces();
    }
  }

  protected updateQuery(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    this.query.set(inputElement.value);
  }

  protected search(event: SubmitEvent): void {
    event.preventDefault();
    this.loadPlaces();
  }

  protected selectPlace(place: PlaceSummaryResponse): void {
    this.selectedPlace.set(place);
    this.placeSelected.emit(place);
  }

  private loadPlaces(): void {
    const requestSequence = ++this.searchSequence;
    this.state.set('loading');
    this.results.set([]);

    this.placeApi.searchPlaces({ query: this.query(), limit: 10 }).subscribe({
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
