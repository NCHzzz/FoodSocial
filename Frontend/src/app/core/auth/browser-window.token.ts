import { InjectionToken } from '@angular/core';

export const BROWSER_WINDOW = new InjectionToken<Window>('BrowserWindow', {
  providedIn: 'root',
  factory: () => window,
});
