import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotFoundPage } from './not-found.page';

describe('NotFoundPage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
  });

  it('renders the heading', () => {
    const fixture = TestBed.createComponent(NotFoundPage);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('This page does not exist');
  });
});
