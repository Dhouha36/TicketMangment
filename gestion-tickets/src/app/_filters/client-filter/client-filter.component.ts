import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  OnInit,
  Output,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { Projet } from 'src/app/_models/Projet';
import { Societe } from 'src/app/_models/societe';
import { LoaderService } from 'src/app/_services/loader.service';
import { ProjetService } from 'src/app/_services/projet.service';
import { SocieteService } from 'src/app/_services/societe.service';

interface SocieteDropdown {
  id: number;
  nom: string;
}

@Component({
  selector: 'app-client-filter',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './client-filter.component.html',
  styleUrls: ['./client-filter.component.css'],
})
export class ClientFilterComponent implements OnInit {
  filterForm!: FormGroup;

  @Output()
  applyFilter = new EventEmitter<{
    actif: boolean | undefined;
    societeId: number | undefined;
    projetId: number | undefined;
  }>();

  // Dropdown « Actif »
  actifOptions: string[] = ['Tous', 'Oui', 'Non'];
  filteredActifs: string[] = [];
  selectedActifLabel = 'Tous';
  actifSearchTerm = '';
  isActifDropdownOpen = false;

  // Dropdown « Société »
  societeOptions: SocieteDropdown[] = [];
  filteredSocietes: SocieteDropdown[] = [];
  selectedSociete?: SocieteDropdown;
  societeSearchTerm = '';
  isSocieteDropdownOpen = false;

  // Dropdown « Projet »
  projetOptions: Projet[] = [];
  filteredProjets: Projet[] = [];
  selectedProjet?: Projet;
  projetSearchTerm = '';
  isProjetDropdownOpen = false;

  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private loaderService: LoaderService,
    private societeService: SocieteService,
    private projetService: ProjetService
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
  }

  ngOnInit(): void {
    // 1) Initialisation du FormGroup
    this.filterForm = this.fb.group({
      actif: ['Tous'],        // 'Tous' | 'Oui' | 'Non'
      societeId: [undefined], // number ou undefined
      projetId: [undefined],  // number ou undefined
    });

    // 2) Charger les Sociétés depuis l’API
    this.societeService.getSocietes().subscribe({
      next: (societes: Societe[]) => {
        this.societeOptions = [
          { id: 0, nom: 'Tous' },
          ...societes.map((s) => ({ id: s.id, nom: s.nom })),
        ];
        this.filteredSocietes = [...this.societeOptions];
      },
      error: (err) =>
        console.error('Erreur lors de la récupération des sociétés', err),
    });

    // 3) Charger les Projets depuis l’API
    this.projetService.getProjets({}).subscribe({
      next: (projets: Projet[]) => {
        this.projetOptions = [
          {
            id: 0,
            nom: 'Tous',
            description: '',
            societeId: 0,
            clientId: null,
            chefProjetId: 0,
            idPays: 0,
          } as Projet,
          ...projets,
        ];
        this.filteredProjets = [...this.projetOptions];
      },
      error: (err) =>
        console.error('Erreur lors de la récupération des projets', err),
    });

    // 4) Initialiser la liste des “Actifs”
    this.filteredActifs = [...this.actifOptions];
  }

  /**
   * Bouton « Appliquer les filtres »
   */
  onSubmit(): void {
    const values = this.filterForm.value as {
      actif: string;
      societeId: number | undefined;
      projetId: number | undefined;
    };

    const actifFinal: boolean | undefined =
      values.actif === 'Tous' ? undefined : values.actif === 'Oui';

    const societeIdFinal: number | undefined = values.societeId;
    const projetIdFinal: number | undefined = values.projetId;

    console.log(
      'DEBUG onSubmit → actifFinal =',
      actifFinal,
      'societeIdFinal =',
      societeIdFinal,
      'projetIdFinal =',
      projetIdFinal
    );

    this.loaderService.showLoader();
    this.applyFilter.emit({
      actif: actifFinal,
      societeId: societeIdFinal,
      projetId: projetIdFinal,
    });
    this.loaderService.hideLoader();
  }

  /**
   * Bouton « Réinitialiser »
   */
  onReset(): void {
    this.filterForm.reset({
      actif: 'Tous',
      societeId: undefined,
      projetId: undefined,
    });
    this.selectedActifLabel = 'Tous';
    this.selectedSociete = undefined;
    this.selectedProjet = undefined;
    this.projetSearchTerm = '';
    this.filteredProjets = [...this.projetOptions];

    this.applyFilter.emit({
      actif: undefined,
      societeId: undefined,
      projetId: undefined,
    });
  }

  /**
   * Ouverture/fermeture des dropdowns
   */
  toggleDropdown(type: 'actif' | 'societe' | 'projet', event: Event): void {
    event.stopPropagation();

    if (type === 'actif') {
      this.isActifDropdownOpen = !this.isActifDropdownOpen;
      if (this.isActifDropdownOpen) {
        this.filteredActifs = [...this.actifOptions];
        this.actifSearchTerm = '';
      }
      this.isSocieteDropdownOpen = false;
      this.isProjetDropdownOpen = false;

    } else if (type === 'societe') {
      this.isSocieteDropdownOpen = !this.isSocieteDropdownOpen;
      if (this.isSocieteDropdownOpen) {
        this.filteredSocietes = [...this.societeOptions];
        this.societeSearchTerm = '';
      }
      this.isActifDropdownOpen = false;
      this.isProjetDropdownOpen = false;

    } else {
      // type === 'projet'
      this.isProjetDropdownOpen = !this.isProjetDropdownOpen;
      if (this.isProjetDropdownOpen) {
        // En ouvrant le dropdown « Projet », on garantit qu'il est à jour en fonction de la société
        if (this.selectedSociete && this.selectedSociete.id !== 0) {
          const socId = this.selectedSociete.id;
          this.filteredProjets = this.projetOptions.filter(
            (p: Projet) => p.societeId === socId
          );
        } else {
          this.filteredProjets = [...this.projetOptions];
        }
        this.projetSearchTerm = '';
      }
      this.isActifDropdownOpen = false;
      this.isSocieteDropdownOpen = false;
    }
  }

  // Filtrer la liste des « Actifs »
  filterActifs(): void {
    const term = this.actifSearchTerm.toLowerCase();
    this.filteredActifs = this.actifOptions.filter((a) =>
      a.toLowerCase().includes(term)
    );
  }

  // Filtrer la liste des « Sociétés »
  filterSocietes(): void {
    const term = this.societeSearchTerm.toLowerCase();
    this.filteredSocietes = this.societeOptions.filter((s) =>
      s.nom.toLowerCase().includes(term)
    );
  }

  // Filtrer la liste des « Projets »
  filterProjets(): void {
    const term = this.projetSearchTerm.toLowerCase();
    this.filteredProjets = this.projetOptions.filter((p) =>
      p.nom.toLowerCase().includes(term)
    );
  }

  // --------------------------------------------------
  //  Sélection d’un élément
  // --------------------------------------------------

  /** Quand type = 'actif', item doit être une string */
  selectItem(type: 'actif', item: string): void;
  /** Quand type = 'societe', item doit être un SocieteDropdown */
  selectItem(type: 'societe', item: SocieteDropdown): void;
  /** Quand type = 'projet', item doit être un Projet */
  selectItem(type: 'projet', item: Projet): void;

  selectItem(
    type: 'actif' | 'societe' | 'projet',
    item: string | SocieteDropdown | Projet
  ): void {
    if (type === 'actif') {
      // Sélection « Actif »
      const valeurActif = item as string;
      this.selectedActifLabel = valeurActif;
      this.filterForm.patchValue({ actif: valeurActif });
      this.isActifDropdownOpen = false;
      return;
    }

    if (type === 'societe') {
      // Sélection « Société »
      const soc: SocieteDropdown = item as SocieteDropdown;
      console.log('→ sélectionner SOCIÉTÉ, id reçue =', soc.id);

      // 1) Transformer ID=0 (“Tous”) en undefined
      const socId = soc.id === 0 ? undefined : soc.id;
      this.filterForm.patchValue({ societeId: socId, projetId: undefined });

      // 2) Mettre à jour la sélection affichée
      this.selectedSociete = socId === undefined ? undefined : { id: socId, nom: soc.nom };

      // 3) Réinitialiser la sélection Projet
      this.selectedProjet = undefined;

      // 4) **Mise à jour immédiate de filteredProjets** selon la société sélectionnée
      if (socId === undefined) {
        this.filteredProjets = [...this.projetOptions];
      } else {
        this.filteredProjets = this.projetOptions.filter(
          (p: Projet) => p.societeId === socId
        );
      }

      // 5) Réinitialiser le terme de recherche du Projet
      this.projetSearchTerm = '';

      this.isSocieteDropdownOpen = false;
      return;
    }

    // type === 'projet'
    const prj: Projet = item as Projet;
    console.log('→ sélectionner PROJET, id reçue =', prj.id);

    // Transformer ID=0 (“Tous”) en undefined
    const prjId = prj.id === 0 ? undefined : prj.id;
    this.filterForm.patchValue({ projetId: prjId });

    // Stocker strictement un objet de type Projet complet (ou undefined)
    this.selectedProjet = prjId === undefined ? undefined : prj;
    this.isProjetDropdownOpen = false;
  }

  /** Si on clique en dehors, on ferme tous les dropdowns */
  @HostListener('document:click')
  onDocumentClick(): void {
    this.isActifDropdownOpen = false;
    this.isSocieteDropdownOpen = false;
    this.isProjetDropdownOpen = false;
  }
}
