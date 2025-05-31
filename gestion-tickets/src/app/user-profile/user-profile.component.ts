import { Photo } from './../_models/photo';
import { ClientService } from './../_services/client.service';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
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
  hoveringAvatar = false;
  avatarUrl: string | null = null;

  userDetails: User | ClientDto | null = null;
  userForm!: FormGroup;
  passwordVisible: boolean = false;
  confirmPasswordVisible: boolean = false;
  paysList: any[] = [];
  selectedCountry: any = null;
  isLoading: boolean = false;

  selectedPhotoFile: File | null = null;
  photoPreviewUrl: string | null = null;

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
      firstName: [{ value: '', disabled: true }, Validators.required],
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
      // --- ON AJOUTE LE FormControl photoFile POUR QUE LE FORMULAIRE SOIT DIRTY LORSQU’ON SÉLECTIONNE UNE IMAGE ---
      photoFile: [null]
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
  
    // 1) On lit l’objet en session pour récupérer seulement l’ID
    const stored = this.accountService.currentUser();
    if (!stored) {
      this.globalLoaderService.hideGlobalLoader();
      return;
    }
  
    const userId = stored.id;
    // 2) Si le champ `role` est undefined, c’est un ClientDto
    const isClientStored = (stored as any).role === undefined;
  
    if (isClientStored) {
      // → on récupère un client
      this.clientService.getById(userId).subscribe({
        next: (freshClient: ClientDto) => {
          // 3a) On met à jour le signal + localStorage
          this.accountService.setCurrentUser(freshClient);
  
          // 4a) On met à jour le composant
          this.userDetails = freshClient;
          this.avatarUrl   = (freshClient as any).photoUrl || null;
  
          // 5a) On cherche la ligne “pays” correspondante
          const countryC = this.paysList.find(p =>
            p.idPays === Number(freshClient.paysId)
          );
          this.selectedCountry = countryC || null;
  
          // 6a) On patche le formulaire client en enlevant le préfixe du numéro
          //    (si vous voulez garder “+216” masqué dans la saisie)
          let localNumberC = freshClient.numTelephone || '';
          if (countryC?.codeTel && localNumberC.startsWith(countryC.codeTel)) {
            localNumberC = localNumberC.substring(countryC.codeTel.length).trim();
          }
          this.userForm.patchValue({
            id: freshClient.id,
            lastName: freshClient.lastName,
            firstName: freshClient.firstName,
            email: freshClient.email,
            numTelephone: localNumberC,
            actif: freshClient.actif,
            paysId: freshClient.paysId,
            societeId: freshClient.societeId,
            societe: freshClient.societe.nom
          });
  
          // Si la photo existe
          if ((freshClient as any).photoUrl) {
            this.photoPreviewUrl = (freshClient as any).photoUrl;
            this.avatarUrl       = (freshClient as any).photoUrl;
          }
  
          this.userForm.get('lastName')!.disable();
          this.userForm.get('firstName')!.disable();
  
          this.globalLoaderService.hideGlobalLoader();
        },
        error: (err) => {
          console.error('Erreur lors de la récupération du client', err);
          this.globalLoaderService.hideGlobalLoader();
        }
      });
    } else {
      // → on récupère un utilisateur interne (User)
      this.accountService.getUser(userId).subscribe({
        next: (freshUser: User) => {
          // 3b) On met à jour le signal + localStorage
          this.accountService.setCurrentUser(freshUser);
  
          // 4b) On met à jour le composant
          this.userDetails = freshUser;
          this.avatarUrl   = (freshUser as any).photoUrl || null;
  
          // 5b) On cherche la ligne “pays” correspondante
          const countryU = this.paysList.find(p =>
            p.idPays === Number((freshUser as any).pays)
          );
          this.selectedCountry = countryU || null;
  
          // 6b) On patche le formulaire User sans le préfixe
          let localNumberU = freshUser.numTelephone || '';
          if (countryU?.codeTel && localNumberU.startsWith(countryU.codeTel)) {
            localNumberU = localNumberU.substring(countryU.codeTel.length).trim();
          }
          this.userForm.patchValue({
            id: freshUser.id,
            lastName: freshUser.lastName,
            firstName: freshUser.firstName,
            email: freshUser.email,
            numTelephone: localNumberU,
            actif: freshUser.actif,
            pays: freshUser.pays,
            role: freshUser.role
          });
  
          if ((freshUser as any).photoUrl) {
            this.photoPreviewUrl = (freshUser as any).photoUrl;
            this.avatarUrl       = (freshUser as any).photoUrl;
          }
  
          this.userForm.get('lastName')!.disable();
          this.userForm.get('firstName')!.disable();
  
          this.globalLoaderService.hideGlobalLoader();
        },
        error: (err) => {
          console.error('Erreur lors de la récupération de l’utilisateur', err);
          this.globalLoaderService.hideGlobalLoader();
        }
      });
    }
  }  
  
  private patchFormWithUser(user: User): void {
    this.userForm.patchValue({
      id: user.id,
      lastName: user.lastName,
      firstName: user.firstName,
      email: user.email,
      numTelephone: user.numTelephone,
      actif: user.actif,
      pays: user.pays,
      role: user.role
    });
  
    if ((user as any).photoUrl) {
      this.photoPreviewUrl = (user as any).photoUrl;
      this.avatarUrl       = (user as any).photoUrl;
    }
  
    // Verrouiller les champs firstName / lastName
    this.userForm.get('lastName')!.disable();
    this.userForm.get('firstName')!.disable();
  }

  private patchFormWithClient(client: ClientDto): void {
    this.userForm.patchValue({
      id: client.id,
      lastName: client.lastName,
      firstName: client.firstName,
      email: client.email,
      numTelephone: client.numTelephone,
      actif: client.actif,
      // Pour les clients, on n’affiche pas “role” (il n’existe pas dans ClientDto)
      paysId: client.paysId,
      societeId: client.societeId,
      societe: client.societe.nom
    });
  
    // Si l’API a renvoyé un photoUrl, on l’affiche également
    if ((client as any).photoUrl) {
      this.photoPreviewUrl = (client as any).photoUrl;
      this.avatarUrl       = (client as any).photoUrl;
    }
  
    // Les champs prénom/nom restent en lecture seule
    this.userForm.get('lastName')!.disable();
    this.userForm.get('firstName')!.disable();
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
    const formData = new FormData();
    formData.append('Id', raw.id);
    formData.append('Email', raw.email);
    formData.append('FirstName', raw.firstName);
    formData.append('LastName', raw.lastName);
    formData.append('NumTelephone', raw.numTelephone);
    formData.append('Actif', raw.actif.toString());
    formData.append('PaysId', raw.paysId);
    formData.append('SocieteId', raw.societeId);
  
    if (raw.nouveauPassword) {
      formData.append('NouveauPassword', raw.nouveauPassword);
      formData.append('ConfirmNouveauPassword', raw.confirmNouveauPassword);
    }
    if (this.selectedPhotoFile) {
      formData.append('PhotoFile', this.selectedPhotoFile, this.selectedPhotoFile.name);
    }
  
    this.clientService.update(raw.id, formData).subscribe({
      next: (updatedClient: ClientDto) => {
        // 1) Conserver l’ancien token s’il y en avait un
        const stored = localStorage.getItem('user');
        if (stored) {
          try {
            const current = JSON.parse(stored) as ClientDto;
            if (current.token) {
              (updatedClient as any).token = current.token;
            }
          } catch {}
        }
  
        // 2) Mettre à jour le signal dans AccountService
        this.accountService.setCurrentUser(updatedClient);
  
        // 3) Mettre à jour le localStorage avec le client actualisé
        localStorage.setItem('user', JSON.stringify(updatedClient));
  
        // 4) Mise à jour immédiate de la vue
        this.userDetails = updatedClient;
        this.avatarUrl   = (updatedClient as any).photoUrl || null;
        this.patchFormWithClient(updatedClient);
  
        this.toastr.success("Compte client mis à jour avec succès.");
        this.loaderService.hideLoader();
      },
      error: (err) => {
        if (err.status === 400) {
          this.toastr.error("Requête invalide, veuillez vérifier les champs.");
        } else if (err.status === 404) {
          this.toastr.error("Client non trouvé.");
        } else {
          this.toastr.error("Erreur lors de la mise à jour du client.");
        }
        this.loaderService.hideLoader();
      }
    });
  }
   

  private submitUser(): void {
    const raw = this.userForm.getRawValue();
    const formData = new FormData();
    formData.append('Id', raw.id);
    formData.append('Email', raw.email);
    formData.append('FirstName', raw.firstName);
    formData.append('LastName', raw.lastName);
    formData.append('NumTelephone', raw.numTelephone);
    formData.append('Actif', raw.actif.toString());
    formData.append('Pays', raw.pays);
    formData.append('Role', raw.role);
  
    if (raw.nouveauPassword) {
      formData.append('NouveauPassword', raw.nouveauPassword);
      formData.append('ConfirmNouveauPassword', raw.confirmNouveauPassword);
    }
    if (this.selectedPhotoFile) {
      formData.append('PhotoFile', this.selectedPhotoFile, this.selectedPhotoFile.name);
    }
  
    this.accountService.updateUser(raw.id, formData).subscribe({
      next: (updated: User) => {
        // 1) Conserver l’ancien token s’il y en avait un
        const stored = localStorage.getItem('user');
        if (stored) {
          try {
            const current = JSON.parse(stored);
            if (current.token) {
              (updated as any).token = current.token;
            }
          } catch {}
        }
  
        // 2) Mettre à jour le signal dans AccountService
        this.accountService.setCurrentUser(updated);
  
        // 3) Mettre à jour le localStorage avec l’utilisateur actualisé
        localStorage.setItem('user', JSON.stringify(updated));
  
        // 4) Mise à jour immédiate de la vue
        this.userDetails = updated;
        this.avatarUrl   = (updated as any).photoUrl || null;
        this.patchFormWithUser(updated);
  
        this.toastr.success('Compte mis à jour avec succès.');
        this.loaderService.hideLoader();
      },
      error: (err) => {
        if (err.status === 400) {
          this.toastr.error('Requête invalide, veuillez vérifier les champs.');
        } else if (err.status === 404) {
          this.toastr.error('Utilisateur non trouvé.');
        } else {
          this.toastr.error('Erreur lors de la mise à jour de l’utilisateur.');
        }
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
        // REMETTRE l'aperçu de la photo
        photoFile: null
      });
      this.photoPreviewUrl = (c as any).photoUrl || null;
      this.avatarUrl = (c as any).photoUrl || null;
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
        photoFile: null
      });
      this.photoPreviewUrl = (u as any).photoUrl || null;
      this.avatarUrl = (u as any).photoUrl || null;
    }

    this.userForm.get('photoFile')!.reset();
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
      return (this.userDetails as User).role;
    }
    return '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }
    const file = input.files[0];
    this.selectedPhotoFile = file;

    // Générer un aperçu de la photo en DataURL
    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.photoPreviewUrl = e.target.result;
      this.avatarUrl = e.target.result; // pour mettre à jour l’avatar immédiatement
    };
    reader.readAsDataURL(file);

    // Mettre à jour le FormControl 'photoFile' (pour que le FormGroup soit marqué dirty)
    this.userForm.patchValue({ photoFile: file });
    this.userForm.get('photoFile')!.markAsDirty();
  }

}