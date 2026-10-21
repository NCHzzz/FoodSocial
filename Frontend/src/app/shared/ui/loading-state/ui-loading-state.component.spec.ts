import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UiLoadingStateComponent } from './ui-loading-state.component';

describe('UiLoadingStateComponent', () => {
  @Component({
    selector: 'app-host',
    imports: [UiLoadingStateComponent],
    template: `
      <app-ui-loading-state
        [title]="'Loading your data'"
        [description]="description()"
      />
    `,
    standalone: true,
  })
  class HostComponent {
    readonly description = input<string>('');
  }

  it('renders title', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading your data');
  });

  it('renders description when provided', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentRef.setInput('description', 'Fetching from the server.');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Fetching from the server.');
  });
});
