import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomersService } from '../../../../core/services/customers.service';
import { Customer } from '../../../../core/models/customer.model';
import { extractErrorMessage } from '../../../../core/utils/http-error';

// Edit-only — there's no admin "create customer" endpoint; customer records only
// ever come into being through self-registration.
@Component({
  selector: 'app-customer-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-dialog.html',
})
export class CustomerDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customersService = inject(CustomersService);

  readonly customer = input.required<Customer>();
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    const c = this.customer();
    this.form.setValue({ firstName: c.firstName, lastName: c.lastName, email: c.email });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.error.set('All fields are required.');
      return;
    }

    this.submitting.set(true);
    this.customersService.update(this.customer().id, this.form.getRawValue()).subscribe({
      next: () => this.saved.emit(),
      error: (err) => {
        this.submitting.set(false);
        this.error.set(extractErrorMessage(err));
      },
    });
  }
}
