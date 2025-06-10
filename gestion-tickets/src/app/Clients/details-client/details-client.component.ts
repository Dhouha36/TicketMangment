import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Pays } from 'src/app/_models/pays';
import { Projet } from 'src/app/_models/Projet';
import { Societe } from 'src/app/_models/societe';
import { Ticket } from 'src/app/_models/ticket';
import { ClientService } from 'src/app/_services/client.service';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { PaysService } from 'src/app/_services/pays.service';
import { SocieteService } from 'src/app/_services/societe.service';
import { ConfirmModalComponent } from 'src/app/confirm-modal/confirm-modal.component';
import { ClientDto } from 'src/app/DTOs/ClientDto';
import { ProjetMini } from 'src/app/DTOs/ProjetMini';
import { PipesModule } from "../../_pipes/pipes.module";
import { forkJoin } from 'rxjs';
import { AttachProjectDialogComponent } from 'src/app/utilisateurs/attach-project-dialog/attach-project-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { PaysModalComponent } from 'src/app/PaysFile/pays-modal/pays-modal.component';

@Component({
  selector: 'app-details-client',
  imports: [CommonModule, ReactiveFormsModule, PipesModule, FormsModule],
  templateUrl: './details-client.component.html',
  styleUrl: './details-client.component.css'
})
export class DetailsClientComponent implements OnInit {
  client: ClientDto | null = null;
  clientForm!: FormGroup;
  isLoading = false;
  mode: 'view' | 'edit' = 'view';

  paysList: Pays[] = [];
  filteredPays: Pays[] = [];
  // Terme de recherche et état du dropdown
  paysSearchTerm: string = '';
  isPaysDropdownOpen = false;

  societesList: Societe[] = [];
  selectedCountry: Pays | undefined;
  activeTab: 'projets' | 'tickets' = 'projets';

  displayedProjects: Projet[] = [];
  displayedTickets: Ticket[] = [];
  projectSearchTerm: string = '';
  ticketSearchTerm: string = '';

  filteredSocietes: Societe[] = [];
  societeSearchTerm = '';
  isSocieteDropdownOpen = false;

  private fullProjects: Projet[] = [];
  private fullTickets: Ticket[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private toastr: ToastrService,
    private clientService: ClientService,
    private overlayModalService: OverlayModalService,
    private paysService: PaysService,
    private societeService: SocieteService
  ) { }

  ngOnInit(): void {
    this.initForm();
    forkJoin({
      pays: this.paysService.getPays(),
      societes: this.societeService.getSocietes()
    }).subscribe({
      next: ({ pays, societes }) => {
        this.paysList = pays;
        this.filteredPays = [...pays];
        this.societesList = societes;
        this.filteredSocietes = [...societes];
        const id = +this.route.snapshot.params['id'];
        this.loadClient(id);
      },
      error: () => this.toastr.error('Erreur chargement pays ou sociétés')
    });

    this.route.queryParams.subscribe(q => {
      this.mode = q['mode'] === 'edit' ? 'edit' : 'view';
    });
  }

  private initForm(): void {
    this.clientForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      paysId: [null, Validators.required],
      societeId: [null, Validators.required],
      numTelephone: ['', [Validators.required, Validators.pattern(/^[0-9\s]+$/)]],
      actif: [false]
    });

    this.clientForm.get('paysId')?.valueChanges.subscribe(id => {
      this.selectedCountry = this.paysList.find(p => p.idPays === id);
    });
  }

  private loadClient(id: number): void {
    this.isLoading = true;
    this.clientService.getById(id).subscribe({
      next: client => {
        this.client = client;
        this.clientForm.patchValue({
          firstName: client.firstName,
          lastName: client.lastName,
          email: client.email,
          paysId: client.paysId,
          societeId: client.societeId,
          numTelephone: (client.numTelephone || '')
            .replace(client.pays?.toString() || '', '')
            .trim(),
          actif: client.actif
        });
        this.selectedCountry = this.paysList.find(p => p.idPays === client.paysId);
        this.fetchProjects();
        this.fetchTickets();
      },
      error: () => this.toastr.error('Erreur chargement client'),
      complete: () => this.isLoading = false
    });
  }

  onProjectSearch(): void {
    this.displayedProjects = this.fullProjects.filter(p =>
      p.nom.toLowerCase().includes(this.projectSearchTerm.toLowerCase())
    );
  }

  private fetchProjects(): void {
    if (!this.client) return;
    this.clientService.getClientProjects(this.client.id)
      .subscribe(list => {
        this.fullProjects = list;
        this.onProjectSearch();
      });
  }

  detachProject(projetId: number): void {
    if (!this.client) return;
  
    // 1) Ouvrir votre modal de confirmation
    const modalRef = this.overlayModalService.open(ConfirmModalComponent);
  
    // 2) Personnaliser le message
    modalRef.message = `Voulez-vous vraiment détacher ce projet ?`;
  
    // 3) Si l’utilisateur confirme, appeler le service
    modalRef.confirmed.subscribe(() => {
      this.clientService.detachProjectFromClient(this.client!.id, projetId)
        .subscribe({
          next: () => {
            this.toastr.success('Projet détaché avec succès');
            this.fetchProjects();
          },
          error: err => {
            this.toastr.error('Impossible de détacher le projet');
          }
        });
      this.overlayModalService.close();  // fermer le modal
    });
  
    // 4) En cas d’annulation, juste fermer le modal
    modalRef.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  onTicketSearch(): void {
    this.displayedTickets = this.fullTickets.filter(t =>
      t.title.toLowerCase().includes(this.ticketSearchTerm.toLowerCase())
    );
  }

  private fetchTickets(): void {
    if (!this.client) return;
    this.clientService.getClientTickets(this.client.id)
      .subscribe(list => {
        this.fullTickets = list;
        this.onTicketSearch();
      });
  }

  switchMode(): void {
    this.mode = this.mode === 'view' ? 'edit' : 'view';
    this.router.navigate([], { queryParams: { mode: this.mode } });
  }

  onSubmit(): void {
    if (this.clientForm.invalid) {
      this.toastr.error('Veuillez corriger les erreurs');
      return;
    }
  
    const formData = new FormData();
    formData.append('Id', String(this.client!.id));
    formData.append('Email', this.clientForm.get('email')!.value);
    formData.append('FirstName', this.clientForm.get('firstName')!.value);
    formData.append('LastName', this.clientForm.get('lastName')!.value);
    formData.append('NumTelephone', this.clientForm.get('numTelephone')!.value);
    formData.append('PaysId', String(this.clientForm.get('paysId')!.value));
    formData.append('SocieteId', String(this.clientForm.get('societeId')!.value));
    formData.append('Actif', String(this.clientForm.get('actif')!.value));
    // if you need to pass “Pays” string:
    formData.append('Pays', this.getPaysName(this.clientForm.get('paysId')!.value));
  
    this.clientService.update(this.client!.id, formData).subscribe({
      next: () => {
        this.toastr.success('Client mis à jour');
        this.loadClient(this.client!.id);
      },
      error: () => this.toastr.error('Erreur mise à jour')
    });
  }
  

  deleteClient(): void {
    if (!this.client) return;
    if (confirm(`Supprimer ${this.client.firstName} ${this.client.lastName} ?`)) {
      this.clientService.delete(this.client.id).subscribe(() => {
        this.toastr.success('Client supprimé');
        this.router.navigate(['/home/clients']);
      });
    }
  }

  switchTab(tab: 'projets' | 'tickets'): void {
    this.activeTab = tab;
  }

  openAttachProjectDialog(): void {
    const dialogRef = this.dialog.open(AttachProjectDialogComponent, {
      data: {}
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && this.client) {
        this.clientService.addClientToProject(this.client.id, result.projetId)
          .subscribe({
            next: () => {
              this.toastr.success('Client attaché au projet avec succès !');
              this.fetchProjects(); // ou loadProjects selon ton code
            },
            error: err => {
              // Si l’API renvoie un 400 avec un message, on l’affiche
              const apiMessage = err.error as string;
              if (err.status === 400 && apiMessage) {
                this.toastr.error(apiMessage);
              } else {
                //this.toastr.error('Le client est déjà associé à ce projet.');
              }
            }
          });
      }
    });
  }

  viewProjet(projectId: number): void {
    // Redirection vers la page de détails du projet
    this.router.navigate(['home/Projets/details/', projectId]);
  }

  /** Bascule l’affichage du dropdown */
  togglePaysDropdown(): void {
    this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
  }

  /** Filtre la liste selon le terme saisi */
  onPaysSearch(): void {
    const term = this.paysSearchTerm.trim().toLowerCase();
    this.filteredPays = term
      ? this.paysList.filter(p => p.nom.toLowerCase().includes(term))
      : [...this.paysList];
  }

  /** Met à jour le formulaire et ferme le dropdown */
  selectPays(idPays: number): void {
    this.clientForm.get('paysId')!.setValue(idPays);
    this.clientForm.get('paysId')!.markAsDirty();
    this.isPaysDropdownOpen = false;
  }

  /** Renvoie le nom complet d’un pays à partir de son id */
  getPaysName(idPays: number): string {
    return this.paysList.find(p => p.idPays === idPays)?.nom || '';
  }
  openPaysModal(): void {
    const modal = this.overlayModalService.open(PaysModalComponent);
    // Quand un nouveau pays est ajouté, on recharge la liste
    modal.added.subscribe(() => {
      this.paysService.getPays();          // recharge paysList & filteredPays
      this.overlayModalService.close();
    });
  }

  toggleSocieteDropdown(): void {
    this.isSocieteDropdownOpen = !this.isSocieteDropdownOpen;
  }
  
  /** Filtre la liste des sociétés */
  onSocieteSearch(): void {
    const term = this.societeSearchTerm.trim().toLowerCase();
    this.filteredSocietes = term
      ? this.societesList.filter(s => s.nom.toLowerCase().includes(term))
      : [...this.societesList];
  }
  
  /** Sélectionne une société dans le formulaire */
  selectSociete(idSociete: number): void {
    this.clientForm.get('societeId')!.setValue(idSociete);
    this.clientForm.get('societeId')!.markAsDirty();
    this.isSocieteDropdownOpen = false;
  }
  
  /** Récupère le nom d’une société par son id */
  getSocieteName(idSociete: number): string {
    return this.societesList.find(s => s.id === idSociete)?.nom || '';
  }
}

