import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, AbstractControl, ValidationErrors, ValidatorFn, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AccountService } from '../../_services/account.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm: FormGroup;
  token: string = '';
  message: string | null = '';

  // Variables de gestion d'état
  formSubmitted: boolean = false;
  isLoading: boolean = false;
  resetPasswordError: any;

  // Pour l'affichage/masquage du mot de passe
  passwordVisible: boolean = false;
  confirmPasswordVisible: boolean = false;

  // Définir le validateur personnalisé directement dans le composant
  private newPasswordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    if (!newPassword || !confirmPassword) {
      return null;
    }
    return newPassword !== confirmPassword ? { passwordMismatch: true } : null;
  };

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private accountService: AccountService
  ) {
    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(16),
        Validators.pattern(/^[\w!@#$%^&*(),.?":{}|<>]+$/)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.newPasswordMatchValidator });
  }

  ngOnInit(): void {
    // ← au lieu de route.queryParams
    this.route.queryParamMap.subscribe(params => {
      const t = params.get('token');
      if (!t) {
        // pas de token → on renvoie au login
        this.router.navigate(['/']);
        return;
      }
      // on stocke le token et on peut préremplir (ou décocher le bouton submit, etc.)
      this.token = t.trim();
    });
  }  
  
  // Bascule de l'affichage du mot de passe pour le champ newPassword
  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  // Bascule de l'affichage du mot de passe pour le champ confirmPassword
  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible = !this.confirmPasswordVisible;
  }
  
  // Envoi du formulaire
  onSubmit(): void {
    this.formSubmitted = true;
    if (this.resetPasswordForm.invalid) {
      return;
    }
    
    const body = {
      token: this.token,
      newPassword: this.resetPasswordForm.get('newPassword')?.value,
      confirmPassword: this.resetPasswordForm.get('confirmPassword')?.value
    };

    console.log(body); // Pour vérifier le payload

    this.isLoading = true;
    this.accountService.resetPassword(body)
      .subscribe({
        next: (res: any) => {
          this.message = res.message;
          this.isLoading = false;
          setTimeout(() => this.router.navigate(['']), 3000);
        },
        error: (err) => {
          this.message = 'Une erreur est survenue lors de la réinitialisation du mot de passe.';
          this.resetPasswordError = err.error || {};
          this.isLoading = false;
        }
      });
  }
  dismissMessage() {
    this.message = null;
  }
}