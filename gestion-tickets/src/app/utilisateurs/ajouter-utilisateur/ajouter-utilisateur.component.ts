import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
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
import { User } from 'src/app/_models/user';

@Component({
  selector: 'app-ajouter-utilisateur',
  imports: [CommonModule, ReactiveFormsModule, MatTooltipModule],
  templateUrl: './ajouter-utilisateur.component.html',
  styleUrls: ['./ajouter-utilisateur.component.scss']
})
export class AjouterUtilisateurComponent implements OnInit {
  @Input() societeId!: number;
  @Input() fixedRole!: string;
  @Output() userCreated = new EventEmitter<User>();
  registerForm: FormGroup;
  roles: Role[] = [];
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
      numTelephone: ['', [
        Validators.required,
        Validators.pattern(/^[0-9\s]+$/),
        Validators.minLength(8),
        Validators.maxLength(10)
      ]],
      actif: [false],
      contrat: [false],
      contract: this.fb.group({
        dateDebut: ['', Validators.required],
        dateFin: ['', Validators.required],
        type: [null, Validators.required]
      })
    });
  }

  ngOnInit(): void {
    // Chargement des listes
    this.loadPays();
    this.loadSocietes();
    this.loadRoles();

    this.registerForm.get('pays')?.valueChanges.subscribe(value => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +value);
    });


    // Initialisation du groupe 'contract' selon la case 'contrat'
    const contratControl = this.registerForm.get('contrat');
    const contractGroup = this.registerForm.get('contract');

    if (!contratControl?.value) {
      contractGroup?.disable();
      contractGroup?.get('dateDebut')?.clearValidators();
      contractGroup?.get('dateFin')?.clearValidators();
      contractGroup?.get('type')?.clearValidators();
      contractGroup?.updateValueAndValidity();
    }

    // Surveillance de la case 'contrat'
    contratControl?.valueChanges.subscribe(isChecked => {
      if (isChecked) {
        contractGroup?.enable();
        contractGroup?.get('dateDebut')?.setValidators(Validators.required);
        contractGroup?.get('dateFin')?.setValidators(Validators.required);
        contractGroup?.get('type')?.setValidators(Validators.required);
        contractGroup?.get('dateDebut')?.updateValueAndValidity();
        contractGroup?.get('dateFin')?.updateValueAndValidity();
        contractGroup?.get('type')?.updateValueAndValidity();
      } else {
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

  onContractChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.checked) {
      this.openContractDialog();
    }
  }

  openContractDialog(): void {
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      data: {
        contractForm: this.registerForm.get('contract'),
        isProject: false
      }
    });
  
    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        this.registerForm.get('contrat')?.setValue(false);
      }
    });
  }
  

  register(): void {
    if (this.registerForm.valid) {
      const formValue = this.registerForm.value;

      const phoneNumber = formValue.numTelephone;
      const fullPhoneNumber = this.selectedCountry
        ? this.selectedCountry.codeTel + ' ' + phoneNumber
        : phoneNumber;

      const userForRegister: any = {
        email: formValue.email,
        role: formValue.role.charAt(0).toUpperCase() + formValue.role.slice(1),
        firstname: formValue.firstName,
        lastname: formValue.lastName,
        numtelephone: fullPhoneNumber,
        pays: +formValue.pays,
        actif: formValue.actif
      };

      if (formValue.contrat) {
        userForRegister.contract = {
          dateDebut: formValue.contract.dateDebut,
          dateFin: formValue.contract.dateFin,
          type: formValue.contract.type
        };
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
          const errorMessage = err.error?.message || err.message || "Erreur lors de l'enregistrement de l'utilisateur.";
          this.toastr.error(errorMessage);
          this.loaderService.hideLoader();
        }
      });
    } else {
      this.toastr.warning("Veuillez remplir correctement le formulaire.");
    }
  }

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
}
