import { ClientService } from './../_services/client.service';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ValidatorFn, ReactiveFormsModule } from '@angular/forms';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';
import { PaysService } from '../_services/pays.service';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { LoaderService } from '../_services/loader.service';
import { GlobalLoaderService } from '../_services/global-loader.service';
import { ClientDto } from '../DTOs/ClientDto';
import { ClientUpdateDto } from '../DTOs/ClientUpdateDto';

// Validateur personnalisé pour vérifier que 'nouveauPassword' et 'confirmNouveauPassword' correspondent
export const newPasswordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const nouveauPassword = control.get('nouveauPassword');
  const confirmNouveauPassword = control.get('confirmNouveauPassword');
  if (!nouveauPassword?.value && !confirmNouveauPassword?.value) {
    return null;
  }
  return (nouveauPassword && confirmNouveauPassword && nouveauPassword.value !== confirmNouveauPassword.value)
    ? { passwordMismatch: true }
    : null;
};

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  userDetails: User | ClientDto | null = null;
  userForm!: FormGroup;
  passwordVisible: boolean = false;
  confirmPasswordVisible: boolean = false;

  // Liste des pays chargée via PaysService
  paysList: any[] = [];
  // Propriété pour afficher le drapeau et le code téléphone
  selectedCountry: any = null;
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private accountService: AccountService,
    private paysService: PaysService,
    private clientService: ClientService,
    private toastr: ToastrService,
    private loaderService: LoaderService,
    private globalLoaderService: GlobalLoaderService
  ) {
    this.loaderService.isLoading$.subscribe(loading => {
      this.isLoading = loading;
    });
  }

  ngOnInit(): void {

    this.initForm();
    this.globalLoaderService.showGlobalLoader();
    // Charger la liste des pays avant de charger l'utilisateur
    this.paysService.getPays().subscribe({
      next: (pays) => {
        this.paysList = pays;
        this.loadUserDetails();
        this.globalLoaderService.hideGlobalLoader();
      },
      error: (err) => {
        console.error("Erreur lors du chargement des pays", err);
      }
    });
  }

  initForm(): void {
    this.userForm = this.fb.group({
      id: [null],
      lastName: [{ value: '', disabled: true }, Validators.required],
      firstName:[{ value: '', disabled: true }, Validators.required],
      email: ['', [Validators.required, Validators.email]],
      numTelephone: ['', [
        Validators.required,
        Validators.pattern(/^[0-9\s]+$/),
        Validators.minLength(8),
        Validators.maxLength(10)
      ]],
      actif: [false],
      // → pour l’utilisateur interne
      role: [{ value: '', disabled: true }],
      pays: [{ value: '', disabled: true }],
      // → pour le client
      paysId: [{ value: '', disabled: true }],
      societeId: [{ value: '', disabled: true }],
      societe: [{ value: '', disabled: true }],
      // Changement de mot de passe (optionnel)
      nouveauPassword: ['', [Validators.minLength(8), Validators.maxLength(16)]],
      confirmNouveauPassword: [''],
    }, { validators: newPasswordMatchValidator });
  }

  // Convertit l'ID d'un pays en son nom
  getPaysName(paysId: string | number | undefined): string {
    if (paysId === undefined) {
      return '';
    }
    const id = Number(paysId);
    const pays = this.paysList.find(p => p.idPays === id);
    return pays ? pays.nom : '';
  }


  loadUserDetails(): void {
    this.globalLoaderService.showGlobalLoader();
    this.userDetails = this.accountService.currentUser();
    if (!this.userDetails) {
      this.globalLoaderService.hideGlobalLoader();
      return;
    }

    const country = this.paysList.find(p =>
      p.idPays === Number((this.userDetails as any).paysId ?? this.userDetails!.pays)
    );
    const codeTel = country?.codeTel || '';
    let localNumber = this.userDetails.numTelephone || '';
    if (codeTel && localNumber.startsWith(codeTel)) {
      localNumber = localNumber.substring(codeTel.length).trim();
    }
    this.selectedCountry = country;

    const patch: any = {
      id: this.userDetails.id,
      lastName: this.userDetails.lastName,
      firstName: this.userDetails.firstName,
      email: this.userDetails.email,
      numTelephone: localNumber,
      actif: this.userDetails.actif
    };

    if (this.isClient()) {
      const c = this.userDetails as ClientDto;
      this.userForm.patchValue({
        id: c.id,
        lastName: c.lastName,
        firstName: c.firstName,
        email: c.email,
        numTelephone: c.numTelephone,
        actif: c.actif,
        paysId: c.paysId,
        societeId: c.societeId,
        societe:   c.societe.nom,
      });
    } else {
      const u = this.userDetails as User;
      this.userForm.patchValue({
        id: u.id,
        lastName: u.lastName,
        firstName: u.firstName,
        email: u.email,
        numTelephone: u.numTelephone,
        actif: u.actif,
        pays: u.pays,
        role: u.role,
      });
    }

    this.userForm.patchValue(patch);
  
    this.userForm.get('lastName')!.disable();
    this.userForm.get('firstName')!.disable();
    this.globalLoaderService.hideGlobalLoader();
  }

  onSubmit(): void {
    if (!this.userForm.dirty) {
      this.toastr.warning("Veuillez modifier au moins un champ.");
      return;
    }
    if (this.userForm.invalid) {
      this.toastr.error("Veuillez corriger les erreurs du formulaire.");
      return;
    }

    // Affiche le loader
    this.loaderService.showLoader();

    // Selon le type de DTO, on appelle la bonne méthode
    if (this.isClient()) {
      this.submitClient();
    } else {
      this.submitUser();
    }
  }

  private submitClient(): void {
    const raw = this.userForm.getRawValue();
    const dto: ClientUpdateDto = {
      id: raw.id,
      email: raw.email,
      firstName: raw.firstName,
      lastName: raw.lastName,
      numTelephone: raw.numTelephone,
      actif: raw.actif,
      paysId: raw.paysId, 
      societeId: raw.societeId,
      nouveauPassword: raw.nouveauPassword || undefined,
      confirmNouveauPassword: raw.confirmNouveauPassword || undefined
    };
  
    this.loaderService.showLoader();
    this.clientService.update(dto.id, dto).subscribe({
      next: () => {
        this.clientService.getById(dto.id).subscribe({
          next: updated => {
            // 1) Restaurer l’ancien token
            const current = this.accountService.currentUser() as ClientDto;
            updated.token = current.token;
  
            // 2) Mettre à jour le signal
            this.accountService.setCurrentUser(updated);
  
            // 3) Mettre à jour le localStorage
            localStorage.setItem('user', JSON.stringify(updated));
  
            // 4) Recharger le formulaire
            this.userDetails = updated;
            this.loadUserDetails();
  
            this.toastr.success("Compte mis à jour avec succès.");
            this.loaderService.hideLoader();
          },
          error: () => {
            this.toastr.error("Compte mis à jour, mais impossible de rafraîchir.");
            this.loaderService.hideLoader();
          }
        });
      },
      error: () => {
        this.toastr.error("Erreur lors de la mise à jour du client.");
        this.loaderService.hideLoader();
      }
    });
  }
  

  private submitUser(): void {
    const dto = this.userForm.getRawValue() as User;
    this.loaderService.showLoader();
  
    // 1) Récupère le token depuis le localStorage
    const stored = localStorage.getItem('user');
    let token: string | undefined;
    if (stored) {
      try {
        token = JSON.parse(stored)?.token;
      } catch {
        token = undefined;
      }
    }
  
    // 2) Appelle l'API de mise à jour
    this.accountService.updateUser(dto).subscribe({
      next: () => {
        // 3) Recharge l'utilisateur complet
        this.accountService.getUser(dto.id).subscribe({
          next: (freshUser) => {
            if (freshUser) {
              // 4) Réinjecte le token préservé
              if (token) {
                (freshUser as any).token = token;
              }
              // 5) Mets à jour le signal et le localStorage
              this.accountService.setCurrentUser(freshUser);
              localStorage.setItem('user', JSON.stringify(freshUser));
  
              // Rechage le formulaire et UI
              this.userDetails = freshUser;
              this.loadUserDetails();
  
              this.toastr.success("Compte mis à jour avec succès.");
              this.userForm.patchValue({
                nouveauPassword: '',
                confirmNouveauPassword: ''
              });
              // en bonus, tu peux les repasser en pristine et untouched
              this.userForm.get('nouveauPassword')!.markAsPristine();
              this.userForm.get('nouveauPassword')!.markAsUntouched();
              this.userForm.get('confirmNouveauPassword')!.markAsPristine();
              this.userForm.get('confirmNouveauPassword')!.markAsUntouched();
            } else {
              this.toastr.error("Utilisateur mis à jour, mais impossible de le recharger.");
            }
            this.loaderService.hideLoader();
          },
          error: () => {
            this.toastr.error("Erreur lors du rechargement de l'utilisateur.");
            this.loaderService.hideLoader();
          }
        });
      },
      error: () => {
        this.toastr.error("Erreur lors de la mise à jour de l'utilisateur.");
        this.loaderService.hideLoader();
      }
    });
  }
  
  
  onCancel(): void {
    if (!this.userDetails) return;

    // On repopule selon le type
    if (this.isClient()) {
      const c = this.userDetails as ClientDto;
      this.userForm.patchValue({
        lastName: c.lastName,
        firstName: c.firstName,
        email: c.email,
        pays: c.paysId,
        societe: c.societe.nom,     // pas de role ici
        numTelephone: c.numTelephone,
        actif: c.actif,
        // pas de nouveauPassword / confirmNouveauPassword côté client ?
      });
    } else {
      const u = this.userDetails as User;
      this.userForm.patchValue({
        lastName: u.lastName,
        firstName: u.firstName,
        email: u.email,
        pays: u.pays,
        role: u.role,
        numTelephone: u.numTelephone,
        actif: u.actif,
        // etc.
      });
    }
  }


  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible = !this.confirmPasswordVisible;
  }

  isClient(): this is { userDetails: ClientDto } {
    return !!this.userDetails
      && (this.userDetails as any).paysId !== undefined
      && Array.isArray((this.userDetails as any).projets);
  }

  getInitials(): string {
    if (!this.userDetails) {
      return '';
    }
    const first = this.userDetails.firstName ?? '';
    const last = this.userDetails.lastName ?? '';
    return `${first.charAt(0).toUpperCase()}${last.charAt(0).toUpperCase()}`;
  }

  get roleLabel(): string {
    if (this.userDetails && !this.isClient()) {
      // on sait que userDetails est un User ici
      return (this.userDetails as User).role;
    }
    return '';
  }
}