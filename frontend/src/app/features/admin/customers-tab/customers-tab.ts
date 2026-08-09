import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CustomersService } from '../../../core/services/customers.service';
import { AdminHighlightService } from '../admin-highlight.service';
import { Customer } from '../../../core/models/customer.model';
import { extractErrorMessage } from '../../../core/utils/http-error';
import { CustomerDialog } from '../dialogs/customer-dialog/customer-dialog';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';
import { ErrorDialog } from '../../../shared/error-dialog/error-dialog';

type SortKey = 'firstName' | 'lastName' | 'email';

@Component({
  selector: 'app-customers-tab',
  imports: [CustomerDialog, ConfirmDialog, ErrorDialog],
  templateUrl: './customers-tab.html',
})
export class CustomersTab implements OnInit {
  private readonly customersService = inject(CustomersService);
  protected readonly highlight = inject(AdminHighlightService);

  private readonly customers = signal<Customer[]>([]);
  private readonly sort = signal<{ key: SortKey; dir: 'asc' | 'desc' }>({
    key: 'lastName',
    dir: 'asc',
  });

  protected readonly sortedCustomers = computed(() => {
    const { key, dir } = this.sort();
    return [...this.customers()].sort((a, b) => {
      const cmp = a[key].localeCompare(b[key]);
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  protected readonly editingCustomer = signal<Customer | null>(null);
  protected readonly pendingDelete = signal<Customer | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected sortArrow(key: SortKey): string {
    const s = this.sort();
    return s.key !== key ? '' : s.dir === 'asc' ? '▲' : '▼';
  }

  protected toggleSort(key: SortKey): void {
    this.sort.update((s) => ({
      key,
      dir: s.key === key && s.dir === 'asc' ? 'desc' : 'asc',
    }));
  }

  protected onDialogSaved(): void {
    this.editingCustomer.set(null);
    this.load();
  }

  protected confirmDelete(): void {
    const customer = this.pendingDelete();
    if (!customer) return;
    this.customersService.delete(customer.id).subscribe({
      next: () => {
        this.pendingDelete.set(null);
        this.load();
      },
      error: (err) => {
        this.pendingDelete.set(null);
        this.deleteError.set(extractErrorMessage(err));
      },
    });
  }

  private load(): void {
    this.customersService.getAll().subscribe((customers) => this.customers.set(customers));
  }
}
