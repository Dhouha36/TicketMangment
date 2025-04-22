import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormGroup, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Validators } from '@angular/forms';
import { AccountService } from '../../_services/account.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  imports: [ ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  forgotPasswordForm: FormGroup;
  message: string | null = '';
  isLoading: boolean = false;
  formSubmitted: boolean = false;
  forgotPasswordError: any;
  isErrorMessage = false;

  constructor(private fb: FormBuilder, private accountService: AccountService) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit() {
    this.formSubmitted = true;
    if (this.forgotPasswordForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.accountService.forgotPassword(this.forgotPasswordForm.value)
      .subscribe({
        next: (res: any) => {
          this.message = res.message;
          this.isErrorMessage = false;
          this.isLoading = false;
        },
        error: (err) => {
          this.message = 'Une erreur est survenue. Veuillez réessayer plus tard.';
          // Vous pouvez ajouter ici un traitement détaillé des erreurs renvoyées par le backend
          this.isErrorMessage = true;
          this.forgotPasswordError = err.error || {};
          this.isLoading = false;
        }
      });
  }
  dismissMessage() {
    this.message = null;
  }
}