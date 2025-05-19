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
  societesList: Societe[] = [];
  selectedCountry: Pays | undefined;
  activeTab: 'projets' | 'tickets' = 'projets';

  displayedProjects: Projet[] = [];
  displayedTickets: Ticket[] = [];
  projectSearchTerm: string = '';
  ticketSearchTerm: string = '';

  private fullProjects: Projet[] = [];
  private fullTickets: Ticket[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private toastr: ToastrService,
    private clientService: ClientService,
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
        this.societesList = societes;
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
    if (confirm('Détacher ce projet ?')) {
      this.clientService.detachProjectFromClient(this.client.id, projetId)
        .subscribe(() => this.fetchProjects());
    }
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
    // Récupère le nom du pays sélectionné
    const selectedPays = this.paysList.find(p => p.idPays === this.clientForm.value.paysId)?.nom ?? '';
  
    // Construit l'objet à envoyer, avec le champ `pays`
    const updated: Partial<ClientDto> = {
      ...this.clientForm.value,
      id: this.client!.id,
      pays: selectedPays
    };
  
    this.clientService.update(this.client!.id, updated).subscribe({
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

  getStatusName(id: number): string { return ''; }
  getPriorityName(id: number): string { return ''; }

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
}

