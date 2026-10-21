import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UiEmptyStateComponent } from './ui-empty-state.component';

describe('UiEmptyStateComponent', () => {
  @Component({
    selector: 'app-host',
    imports: [UiEmptyStateComponent],
    template: `
      <app-ui-empty-state
        [title]="'No results'"
        [description]="description()"
        [retryLabel]="retryLabel()"
        [retry]="onRetry"
      />
    `,
    standalone: true,
  })
  class HostComponent {
    readonly description = input<string>('');
    readonly retryLabel = input<string>('');
    readonly onRetry = vi.fn();
  }

  it('renders title', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No results');
  });

  it('renders description when provided', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentRef.setInput('description', 'Try something else.');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Try something else.');
  });

  it('calls retry on button click', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentRef.setInput('retryLabel', 'Retry');
    fixture.detectChanges();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    button.click();
    expect(fixture.componentRef.instance.onRetry).toHaveBeenCalledOnce();
  });
});
