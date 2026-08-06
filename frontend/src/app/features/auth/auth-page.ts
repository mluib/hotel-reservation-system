import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/utils/http-error';

type AuthMode = 'signin' | 'signup';

@Component({
  selector: 'app-auth-page',
  imports: [ReactiveFormsModule],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.css',
})
export class AuthPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly mode = signal<AuthMode>('signin');
  protected readonly error = signal<string | null>(null);
  protected readonly submitting = signal(false);

  private returnUrl = '/rooms';

  protected readonly signInForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly signUpForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/']);
      return;
    }
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/rooms';
  }

  setMode(mode: AuthMode): void {
    this.mode.set(mode);
    this.error.set(null);
  }

  submitSignIn(): void {
    if (this.signInForm.invalid) {
      this.error.set('Enter your email and password.');
      return;
    }
    this.error.set(null);
    this.submitting.set(true);
    this.auth.login(this.signInForm.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: (err) => {
        this.submitting.set(false);
        this.error.set(extractErrorMessage(err, 'Invalid credentials.'));
      },
    });
  }

  submitSignUp(): void {
    if (this.signUpForm.invalid) {
      this.error.set('All fields are required.');
      return;
    }
    this.error.set(null);
    this.submitting.set(true);
    this.auth.register(this.signUpForm.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: (err) => {
        this.submitting.set(false);
        this.error.set(extractErrorMessage(err, 'Could not create the account.'));
      },
    });
  }
}
