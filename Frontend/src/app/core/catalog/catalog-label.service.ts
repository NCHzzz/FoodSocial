import { Injectable, Signal, WritableSignal, inject, signal } from '@angular/core';

import { DishApi } from '../dishes/infrastructure/dish.api';
import { PlaceApi } from '../places/infrastructure/place.api';

type CatalogLabelStatus = 'loading' | 'loaded' | 'unavailable';

interface CatalogLabelState {
  readonly label: string;
  readonly status: CatalogLabelStatus;
}

@Injectable({ providedIn: 'root' })
export class CatalogLabelService {
  private readonly dishApi = inject(DishApi);
  private readonly placeApi = inject(PlaceApi);
  private readonly dishLabels = new Map<string, WritableSignal<CatalogLabelState>>();
  private readonly placeLabels = new Map<string, WritableSignal<CatalogLabelState>>();

  resolveDish(dishId: string): Signal<CatalogLabelState> {
    return this.resolveLabel(this.dishLabels, dishId, 'Dish details loading', () => {
      this.dishApi.getDish(dishId).subscribe({
        next: (dish) => this.dishLabels.get(dishId)?.set({ label: dish.name, status: 'loaded' }),
        error: () => this.dishLabels.get(dishId)?.set({ label: 'Dish unavailable', status: 'unavailable' }),
      });
    });
  }

  resolvePlace(placeId: string): Signal<CatalogLabelState> {
    return this.resolveLabel(this.placeLabels, placeId, 'Place details loading', () => {
      this.placeApi.getPlace(placeId).subscribe({
        next: (place) => this.placeLabels.get(placeId)?.set({ label: place.name, status: 'loaded' }),
        error: () => this.placeLabels.get(placeId)?.set({ label: 'Place unavailable', status: 'unavailable' }),
      });
    });
  }

  dishLabel(dishId: string): string {
    return this.resolveDish(dishId)().label;
  }

  placeLabel(placeId: string): string {
    return this.resolvePlace(placeId)().label;
  }

  private resolveLabel(
    labels: Map<string, WritableSignal<CatalogLabelState>>,
    id: string,
    loadingLabel: string,
    load: () => void,
  ): Signal<CatalogLabelState> {
    const existingLabel = labels.get(id);
    if (existingLabel) {
      return existingLabel.asReadonly();
    }

    const labelState = signal<CatalogLabelState>({ label: loadingLabel, status: 'loading' });
    labels.set(id, labelState);
    load();
    return labelState.asReadonly();
  }
}
