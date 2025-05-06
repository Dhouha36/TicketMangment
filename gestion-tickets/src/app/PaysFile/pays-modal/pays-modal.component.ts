import { NgFor } from '@angular/common';
import { Component, OnInit, EventEmitter, Output, Inject } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs/operators';
import { Pays } from 'src/app/_models/pays';
import { GlobalLoaderService } from 'src/app/_services/global-loader.service';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { PaysService } from 'src/app/_services/pays.service';
import { ConfirmModalComponent } from 'src/app/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-pays-modal',
  imports: [ NgFor, FormsModule],
  templateUrl: './pays-modal.component.html',
  styleUrl: './pays-modal.component.css'
})
export class PaysModalComponent implements OnInit {
  @Output() added = new EventEmitter<void>();

  existingPays: Pays[] = [];
  availablePays: Pays[] = [];
  filteredPays: Pays[]  = [];

  searchTerm = '';
  addForm: FormGroup;

  constructor(
    private paysService: PaysService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private overlayModalService: OverlayModalService,
    private globalLoaderService: GlobalLoaderService 
  ) {
    this.addForm = this.fb.group({
      nom: ['', Validators.required],
      codeTel: ['', Validators.required],
      photo: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.paysService.getPays().subscribe(p => {
      this.existingPays = p.map(pay => this.normalizeUrl(pay));
      this.loadAllCountries();
    });
  }

  private loadAllCountries(): void {
    // 1) on affiche le loader
    this.globalLoaderService.showGlobalLoader();

    this.paysService.getAllCountries().subscribe({
      next: all => {
        this.availablePays = all
          .filter(a => !this.existingPays.some(e => e.nom === a.nom))
          .map(pay => this.normalizeUrl(pay))
          .sort((a, b) => a.nom.localeCompare(b.nom));
        this.updateFilteredPays();
      },
      error: err => {
        console.error('Erreur lors du chargement de tous les pays', err);
        this.toastr.error('Impossible de charger la liste des pays.');
      },
      complete: () => {
        // 2) on masque le loader
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }

  onSearchChange(): void {
    this.updateFilteredPays();
  }

  private updateFilteredPays(): void {
    const term = this.searchTerm.trim().toLowerCase();
    this.filteredPays = term
      ? this.availablePays.filter(p => p.nom.toLowerCase().includes(term))
      : [...this.availablePays];
  }

  /** Ouvre la modale de confirmation via CDK Overlay */
  promptAddCountry(p: Pays): void {
    // On garde bien votre message d’origine :
    const svc = this.overlayModalService as any;
    const parentRef = svc.overlayRef;
  
    const confirm = this.overlayModalService.open(ConfirmModalComponent);
    confirm.message = `Voulez-vous vraiment ajouter le pays "${p.nom}" ?`;
  
    confirm.confirmed
      .pipe(take(1))
      .subscribe(() => {
        this.addCountryConfirmed(p);
        // on ferme la confirm et on restaure le modal parent
        this.toastr.success('Pays ajouté avec succès')
        svc.overlayRef.dispose();
        svc.overlayRef = parentRef;
      });
  
    confirm.cancelled
      .pipe(take(1))
      .subscribe(() => {
        svc.overlayRef.dispose();
        svc.overlayRef = parentRef;
      });
  }

  /** Appelle le service pour ajouter le pays et met à jour la vue */
  private addCountryConfirmed(p: Pays): void {
    this.paysService.addPays(p.nom, p.codeTel, p.photoUrl!).then(obs => {
      obs.subscribe(newPays => {
        const norm = this.normalizeUrl(newPays);
        this.existingPays.push(norm);
        this.availablePays = this.availablePays.filter(x => x.nom !== p.nom);
        this.updateFilteredPays();
        this.added.emit();
      }, err => console.error(err));
    }).catch(err => console.error(err));
  }

  close(): void {
    this.overlayModalService.close();
  }

  private normalizeUrl(pay: Pays): Pays {
    if (pay.photoUrl) pay.photoUrl = pay.photoUrl.split('\\').join('/');
    return pay;
  }

  deletePays(idPays: number): void {
  const svc = this.overlayModalService as any;
  const parentRef = svc.overlayRef;

  const modal = this.overlayModalService.open(ConfirmModalComponent);
  modal.message = 'Êtes-vous sûr de vouloir supprimer ce pays ?';

  modal.confirmed
    .pipe(take(1))
    .subscribe(() => {
      this.paysService.deletePays(idPays).subscribe({
        next: () => {
          this.existingPays = this.existingPays.filter(p => p.idPays !== idPays);
          this.toastr.success('Pays supprimé avec succès.');
        },
        error: err => console.error(err)
      });
      svc.overlayRef.dispose();
      svc.overlayRef = parentRef;
    });

  modal.cancelled
    .pipe(take(1))
    .subscribe(() => {
      svc.overlayRef.dispose();
      svc.overlayRef = parentRef;
    });
}  
}