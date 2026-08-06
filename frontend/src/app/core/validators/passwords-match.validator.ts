import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordsMatch(
  passwordKey = 'password',
  confirmKey = 'confirmPassword',
): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey)?.value;
    const confirm = group.get(confirmKey)?.value;
    if (!password || !confirm) return null;
    return password === confirm ? null : { passwordsMismatch: true };
  };
}
