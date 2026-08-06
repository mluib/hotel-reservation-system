import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Cross-field validator for a FormGroup with checkIn/checkOut date controls (values
// are yyyy-MM-dd strings from <input type="date">, so plain string comparison already
// matches chronological order). Reused across booking, room filters, and anywhere else
// the mockup re-validates "check-out after check-in".
export function checkOutAfterCheckIn(
  checkInKey = 'checkIn',
  checkOutKey = 'checkOut',
): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const checkIn = group.get(checkInKey)?.value;
    const checkOut = group.get(checkOutKey)?.value;
    if (!checkIn || !checkOut) return null;
    return checkOut > checkIn ? null : { checkOutBeforeCheckIn: true };
  };
}
