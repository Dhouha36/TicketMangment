import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AccountService } from '../../_services/account.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-reset-password',
  imports: [ CommonModule, ReactiveFormsModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  token!: string;
  email!: string;
  message: string = '';
  success: boolean = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private accountService: AccountService,
    private router: Router
  ) {
    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  ngOnInit(): void {
    // Récupérer token et email depuis les query params
    this.route.queryParams.subscribe(params => {
      this.token = params['token'];
      this.email = params['email'];
    });
  }

  resetPassword() {
    if (this.resetPasswordForm.invalid) {
      return;
    }

    const dto = {
      email: this.email,
      token: this.token,
      newPassword: this.resetPasswordForm.get('newPassword')?.value
    };

    this.accountService.resetPassword(dto).subscribe({
      next: (response: any) => {
        this.success = true;
        this.message = response.message || "Votre mot de passe a été réinitialisé avec succès.";
        // Rediriger vers la page de login après quelques instants par exemple
        setTimeout(() => {
          this.router.navigateByUrl('/login');
        }, 3000);
      },
      error: (err) => {
        this.success = false;
        this.message = err.error?.message || "Une erreur est survenue lors de la réinitialisation.";
        console.error(err);
      }
    });
  }
}
