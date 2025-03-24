import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { ContractDialogComponent } from '../../contract-dialog/contract-dialog.component';
import { Pays } from '../../_models/pays';
import { PaysService } from '../../_services/pays.service';
import { SocieteService } from '../../_services/societe.service';
import { Societe } from '../../_models/societe';
import { CommonModule } from '@angular/common';
import { AccountService } from '../../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RoleService } from '../../_services/role.service';
import { Role } from '../../_models/role.model';
import { LoaderService } from '../../_services/loader.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../../_services/overlay-modal.service';

@Component({
  selector: 'app-ajouter-utilisateur',
  imports: [CommonModule, ReactiveFormsModule, MatTooltipModule],
  templateUrl: './ajouter-utilisateur.component.html',
  styleUrls: ['./ajouter-utilisateur.component.scss']
})
export class AjouterUtilisateurComponent implements OnInit {
  registerForm: FormGroup;
  roles: Role[] = [];
  passwordVisible = false;
  confirmPasswordVisible = false;
  paysList: Pays[] = [];
  societesList: Societe[] = [];
  selectedCountry: Pays | undefined;

  isLoading: boolean = false;


  constructor(
    private paysService: PaysService,
    private societeService: SocieteService,
    private roleService: RoleService,
    private dialog: MatDialog,
    private fb: FormBuilder,
    private accountService: AccountService,
    private toastr: ToastrService,
    private router: Router,
    private loaderService: LoaderService,
    private overlayModalService: OverlayModalService,
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
    this.registerForm = this.fb.group({
      lastName: ['', Validators.required],
      firstName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      pays: ['', Validators.required],
      role: ['', Validators.required],
      societe: [{ value: '', disabled: true }],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(16)]],
      numTelephone: ['', [
        Validators.required,
        Validators.pattern(/^[0-9\s]+$/),
        Validators.minLength(8),
        Validators.maxLength(10)
      ]]
      ,
      confirmPassword: ['', Validators.required],
      actif: [false],
      contrat: [false],
      contract: this.fb.group({
        dateDebut: ['', Validators.required],
        dateFin: ['', Validators.required],
        type: ['Standard', Validators.required]
      })
    }, { validators: passwordMatchValidator });
  }

  ngOnInit(): void {
    // Chargement des listes de pays et de sociétés
    this.loadPays();
    this.loadSocietes();
    this.loadRoles();

    this.registerForm.get('pays')?.valueChanges.subscribe(value => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +value);
    });

    // Récupération des contrôles pour plus de lisibilité
    const contratControl = this.registerForm.get('contrat');
    const contractGroup = this.registerForm.get('contract');

    this.registerForm.get('role')?.valueChanges.subscribe(role => {
      const societeControl = this.registerForm.get('societe');
      if (role && role.toLowerCase() === 'client') {
        societeControl?.enable(); // Active le champ pour le rôle "client"
        societeControl?.setValidators(Validators.required);
      } else {
        societeControl?.disable(); // Désactive le champ pour les autres rôles
        societeControl?.clearValidators();
        societeControl?.setValue(''); // Optionnel : réinitialise la valeur
      }
      societeControl?.updateValueAndValidity();
    });

    // Au démarrage, si la case 'contrat' est false, désactivez le groupe 'contract'
    if (!contratControl?.value) {
      contractGroup?.disable();
      // On retire les validateurs de chaque contrôle du groupe
      contractGroup?.get('dateDebut')?.clearValidators();
      contractGroup?.get('dateFin')?.clearValidators();
      contractGroup?.get('type')?.clearValidators();
      contractGroup?.updateValueAndValidity();
    }

    // Surveillez les changements de la sélection de la société
    this.registerForm.get('societe')?.valueChanges.subscribe(selectedSociete => {
      if (selectedSociete && selectedSociete !== '') {
        // Si une société est sélectionnée, on désactive la case 'contrat' et le groupe 'contract'
        this.registerForm.get('contrat')?.disable();
        this.registerForm.get('contrat')?.setValue(false);
        contractGroup?.disable();
        contractGroup?.get('dateDebut')?.clearValidators();
        contractGroup?.get('dateFin')?.clearValidators();
        contractGroup?.get('type')?.clearValidators();
        contractGroup?.updateValueAndValidity();
      } else {
        // Sinon, on réactive la case 'contrat'
        this.registerForm.get('contrat')?.enable();
      }
    });

    // Surveillez les changements de la case 'contrat'
    contratControl?.valueChanges.subscribe(isChecked => {
      if (isChecked) {
        // Si la case est cochée, activez le groupe 'contract' et appliquez les validateurs requis
        contractGroup?.enable();
        contractGroup?.get('dateDebut')?.setValidators(Validators.required);
        contractGroup?.get('dateFin')?.setValidators(Validators.required);
        contractGroup?.get('type')?.setValidators(Validators.required);
        // Mise à jour de la validité des contrôles du groupe
        contractGroup?.get('dateDebut')?.updateValueAndValidity();
        contractGroup?.get('dateFin')?.updateValueAndValidity();
        contractGroup?.get('type')?.updateValueAndValidity();
      } else {
        // Si la case est décochée, désactivez le groupe et retirez ses validateurs
        contractGroup?.disable();
        contractGroup?.get('dateDebut')?.clearValidators();
        contractGroup?.get('dateFin')?.clearValidators();
        contractGroup?.get('type')?.clearValidators();
        contractGroup?.updateValueAndValidity();
      }
    });
  }

  loadPays(): void {
    this.paysService.getPays().subscribe({
      next: (pays: Pays[]) => this.paysList = pays,
      error: (err) => {
        console.error('Erreur lors de la récupération des pays', err);
        this.toastr.error("Erreur lors du chargement des pays.");
      }
    });
  }

  loadSocietes(): void {
    this.societeService.getSocietes().subscribe({
      next: (societes: Societe[]) => this.societesList = societes,
      error: (err) => {
        console.error('Erreur lors de la récupération des sociétés', err);
        this.toastr.error("Erreur lors du chargement des sociétés.");
      }
    });
  }

  loadRoles(): void {
    this.roleService.getRoles().subscribe({
      next: (roles: Role[]) => {
        this.roles = roles;
      },
      error: (err) => {
        console.error("Erreur lors du chargement des rôles", err);
        this.toastr.error("Erreur lors du chargement des rôles.");
      }
    });
  }

  // Ouvre le dialogue pour renseigner le contrat
  onContractChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.checked) {
      this.openContractDialog();
    }
  }

  openContractDialog(): void {
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      data: {
        contractForm: this.registerForm.get('contract')
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        // Si le dialogue est fermé sans sauvegarder, on désactive la case à cocher contrat
        this.registerForm.get('contrat')?.setValue(false);
      }
    });
  }

  // Bouton "Créer" : crée l'utilisateur et redirige vers la liste
  register(): void {
    if (this.registerForm.valid) {
      const formValue = this.registerForm.value;
      const hasSociete = formValue.societe && formValue.societe !== '';

      const phoneNumber = formValue.numTelephone; 
      // Concaténer le code pays si disponible
      const fullPhoneNumber = this.selectedCountry
        ? this.selectedCountry.codeTel + ' ' + phoneNumber
        : phoneNumber;

      // Construction de l'objet utilisateur à envoyer à l'API
      const userForRegister: any = {
        email: formValue.email,
        role: formValue.role.charAt(0).toUpperCase() + formValue.role.slice(1),
        firstname: formValue.firstName,
        lastname: formValue.lastName,
        numtelephone: fullPhoneNumber,
        pays: +formValue.pays,
        actif: formValue.actif,
        password: formValue.password,
        societeId: hasSociete ? +formValue.societe : null,
        contract: null
      };

      // Si la case contrat est cochée ET que l'utilisateur n'est PAS rattaché à une société
      if (formValue.contrat) {
        if (hasSociete) {
          this.toastr.error("Un utilisateur lié à une société ne peut pas créer de contrat.");
          return;
        } else {
          userForRegister.contract = {
            dateDebut: formValue.contract.dateDebut,
            dateFin: formValue.contract.dateFin,
            type: formValue.contract.type
          };
        }
      }

      this.loaderService.showLoader();

      this.accountService.register(userForRegister).subscribe({
        next: user => {
          this.toastr.success("Utilisateur enregistré avec succès.");
          this.loaderService.hideLoader();
          this.router.navigate(['/home/utilisateurs'], { queryParams: { newUser: user.id } });
        },
        error: err => {
          console.error('Erreur lors de l\'enregistrement', err);
          // Extraire le message d'erreur exact, en tenant compte d'une potentielle présence de err.error.message
          const errorMessage = err.error?.message || err.message || "Erreur lors de l'enregistrement de l'utilisateur.";
          this.toastr.error(errorMessage);
          this.loaderService.hideLoader();
        }
      });
    } else {
      this.toastr.warning("Veuillez remplir correctement le formulaire.");
    }
  }



  // Méthode pour réinitialiser le formulaire à son état initial
  private resetForm(): void {
    this.registerForm.reset({
      lastName: '',
      firstName: '',
      email: '',
      pays: '',
      role: '',
      societe: '',
      password: '',
      numTelephone: '',
      confirmPassword: '',
      actif: false,
      contrat: false,
      contract: {
        dateDebut: '',
        dateFin: '',
        type: 'Standard'
      }
    });
    // Réinitialiser l'état du groupe contract (désactivé et sans validateurs)
    const contractGroup = this.registerForm.get('contract');
    if (contractGroup) {
      contractGroup.disable();
      contractGroup.get('dateDebut')?.clearValidators();
      contractGroup.get('dateFin')?.clearValidators();
      contractGroup.get('type')?.clearValidators();
      contractGroup.updateValueAndValidity();
    }
  }

  // Bouton "Annuler" : si des modifications sont présentes, demander confirmation, sinon retourner à la liste
  cancel(): void {
    if (this.registerForm.dirty) {
      const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
      modalInstance.message = "Vous avez des modifications non sauvegardées. Voulez-vous vraiment annuler ?";
  
      modalInstance.confirmed.subscribe(() => {
        this.router.navigate(['/home/utilisateurs']);
        this.overlayModalService.close();
      });
  
      modalInstance.cancelled.subscribe(() => {
        this.overlayModalService.close();
      });
    } else {
      this.router.navigate(['/home/utilisateurs']);
    }
  }
  

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible = !this.confirmPasswordVisible;
  }
}

// Validator personnalisé pour la confirmation du mot de passe
export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (password && confirmPassword && password.value !== confirmPassword.value) {
    confirmPassword.setErrors({ isMatching: true });
    return { isMatching: true };
  } else {
    if (confirmPassword?.hasError('isMatching')) {
      confirmPassword.setErrors(null);
    }
    return null;
  }
};
