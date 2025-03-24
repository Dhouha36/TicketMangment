import { Component, OnInit, OnDestroy, HostListener, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { NgxEditorModule, Editor, Toolbar } from 'ngx-editor';

import { TicketService } from '../../_services/ticket.service';
import { CategorieProblemeService } from '../../_services/categorie-probleme.service';
import { ProjetService } from '../../_services/projet.service';
import { AccountService } from '../../_services/account.service';
import { PrioriteService } from '../../_services/priorite.service';
import { QualificationService } from '../../_services/qualification.service';
import { Qualification } from '../../_models/qualification.model';
import { Priorite } from '../../_models/priorite.model';
import { finalize, forkJoin } from 'rxjs';
import { LoaderService } from '../../_services/loader.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../../_services/overlay-modal.service';

@Component({
  selector: 'app-ajouter-ticket',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgxEditorModule, FormsModule],
  templateUrl: './ajouter-ticket.component.html',
  styleUrls: ['./ajouter-ticket.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class AjouterTicketComponent implements OnInit, OnDestroy {
  addTicketForm: FormGroup;

  // Listes chargées depuis la base
  categories: any[] = [];
  projets: any[] = [];
  usersOptions: any[] = []; // Liste des utilisateurs (responsables)

  // Options pour les selects standards
  qualificationOptions: Qualification[] = [];
  prioriteOptions: Priorite[] = [];

  // Variables pour le select custom Qualification
  selectedQualification: Qualification | null = null;
  isQualificationDropdownOpen: boolean = false;
  qualificationSearchTerm: string = '';
  filteredQualifications: Qualification[] = [];

  // Variables pour le select custom Priorité
  selectedPriority: Priorite | null = null;
  isPriorityDropdownOpen: boolean = false;
  prioritySearchTerm: string = '';
  filteredPriorities: Priorite[] = [];

  // Variables pour le dropdown Projet
  selectedProjet: any = null;
  isProjetDropdownOpen: boolean = false;
  searchProjet: string = '';
  filteredProjets: any[] = [];

  // Variables pour le dropdown Catégorie
  selectedCategorie: any = null;
  isCategorieDropdownOpen: boolean = false;
  searchCategorie: string = '';
  filteredCategories: any[] = [];

  selectedFile: File | null = null;
  formSubmitted = false;

  // Instance de l'éditeur et configuration de la toolbar pour ngx-editor
  editor!: Editor;
  toolbar!: Toolbar;

  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private ticketService: TicketService,
    private categorieProblemeService: CategorieProblemeService,
    private projetService: ProjetService,
    private accountService: AccountService,
    private prioriteService: PrioriteService,
    private qualificationService: QualificationService,
    private toastr: ToastrService,
    private router: Router,
    private loaderService: LoaderService,
    private overlayModalService: OverlayModalService,
  ) {

    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
    // Création du formulaire réactif avec les clés du modèle
    this.addTicketForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      qualificationId: ['', Validators.required],
      priorityId: ['', Validators.required],
      problemCategoryId: [null, Validators.required],
      projetId: [null, Validators.required],
      attachment: [null],
    });
  }

  ngOnInit(): void {
    forkJoin([
      this.loadCategories(),
      this.loadProjets(),
      this.loadPriorites(),
      this.loadQualifications(),
      this.loadUsers()
    ]).subscribe();

    // Initialisation de ngx-editor
    this.editor = new Editor();

    // Configuration de la toolbar (personnalisez-la selon vos besoins)
    this.toolbar = [
      ['bold', 'italic', 'underline', 'strike'],
      ['link'],
      ['code', 'blockquote'],
      ['bullet_list', 'ordered_list'],
      ['undo'],
      ['redo']
    ];
  }

  ngOnDestroy(): void {
    // Détruire l'éditeur pour libérer les ressources
    this.editor.destroy();
  }

  // Méthode pour charger la liste des catégories depuis la base
  loadCategories(): void {
    this.categorieProblemeService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.filteredCategories = [...this.categories];
      },
      error: (err) => {
        console.error("Erreur lors du chargement des catégories", err);
        this.toastr.error("Erreur lors du chargement des catégories de problèmes.");
      }
    });
  }

  // Méthode pour charger la liste des projets depuis la base
  loadProjets(): void {
    this.projetService.getUserProjets().subscribe({
      next: (projets) => {
        this.projets = projets;
        this.filteredProjets = [...this.projets];
      },
      error: (err) => {
        console.error("Erreur lors du chargement des projets", err);
        this.toastr.error("Erreur lors du chargement des projets.");
      }
    });
  }

  // Méthode pour charger les priorités depuis la base
  loadPriorites(): void {
    this.prioriteService.getPriorites().subscribe({
      next: (priorites) => {
        this.prioriteOptions = priorites;
        this.filteredPriorities = [...priorites];
      },
      error: (err) => {
        console.error("Erreur lors du chargement des priorités", err);
        this.toastr.error("Erreur lors du chargement des priorités.");
      }
    });
  }

  // Méthode pour charger les qualifications depuis la base
  loadQualifications(): void {
    this.qualificationService.getQualifications().subscribe({
      next: (qualifications) => {
        this.qualificationOptions = qualifications;
        this.filteredQualifications = [...qualifications];
      },
      error: (err) => {
        console.error("Erreur lors du chargement des qualifications", err);
        this.toastr.error("Erreur lors du chargement des qualifications.");
      }
    });
  }

  // Méthode pour charger la liste des utilisateurs (responsables)
  loadUsers(): void {
    this.accountService.getUsers(1, 1000).subscribe({
      next: (result) => {
        this.usersOptions = result.items ?? [];
      },
      error: (err) => {
        console.error("Erreur lors du chargement des utilisateurs", err);
      }
    });
  }

  // Gestion de l'ouverture/fermeture des dropdowns pour "projet", "catégorie", "qualification" et "priority"
  toggleDropdown(type: string): void {
    if (type === 'projet') {
      this.isProjetDropdownOpen = !this.isProjetDropdownOpen;
      if (this.isProjetDropdownOpen) {
        this.filteredProjets = [...this.projets];
        this.searchProjet = '';
      }
    } else if (type === 'categorie') {
      this.isCategorieDropdownOpen = !this.isCategorieDropdownOpen;
      if (this.isCategorieDropdownOpen) {
        this.filteredCategories = [...this.categories];
        this.searchCategorie = '';
      }
    } else if (type === 'qualification') {
      this.isQualificationDropdownOpen = !this.isQualificationDropdownOpen;
      if (this.isQualificationDropdownOpen) {
        this.filteredQualifications = [...this.qualificationOptions];
        this.qualificationSearchTerm = '';
      }
    } else if (type === 'priority') {
      this.isPriorityDropdownOpen = !this.isPriorityDropdownOpen;
      if (this.isPriorityDropdownOpen) {
        this.filteredPriorities = [...this.prioriteOptions];
        this.prioritySearchTerm = '';
      }
    }
  }

  // Filtrer les projets en fonction de la saisie
  filterProjets(): void {
    this.filteredProjets = this.projets.filter(p =>
      p.nom.toLowerCase().includes(this.searchProjet.toLowerCase())
    );
  }

  // Lorsque l'utilisateur sélectionne un projet
  selectProjet(projet: any): void {
    this.selectedProjet = projet;
    this.isProjetDropdownOpen = false;
    this.addTicketForm.patchValue({ projetId: projet.id });
  }

  // Filtrer les catégories en fonction de la saisie
  filterCategories(): void {
    this.filteredCategories = this.categories.filter(cat =>
      cat.nom.toLowerCase().includes(this.searchCategorie.toLowerCase())
    );
  }

  // Lorsque l'utilisateur sélectionne une catégorie
  selectCategorie(categorie: any): void {
    this.selectedCategorie = categorie;
    this.isCategorieDropdownOpen = false;
    this.addTicketForm.patchValue({ problemCategoryId: categorie.id });
  }

  // Filtrer les qualifications en fonction de la saisie
  filterQualifications(): void {
    this.filteredQualifications = this.qualificationOptions.filter(q =>
      q.name.toLowerCase().includes(this.qualificationSearchTerm.toLowerCase())
    );
  }

  // Lorsque l'utilisateur sélectionne une qualification
  selectQualification(qualification: Qualification): void {
    this.selectedQualification = qualification;
    this.isQualificationDropdownOpen = false;
    this.addTicketForm.patchValue({ qualificationId: qualification.id });
  }

  // Filtrer les priorités en fonction de la saisie
  filterPriorities(): void {
    this.filteredPriorities = this.prioriteOptions.filter(p =>
      p.name.toLowerCase().includes(this.prioritySearchTerm.toLowerCase())
    );
  }

  // Lorsque l'utilisateur sélectionne une priorité
  selectPriority(priority: Priorite): void {
    this.selectedPriority = priority;
    this.isPriorityDropdownOpen = false;
    this.addTicketForm.patchValue({ priorityId: priority.id });
  }

  // Gestion de la sélection d'un fichier pour l'attachement
  onFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedFile = event.target.files[0];
      this.addTicketForm.patchValue({ attachment: this.selectedFile });
    }
  }

  // Soumission du formulaire pour créer le ticket
  onSubmit(): void {
    this.formSubmitted = true;
    if (this.addTicketForm.invalid || !this.selectedQualification || !this.selectedPriority) {
      Object.values(this.addTicketForm.controls).forEach(control => control.markAsTouched());
      return;
    }

    const formData = new FormData();
    formData.append('title', this.addTicketForm.get('title')?.value);
    const rawDescription = this.addTicketForm.get('description')?.value;
    const cleanedDescription = rawDescription.replace(/<\/?p>/g, '');
    formData.append('description', cleanedDescription);
    formData.append('qualificationId', Number(this.addTicketForm.get('qualificationId')?.value).toString());
    formData.append('priorityId', Number(this.addTicketForm.get('priorityId')?.value).toString());
    formData.append('problemCategoryId', Number(this.addTicketForm.get('problemCategoryId')?.value).toString());
    formData.append('projetId', Number(this.addTicketForm.get('projetId')?.value).toString());

    const currentUser = this.accountService.currentUser();
    if (currentUser) {
      formData.append('ownerId', currentUser.id.toString());
    }
    if (this.selectedFile) {
      formData.append('attachment', this.selectedFile, this.selectedFile.name);
    }

    // Active le loader local
    this.loaderService.showLoader();
    this.ticketService.createTicket(formData)
      .pipe(finalize(() => this.loaderService.hideLoader()))
      .subscribe({
        next: (ticket) => {
          this.toastr.success("Ticket créé avec succès.");
          this.router.navigate(['/home/Tickets/details', ticket.id]);
        },
        error: (error) => {
          console.error('Erreur lors de la création du ticket', error);
          let errMsg = "Erreur lors de la création du ticket.";
          if (Array.isArray(error)) {
            errMsg = error.join(' ');
          } else if (typeof error === 'string') {
            errMsg = error;
          } else if (error.error) {
            if (Array.isArray(error.error)) {
              errMsg = error.error.join(' ');
            } else if (typeof error.error === 'string') {
              errMsg = error.error;
            } else if (typeof error.error === 'object') {
              errMsg = error.error.message || JSON.stringify(error.error);
            }
          } else if (error.message) {
            errMsg = error.message;
          }
          this.toastr.error(errMsg);
        }
      });
  }

  // Créer le ticket et réinitialiser le formulaire pour en ajouter un autre
  createAndReset(): void {
    if (this.addTicketForm.invalid || !this.selectedQualification || !this.selectedPriority) {
      this.toastr.warning("Veuillez remplir correctement le formulaire.");
      return;
    }

    const formData = new FormData();
    formData.append('title', this.addTicketForm.get('title')?.value);
    const rawDescription = this.addTicketForm.get('description')?.value;
    const cleanedDescription = rawDescription.replace(/<\/?p>/g, '');
    formData.append('description', cleanedDescription);
    formData.append('qualificationId', Number(this.addTicketForm.get('qualificationId')?.value).toString());
    formData.append('priorityId', Number(this.addTicketForm.get('priorityId')?.value).toString());
    formData.append('problemCategoryId', Number(this.addTicketForm.get('problemCategoryId')?.value).toString());
    formData.append('projetId', Number(this.addTicketForm.get('projetId')?.value).toString());

    const currentUser = this.accountService.currentUser();
    if (currentUser) {
      formData.append('ownerId', currentUser.id.toString());
    }
    if (this.selectedFile) {
      formData.append('attachment', this.selectedFile, this.selectedFile.name);
    }

    this.loaderService.showLoader();
    this.ticketService.createTicket(formData)
      .pipe(finalize(() => this.loaderService.hideLoader()))
      .subscribe({
        next: (ticket) => {
          this.toastr.success("Ticket créé avec succès. Vous pouvez en ajouter un autre.");
          this.resetForm();
        },
        error: (error) => {
          console.error('Erreur lors de la création du ticket', error);
          let errMsg = "Erreur lors de la création du ticket.";
          if (Array.isArray(error)) {
            errMsg = error.join(' ');
          } else if (typeof error === 'string') {
            errMsg = error;
          } else if (error.error) {
            if (Array.isArray(error.error)) {
              errMsg = error.error.join(' ');
            } else if (typeof error.error === 'string') {
              errMsg = error.error;
            } else if (typeof error.error === 'object') {
              errMsg = error.error.message || JSON.stringify(error.error);
            }
          } else if (error.message) {
            errMsg = error.message;
          }
          this.toastr.error(errMsg);
        }
      });
  }
  
  // Réinitialisation du formulaire
  resetForm(): void {
    this.addTicketForm.reset();
    this.selectedFile = null;
    this.selectedProjet = null;
    this.selectedCategorie = null;
    this.selectedQualification = null;
    this.selectedPriority = null;
  }



  // Annulation : redirige selon si des modifications ont été effectuées
  cancel(): void {
    if (this.addTicketForm.dirty) {
      const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
      modalInstance.message = "Vous avez des modifications non sauvegardées. Voulez-vous vraiment annuler ?";
      
      modalInstance.confirmed.subscribe(() => {
        this.router.navigate(['/home/Tickets']);
        this.overlayModalService.close();
      });
      
      modalInstance.cancelled.subscribe(() => {
        this.overlayModalService.close();
      });
    } else {
      this.router.navigate(['/home/Tickets']);
    }
  }
  

  // Gestionnaire de clic global pour fermer les dropdowns si le clic se fait en dehors
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.custom-select')) {
      this.isProjetDropdownOpen = false;
      this.isCategorieDropdownOpen = false;
      this.isQualificationDropdownOpen = false;
      this.isPriorityDropdownOpen = false;
    }
  }
}
