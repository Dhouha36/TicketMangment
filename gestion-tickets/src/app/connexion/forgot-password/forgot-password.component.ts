import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountService } from '../../_services/account.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-forgot-password',
  imports: [ FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  forgotPasswordForm: FormGroup;
  message: string = '';

  constructor(private fb: FormBuilder, private accountService: AccountService) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  sendResetLink() {
    if (this.forgotPasswordForm.invalid) {
      return;
    }

    this.accountService.forgotPassword(this.forgotPasswordForm.value.email).subscribe({
      next: (response) => {
        this.message = 'Un lien de réinitialisation a été envoyé à votre adresse e-mail.';
      },
      error: (err) => {
        this.message = 'Une erreur est survenue lors de l’envoi du lien de réinitialisation.';
        console.error(err);
      }
    });
  }
}
