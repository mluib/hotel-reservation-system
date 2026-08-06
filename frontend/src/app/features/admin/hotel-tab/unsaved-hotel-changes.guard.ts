import { CanDeactivateFn } from '@angular/router';
import { HotelTab } from './hotel-tab';

// Reproduces the mockup's "Save & continue / Discard / Keep editing" gate: if the
// admin form has unsaved edits, hand off to the component's own dialog and let the
// navigation wait on whatever the user picks there.
export const unsavedHotelChangesGuard: CanDeactivateFn<HotelTab> = (component) => {
  if (!component.hasUnsavedChanges()) return true;
  return new Promise<boolean>((resolve) => component.confirmLeave(resolve));
};
