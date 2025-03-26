import { Component, inject } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { AccountService } from '../../_services/account.service';
import { Router, RouterLink } from '@angular/router';
import { NgClass, NgIf } from '@angular/common';

@Component({
    selector: 'app-login',
    imports: [NgClass, NgIf, ReactiveFormsModule, FormsModule, RouterLink],
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.css']
})
export class LoginComponent {
  accountService = inject(AccountService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  formSubmitted = false; 
  loginError: { email?: string; password?: string } = {}; // Variable pour stocker l'erreur d'authentification
  passwordVisible: boolean = false;  // Variable pour contrôler la visibilité du mot de passe

  rememberMe: boolean = false;

  login() {
    this.formSubmitted = true; 
    this.loginError = {}; // Réinitialisation des erreurs

    if (this.loginForm.invalid) {
      return;
    }
    
    this.accountService.login(this.loginForm.value).subscribe({
      next: (userDto) => {
        // En fonction du choix "Se souvenir de moi", sauvegarder le token
        if (this.rememberMe) {
          localStorage.setItem('authToken', userDto.token);
        } else {
          sessionStorage.setItem('authToken', userDto.token);
        }
        this.router.navigateByUrl('/home/dashboard');
      },
      error: (error) => {
        const errorMsg = error.error?.message || 'Erreur inconnue';
        if (errorMsg.toLowerCase().includes('e-mail')) {
          this.loginError.email = errorMsg;
        } else if (errorMsg.toLowerCase().includes('mot de passe')) {
          this.loginError.password = errorMsg;
        } else {
          this.loginError.email = errorMsg;
        }
        console.error('Erreur interceptée :', error);
      },
    });
  }
  
  

   // Fonction pour basculer la visibilité du mot de passe
   togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;  // Toggle entre true et false
  }
  
}

