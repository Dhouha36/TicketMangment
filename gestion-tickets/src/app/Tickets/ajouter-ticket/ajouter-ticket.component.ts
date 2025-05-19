import { Component, OnInit, OnDestroy, HostListener, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { NgxEditorModule, Editor, Toolbar } from 'ngx-editor';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';

import { TicketService } from '../../_services/ticket.service';
import { CategorieProblemeService } from '../../_services/categorie-probleme.service';
import { ProjetService } from '../../_services/projet.service';
import { AccountService } from '../../_services/account.service';
import { PrioriteService } from '../../_services/priorite.service';
import { QualificationService } from '../../_services/qualification.service';
import { Qualification } from '../../_models/qualification.model';
import { Priorite } from '../../_models/priorite.model';
import { LoaderService } from '../../_services/loader.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { TicketCreateDto } from '../../_models/ticketCreateDto';
import { ClientService } from 'src/app/_services/client.service';
import { ClientDto } from 'src/app/DTOs/ClientDto';

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

  // Lists loaded from backend
  categories: any[] = [];
  projets: any[] = [];
  usersOptions: any[] = [];

  // Options for standard selects
  qualificationOptions: Qualification[] = [];
  prioriteOptions: Priorite[] = [];

  // Custom Qualification select
  selectedQualification: Qualification | null = null;
  isQualificationDropdownOpen: boolean = false;
  qualificationSearchTerm: string = '';
  filteredQualifications: Qualification[] = [];

  // Custom Priorité select
  selectedPriority: Priorite | null = null;
  isPriorityDropdownOpen: boolean = false;
  prioritySearchTerm: string = '';
  filteredPriorities: Priorite[] = [];

  // Dropdown Projet
  selectedProjet: any = null;
  isProjetDropdownOpen: boolean = false;
  searchProjet: string = '';
  filteredProjets: any[] = [];

  // Dropdown Catégorie
  selectedCategorie: any = null;
  isCategorieDropdownOpen: boolean = false;
  searchCategorie: string = '';
  filteredCategories: any[] = [];

  selectedFile: File | undefined = undefined;
  formSubmitted = false;

  // Ngx-editor instance and toolbar configuration
  editor!: Editor;
  toolbar!: Toolbar;

  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private ticketService: TicketService,
    private categorieProblemeService: CategorieProblemeService,
    private projetService: ProjetService,
    private accountService: AccountService,
    private clientService: ClientService,
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

    // Create reactive form with keys matching the model
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
    // Use forkJoin to wait for all data loading observables
    forkJoin([
      this.loadCategories(),
      this.loadClientProjets(),
      this.loadPriorites(),
      this.loadQualifications(),
      this.loadUsers()
    ]).subscribe();

    // Initialize ngx-editor and configure toolbar
    this.editor = new Editor();
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
    // Destroy the editor to free resources
    this.editor.destroy();
  }

  // Data-loading methods return Observables instead of subscribing internally

  loadCategories(): Observable<any> {
    return this.categorieProblemeService.getCategories().pipe(
      tap(categories => {
        this.categories = categories;
        this.filteredCategories = [...categories];
      }),
      catchError(error => {
        console.error("Erreur lors du chargement des catégories", error);
        this.toastr.error("Erreur lors du chargement des catégories de problèmes.");
        return of([]);
      })
    );
  }

  loadClientProjets(): Observable<any[]> {
    // Récupère le client connecté
    const client = this.accountService.currentUser() as ClientDto;
    if (!client) {
      this.toastr.error("Impossible de récupérer l'utilisateur courant.");
      return of([]);
    }
  
    return this.clientService.getClientProjects(client.id).pipe(
      tap(projets => {
        this.projets = projets;
        this.filteredProjets = [...projets];
      }),
      catchError(err => {
        console.error("Erreur lors du chargement des projets du client", err);
        this.toastr.error("Impossible de charger vos projets.");
        return of([]);
      })
    );
  }

  loadPriorites(): Observable<any> {
    return this.prioriteService.getPriorites().pipe(
      tap(priorites => {
        this.prioriteOptions = priorites;
        this.filteredPriorities = [...priorites];
      }),
      catchError(error => {
        console.error("Erreur lors du chargement des priorités", error);
        this.toastr.error("Erreur lors du chargement des priorités.");
        return of([]);
      })
    );
  }

  loadQualifications(): Observable<any> {
    return this.qualificationService.getQualifications().pipe(
      tap(qualifications => {
        this.qualificationOptions = qualifications;
        this.filteredQualifications = [...qualifications];
      }),
      catchError(error => {
        console.error("Erreur lors du chargement des qualifications", error);
        this.toastr.error("Erreur lors du chargement des qualifications.");
        return of([]);
      })
    );
  }

  loadUsers(): Observable<any> {
    return this.accountService.getUsers(1, 1000).pipe(
      tap(result => {
        this.usersOptions = result.items ?? [];
      }),
      catchError(error => {
        console.error("Erreur lors du chargement des utilisateurs", error);
        return of({ items: [] });
      })
    );
  }

  // Toggle dropdowns for different selects
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

  // Filter methods for dropdowns
  filterProjets(): void {
    this.filteredProjets = this.projets.filter(p =>
      p.nom.toLowerCase().includes(this.searchProjet.toLowerCase())
    );
  }

  selectProjet(projet: any): void {
    this.selectedProjet = projet;
    this.isProjetDropdownOpen = false;
    this.addTicketForm.patchValue({ projetId: projet.id });
  }

  filterCategories(): void {
    this.filteredCategories = this.categories.filter(cat =>
      cat.nom.toLowerCase().includes(this.searchCategorie.toLowerCase())
    );
  }

  selectCategorie(categorie: any): void {
    this.selectedCategorie = categorie;
    this.isCategorieDropdownOpen = false;
    this.addTicketForm.patchValue({ problemCategoryId: categorie.id });
  }

  filterQualifications(): void {
    this.filteredQualifications = this.qualificationOptions.filter(q =>
      q.name.toLowerCase().includes(this.qualificationSearchTerm.toLowerCase())
    );
  }

  selectQualification(qualification: Qualification): void {
    this.selectedQualification = qualification;
    this.isQualificationDropdownOpen = false;
    this.addTicketForm.patchValue({ qualificationId: qualification.id });
  }

  filterPriorities(): void {
    this.filteredPriorities = this.prioriteOptions.filter(p =>
      p.name.toLowerCase().includes(this.prioritySearchTerm.toLowerCase())
    );
  }

  selectPriority(priority: Priorite): void {
    this.selectedPriority = priority;
    this.isPriorityDropdownOpen = false;
    this.addTicketForm.patchValue({ priorityId: priority.id });
  }

  // File selection handler
  onFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedFile = event.target.files[0];
      this.addTicketForm.patchValue({ attachment: this.selectedFile });
    }
  }

  // Submit the form to create the ticket
  onSubmit(): void {
    this.formSubmitted = true;

    // 1) Validation du formulaire
    if (this.addTicketForm.invalid) {
      Object.values(this.addTicketForm.controls).forEach(ctrl => ctrl.markAsTouched());
      return;
    }

    // 2) Nettoyage de la description (suppression des balises <p>)
    const rawDesc = this.addTicketForm.get('description')?.value;
    const cleanedDescription = rawDesc?.replace(/<\/?p>/g, '') ?? '';

    // 3) Préparation du DTO
    const currentUser = this.accountService.currentUser();
    const ticket: TicketCreateDto = {
      title: this.addTicketForm.get('title')?.value,
      description: cleanedDescription,
      qualificationId: +this.addTicketForm.get('qualificationId')?.value,
      priorityId: +this.addTicketForm.get('priorityId')?.value,
      problemCategoryId: +this.addTicketForm.get('problemCategoryId')?.value,
      projetId: +this.addTicketForm.get('projetId')?.value,
      ownerId: currentUser ? currentUser.id : 0,
      // les deux champs suivants seront remplis par le service s’il y a un fichier
      attachmentBase64: '',
      attachmentFileName: ''
    };

    // 4) Appel du service (convertit le fichier en Base64 si present)
    this.loaderService.showLoader();
    this.ticketService.createTicket(ticket, this.selectedFile)
      .pipe(finalize(() => this.loaderService.hideLoader()))
      .subscribe({
        next: (created: any) => {
          this.toastr.success('Ticket créé avec succès.');
          this.router.navigate(['/home/Tickets/details', created.id]);
        },
        error: (err: any) => {
          console.error('Erreur lors de la création du ticket :', err);
          const msg = typeof err === 'string' ? err
                    : err.error?.message || 'Erreur lors de la création du ticket.';
          this.toastr.error(msg);
        }
      });
  }
  // Reset the form and clear file and dropdown selections
  resetForm(): void {
    this.addTicketForm.reset();
    this.selectedFile = undefined;
    this.selectedProjet = null;
    this.selectedCategorie = null;
    this.selectedQualification = null;
    this.selectedPriority = null;
  }

  // Cancel: if there are unsaved changes, confirm cancellation; otherwise, navigate away.
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

  // Global click handler to close dropdowns when clicking outside
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
