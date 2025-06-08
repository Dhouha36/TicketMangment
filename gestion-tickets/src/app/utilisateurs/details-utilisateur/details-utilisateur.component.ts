import { Component, OnInit, Pipe } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AccountService } from '../../_services/account.service';
import { User } from '../../_models/user';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { PaginatedResult } from '../../_models/pagination';
import { Projet } from '../../_models/Projet';
import { Ticket } from '../../_models/ticket';
import { FormsModule } from '@angular/forms';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { PipesModule } from '../../_pipes/pipes.module';
import { Pays } from '../../_models/pays';
import { Societe } from '../../_models/societe';
import { PaysService } from '../../_services/pays.service';
import { SocieteService } from '../../_services/societe.service';
import { ToastrService } from 'ngx-toastr';
import { DefaultPipe } from '../../_pipes/default.pipe';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { ProjetService } from '../../_services/projet.service';
import { Role } from '../../_models/role.model';
import { RoleService } from '../../_services/role.service';
import { StatutDesTicket } from '../../_models/statut-des-ticket.model';
import { Priorite } from '../../_models/priorite.model';
import { StatusService } from '../../_services/status.service';
import { PrioriteService } from '../../_services/priorite.service';
import { MatDialog } from '@angular/material/dialog';
import { AttachProjectDialogComponent } from '../attach-project-dialog/attach-project-dialog.component';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { LoaderService } from '../../_services/loader.service';
import { GlobalLoaderService } from '../../_services/global-loader.service';
import { TypeContrat } from 'src/app/DTOs/type-contrat.enum';
import { ContratUserService } from 'src/app/_services/contrat-user.service';

@Component({
  selector: 'app-details-utilisateur',
  imports: [FormsModule, NgIf, NgFor, CommonModule, PipesModule, ReactiveFormsModule, DefaultPipe],
  templateUrl: './details-utilisateur.component.html',
  styleUrls: ['./details-utilisateur.component.scss']
})
export class DetailsUtilisateurComponent implements OnInit {
  private user: User | null = null;  // stocke les données utilisateur récupérées
  activeTab: string = 'projets';
  userForm!: FormGroup;
  contratForm!: FormGroup;

  // Projets
  displayedProjects: Projet[] = [];
  projectSearchTerm: string = '';
  projetPageNumber: number = 1;
  projetPageSize: number = 5;
  totalProjets: number = 0;

  // Tickets
  displayedTickets: Ticket[] = [];
  ticketSearchTerm: string = '';
  ticketPageNumber: number = 1;
  ticketPageSize: number = 5;
  totalTickets: number = 0;

  paysList: Pays[] = [];
  societesList: Societe[] = [];

  roles: Role[] = [];
  statuses: StatutDesTicket[] = [];
  priorities: Priorite[] = [];
  selectedCountry: Pays | undefined;

  isLoading: boolean = false;
  mode: 'view' | 'edit' = 'view';
  TypeContrat = TypeContrat;

  constructor(
    private paysService: PaysService,
    private societeService: SocieteService,
    private route: ActivatedRoute,
    private accountService: AccountService,
    private contratUserService: ContratUserService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private router: Router,
    private projetService: ProjetService,
    private roleService: RoleService,
    private statusService: StatusService,
    private prioriteService: PrioriteService,
    private dialog: MatDialog,
    private overlayModalService: OverlayModalService,
    private loaderService: LoaderService,
    private globalLoaderService: GlobalLoaderService 
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
   }

  // Getter pour exposer l'utilisateur dans le template sous le nom "userDetails"
  get userDetails(): User | null {
    return this.user;
  }

  private ticketSearchSubject = new Subject<string>();

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.mode = params['mode'] === 'edit' ? 'edit' : 'view';
    });
    // Initialisation des formulaires et chargement des listes
    this.initForm();
    this.initContratForm();
    this.loadPays();
    this.loadSocietes();
    this.loadRoles();
    this.loadStatuses();
    this.loadPriorities();
  
    this.userForm.get('role')?.valueChanges.subscribe(role => {
      const societeControl = this.userForm.get('societeId');
      if (role === 'Client') {
        societeControl?.enable();
      } else {
        societeControl?.disable();
      }
    });
    
    // Souscription aux changements du champ 'pays' du formulaire utilisateur
    this.userForm.get('pays')?.valueChanges.subscribe(value => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +value);
    });
  
    // Souscription à la recherche sur les tickets avec débounce
    this.ticketSearchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.ticketSearchTerm = searchTerm;
      this.ticketPageNumber = 1;
      this.loadTickets();
    });
  
    // Écouter les changements dans les paramètres de la route
    this.route.params.subscribe(params => {
      const userId = params['id'];
      if (userId) {
        // Charger les détails de l'utilisateur à chaque changement d'ID
        this.loadUserDetails(+userId);
      }
    });
  
    // Écouter les queryParams
    this.route.queryParams.subscribe(queryParams => {
      console.log("QueryParams mis à jour :", queryParams);
    });
  }
  


  openAttachProjectDialog(): void {
    const dialogRef = this.dialog.open(AttachProjectDialogComponent, {
      data: {}
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && this.user) {
        this.projetService.ajouterUtilisateurAuProjet(
          result.projetId,
          this.user.id,
          'Membre' // Valeur par défaut
        ).subscribe({
          next: () => {
            this.toastr.success('Projet attaché avec succès');
            this.loadProjects();
          },
          error: (err) => {
            console.error("Erreur lors de l'attachement du projet");
          }
        });
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
      error: (error) => {
        console.error("Erreur lors du chargement des rôles", error);
        this.toastr.error("Erreur lors du chargement des rôles.");
      }
    });
  }

  private loadStatuses(): void {
    this.statusService.getStatuses().subscribe({
      next: (statuses) => this.statuses = statuses,
      error: (err) => console.error('Erreur chargement statuts', err)
    });
  }

  private loadPriorities(): void {
    this.prioriteService.getPriorites().subscribe({
      next: (priorities) => this.priorities = priorities,
      error: (err) => console.error('Erreur chargement priorités', err)
    });
  }

  getStatusName(statusId: number): string {
    return this.statuses.find(s => s.id === statusId)?.name || 'Inconnu';
  }

  getPriorityName(priorityId: number): string {
    return this.priorities.find(p => p.id === priorityId)?.name || 'Inconnu';
  }

  initForm(): void {
    this.userForm = this.fb.group({
      id: [null],
      lastName: ['', Validators.required],
      firstName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      pays: ['', Validators.required],
      role: ['', Validators.required],
      societeId: [''],
      numTelephone: ['', [
        Validators.required,
        Validators.pattern(/^[0-9\s]+$/),
        Validators.minLength(8),
        Validators.maxLength(10)
      ]],
      actif: [false]
    });
  }



  initContratForm(): void {
    this.contratForm = this.fb.group({
      id: [0], // Ajoutez ce contrôle pour stocker l'identifiant du contrat
      dateDebut: ['', Validators.required],
      dateFin: ['', Validators.required],
      type: ['', Validators.required],
      salaireMensuel: [null]
    });
  }


  loadUserDetails(userId: number): void {
    this.globalLoaderService.showGlobalLoader();
    this.accountService.getUser(userId).subscribe({
      next: (user) => {
        this.user = user;
        // Traitement du numéro de téléphone et mise à jour du formulaire
        const codeTel = user.pays ? this.paysList.find(p => p.idPays === +user.pays)?.codeTel : '';
        let numeroLocal = user.numTelephone || '';
        if (codeTel && numeroLocal.startsWith(codeTel)) {
          numeroLocal = numeroLocal.substring(codeTel.length).trim();
        }
        this.selectedCountry = this.paysList.find(p => p.idPays === +user.pays);
        this.userForm.patchValue({
          id: user.id,
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          role: user.role,
          pays: user.pays,
          societeId: user.societe ? user.societe.id : null,
          numTelephone: numeroLocal,
          actif: user.actif
        });
        if (user.contratUser) {
          this.contratForm.patchValue({
            id:           user.contratUser.id,
            dateDebut:    new Date(user.contratUser.dateDebut + 'Z')
                             .toISOString().substring(0, 10),
            dateFin:      new Date(user.contratUser.dateFin  + 'Z')
                             .toISOString().substring(0, 10),
            type:         user.contratUser.type,
            salaireMensuel: user.contratUser.salaireMensuel
          });
        }        
        // Chargement des projets et tickets associés
        this.loadProjects();
        this.loadTickets();
      },
      error: (error) => {
        console.error('Erreur lors du chargement de l’utilisateur', error);
      },
      complete: () => {
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }
  



  // Mise à jour de l'utilisateur
  onSubmit(): void {
    // 1) Vérifier que le formulaire a été modifié
    if (!this.userForm.dirty) {
      this.toastr.warning('Veuillez modifier au moins un champ.');
      return;
    }

    // 2) Vérifier que le formulaire est valide
    if (this.userForm.invalid) {
      this.toastr.error('Veuillez corriger les erreurs du formulaire.');
      return;
    }

    // 3) Construire un FormData à partir des valeurs du formGroup
    const raw = this.userForm.getRawValue() as {
      id: number;
      lastName: string;
      firstName: string;
      email: string;
      pays: string;
      role: string;
      societeId: number | null;
      numTelephone: string;
      actif: boolean;
    };

    // Reconstituer le numéro international si vous stockez le préfixe (ex. "+216")
    let internationalNumber = raw.numTelephone;
    if (raw.pays) {
      const pays = this.paysList.find(p => p.idPays === +raw.pays);
      if (pays && pays.codeTel) {
        // Si l’utilisateur a saisi "50 123 456", on préfixe avec "+216 "
        if (!internationalNumber.startsWith(pays.codeTel)) {
          internationalNumber = `${pays.codeTel} ${internationalNumber}`;
        }
      }
    }

    // Construire le FormData (même s’il n’y a pas de photo)
    const formData = new FormData();
    formData.append('Id', raw.id.toString());
    formData.append('FirstName', raw.firstName);
    formData.append('LastName', raw.lastName);
    formData.append('Email', raw.email);
    formData.append('Pays', raw.pays);   
    formData.append('Role', raw.role);
    formData.append('SocieteId', raw.societeId ? raw.societeId.toString() : '');
    formData.append('NumTelephone', internationalNumber);
    formData.append('Actif', raw.actif.toString());

    // 4) Afficher le loader global
    this.loaderService.showLoader();

    // 5) Appeler la méthode avec (id, formData)
    this.accountService.updateUser(raw.id, formData).subscribe({
      next: (updatedUser: User) => {
        // 6) Réinjecter l’ancien token si besoin
        const stored = localStorage.getItem('user');
        if (stored) {
          try {
            const current = JSON.parse(stored) as User;
            if (current.token) {
              (updatedUser as any).token = current.token;
            }
          } catch {}
        }

        // 7) Mettre à jour le signal et le localStorage
        this.accountService.setCurrentUser(updatedUser);
        localStorage.setItem('user', JSON.stringify(updatedUser));

        // 8) Mettre à jour la variable locale 'user' et la réafficher dans le formulaire
        this.user = updatedUser;
        this.userForm.reset();   // On reset pour désactiver le dirty
        this.userForm.patchValue({
          id: updatedUser.id,
          firstName: updatedUser.firstName,
          lastName: updatedUser.lastName,
          email: updatedUser.email,
          role: updatedUser.role,
          pays: updatedUser.pays,
          societeId: updatedUser.societe ? updatedUser.societe.id : null,
          numTelephone: updatedUser.numTelephone,
          actif: updatedUser.actif
        });

        this.toastr.success('Mise à jour effectuée avec succès.');
        this.loaderService.hideLoader();
      },
      error: (err) => {
        console.error('Erreur lors de la mise à jour', err);
        if (err.status === 400) {
          this.toastr.error('Requête invalide, veuillez vérifier les champs.');
        } else if (err.status === 404) {
          this.toastr.error("Utilisateur non trouvé.");
        } else {
          this.toastr.error("Erreur lors de la mise à jour de l'utilisateur.");
        }
        this.loaderService.hideLoader();
      }
    });
  }  

  onCancel(): void {
    // Réinitialiser le formulaire avec les valeurs initiales si nécessaire
    if (this.user) {
      this.userForm.patchValue({
        lastName: this.user.lastName,
        firstName: this.user.firstName,
        email: this.user.email,
        pays: this.user.pays,
        role: this.user.role,
        societe: this.user.societeId,
        numTelephone: this.user.numTelephone,
        actif: this.user.actif
        // Les champs de mot de passe restent vides
      });
    }
  }


  // Méthodes pour le contrat

  onSubmitContrat(): void {
    // Vérifier que l'utilisateur est chargé et qu'il a un contrat
    if (!this.user) {
      this.toastr.error("Aucun utilisateur n'est chargé.");
      return;
    }
    if (!this.user.contratUser) {
      this.toastr.error("Aucun contrat trouvé pour cet utilisateur.");
      return;
    }
  
    // Vérifier si au moins un champ du contrat a été modifié
    if (!this.contratForm.dirty) {
      this.toastr.warning("Veuillez modifier au moins un champ du contrat.");
      return;
    }
  
    // Vérifier que le formulaire est valide
    if (this.contratForm.invalid) {
      this.toastr.error("Veuillez corriger les erreurs du formulaire de contrat.");
      return;
    }
    
    const originalContrat = this.user.contratUser;
    const contratToUpdate = {
      ...originalContrat,
      ...this.contratForm.value
    };
  
    // Affiche le loader
    this.loaderService.showLoader();
  
    this.contratUserService.updateContrat(contratToUpdate).subscribe({
      next: () => {
        this.toastr.success("Contrat mis à jour avec succès.");
        if (this.user) {
          this.user.contratUser = contratToUpdate;
        }
        this.loaderService.hideLoader();
      },
      error: (error) => {
        console.error("Erreur lors de la mise à jour du contrat", error);
        this.toastr.error("Erreur lors de la mise à jour du contrat.");
        this.loaderService.hideLoader();
      }
    });
  }  



  cancelContrat(): void {
    if (this.user && this.user.contratUser) {
      this.contratForm.patchValue({
        dateDebut: this.user.contratUser.dateDebut,
        dateFin: this.user.contratUser.dateFin,
      });
    }
  }

  initializeContratForm(): void {
    this.initContratForm();
  }

  // Chargement des projets associés à l'utilisateur
  loadProjects(): void {
    if (!this.user) return;

    this.accountService
      .getUserProjects(this.user.id, this.projetPageNumber, this.projetPageSize, this.projectSearchTerm)
      .subscribe((res: PaginatedResult<Projet[]>) => {
        this.displayedProjects = res.items || [];
        this.totalProjets = res.pagination?.totalItems || 0;
      });
  }

  // Chargement des tickets associés à l'utilisateur
  loadTickets(): void {
    if (!this.user) return;

    this.accountService
      .getUserTickets(this.user.id, this.ticketPageNumber, this.ticketPageSize, this.ticketSearchTerm)
      .subscribe((res: PaginatedResult<Ticket[]>) => {
        this.displayedTickets = res.items || [];
        this.totalTickets = res.pagination?.totalItems || 0;
      });
  }

  getProjectRole(projectId: number): string {
    const member = this.user?.projetMembers?.find(m => m.projetId === projectId);
    return member ? member.role : 'Non défini';
  }

  switchTab(tab: string): void {
    this.activeTab = tab;
    if (tab === 'projets') {
      this.loadProjects();
    } else if (tab === 'tickets') {
      this.loadTickets();
    }
  }

  onProjectSearch(): void {
    this.projetPageNumber = 1;
    this.loadProjects();
  }

  onTicketSearch(): void {
    this.ticketPageNumber = 1;
    this.loadTickets();
  }


  viewProjet(projectId: number): void {
    // Redirection vers la page de détails du projet
    this.router.navigate(['home/Projets/details/', projectId]);
  }


  onDeleteUserFromProject(projetId: number): void {
    if (!this.user) return; // Vérification si l'utilisateur est chargé

    const firstName = this.user.firstName;
    const lastName = this.user.lastName;
    const confirmationMessage = `Êtes-vous sûr de vouloir supprimer "${firstName} ${lastName}" de ce projet ?`;

    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = confirmationMessage;

    modalInstance.confirmed.subscribe(() => {
      this.projetService.supprimerUtilisateurDuProjet(projetId, this.user!.id).subscribe({
        next: () => {
          this.toastr.success(`${firstName} ${lastName} a été retiré du projet.`);
          this.loadProjects(); // Recharge la liste des projets
        },
        error: (error) => {
          console.error('Erreur lors de la suppression du projet', error);
        }
      });
      this.overlayModalService.close();
    });

    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }



}

