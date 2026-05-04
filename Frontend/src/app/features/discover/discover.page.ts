import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSessionService } from '../../core/auth/auth-session.service';
import {
  getAuthSessionBadgeClasses,
  getAuthSessionBadgeLabel,
  getDiscoverSessionAvailabilityMessage,
  isAuthenticatedSession,
} from '../../core/auth/auth-session.presentation';
import { DishSummaryResponse } from '../../core/dishes/dish.models';
import { PlaceSummaryResponse } from '../../core/places/place.models';
import { DishSearchSelectComponent } from '../dishes/dish-search-select.component';
import { PlaceSearchSelectComponent } from '../places/place-search-select.component';
import { UiButtonDirective } from '../../shared/ui/button/ui-button.directive';
import { UiSurfaceDirective } from '../../shared/ui/surface/ui-surface.directive';

@Component({
  selector: 'app-discover-page',
  imports: [RouterLink, DishSearchSelectComponent, PlaceSearchSelectComponent, UiButtonDirective, UiSurfaceDirective],
  templateUrl: './discover.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscoverPage {
  private readonly authSessionService = inject(AuthSessionService);

  protected readonly authState = this.authSessionService.authState;
  protected readonly isAuthenticated = computed(() => isAuthenticatedSession(this.authState()));
  protected readonly session = computed(() => this.authState().session);
  protected readonly roles = computed(() => this.session()?.user.roles ?? []);
  protected readonly sessionStatusClasses = computed(() => getAuthSessionBadgeClasses(this.authState()));
  protected readonly sessionStatusLabel = computed(() => getAuthSessionBadgeLabel(this.authState()));
  protected readonly sessionAvailabilityMessage = computed(() => getDiscoverSessionAvailabilityMessage(this.authState()));
  protected readonly selectedDish = signal<DishSummaryResponse | null>(null);
  protected readonly selectedPlace = signal<PlaceSummaryResponse | null>(null);
  protected readonly hasCatalogContext = computed(() => this.selectedDish() !== null || this.selectedPlace() !== null);

  protected selectDish(dish: DishSummaryResponse): void {
    this.selectedDish.set(dish);
  }

  protected selectPlace(place: PlaceSummaryResponse): void {
    this.selectedPlace.set(place);
  }
}
