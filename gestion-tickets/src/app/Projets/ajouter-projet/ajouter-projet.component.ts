import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { ProjetService } from '../../_services/projet.service';
import { SocieteService } from '../../_services/societe.service';
import { PaysService } from '../../_services/pays.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, NgIf } from '@angular/common';
import { AccountService } from '../../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { MatSidenavModule } from '@angular/material/sidenav';
import { LoaderService } from '../../_services/loader.service';
import { ProjetCreate } from 'src/app/DTOs/projet-create.model';
import { ClientCreate } from 'src/app/DTOs/client-create.model';

@Component({
  selector: 'app-ajouter-projet',
  imports: [CommonModule, FormsModule, NgIf, ReactiveFormsModule, MatSidenavModule],
  templateUrl: './ajouter-projet.component.html',
  styleUrls: ['./ajouter-projet.component.css']
})
export class AjouterProjetComponent implements OnInit {
  wizardForm!: FormGroup;
  step = 1;
  isLoading = false;
  isCompleted = false;

  societes = [] as any[];
  chefsProjet = [] as any[];
  paysList = [] as any[];
  selectedCountry?: any;

  constructor(
    private fb: FormBuilder,
    private projetService: ProjetService,
    private societeService: SocieteService,
    private accountService: AccountService,
    private paysService: PaysService,
    private toastr: ToastrService,
    private loaderService: LoaderService,
    private router: Router
  ) {
    this.loaderService.isLoading$.subscribe(l => this.isLoading = l);
  }

  ngOnInit(): void {
    this.initForm();
    this.loadSocietes();
    this.loadChefs();
    this.loadPays();

    this.clientGroup.get('pays')!.valueChanges.subscribe(id => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +id);
    });
  }

  private initForm(): void {
    this.wizardForm = this.fb.group({
      projet: this.fb.group({
        nom: ['', Validators.required],
        societeId: [null, Validators.required],
        chefProjetId: [null, Validators.required]
      }),
      client: this.fb.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        pays: [null, Validators.required],
        numTelephone: ['', [Validators.required, Validators.pattern('^[0-9\\s]+$'), Validators.minLength(8), Validators.maxLength(15)]]
      })
    });
  }

  private loadSocietes() {
    this.societeService.getSocietes().subscribe(
      data => this.societes = data,
      () => this.toastr.error('Erreur chargement sociétés')
    );
  }
  private loadChefs() {
    this.accountService.getUsersByRole('Chef de Projet').subscribe(
      users => this.chefsProjet = users,
      () => this.toastr.error('Erreur chargement chefs de projet')
    );
  }
  private loadPays() {
    this.paysService.getPays().subscribe(
      data => this.paysList = data,
      () => this.toastr.error('Erreur chargement pays')
    );
  }

  get projetGroup() { return this.wizardForm.get('projet') as FormGroup; }
  get clientGroup() { return this.wizardForm.get('client') as FormGroup; }

  nextStep(): void {
    if (this.projetGroup.invalid) return;
    this.step = 2;
  }

  prevStep(): void {
    this.step = 1;
  }

  submitAll(): void {
    if (this.clientGroup.invalid) return;

    const { projet, client } = this.wizardForm.value;
    const projetDto: ProjetCreate = {
      nom: projet.nom,
      description: '',
      societeId: projet.societeId,
      chefProjetId: projet.chefProjetId
    };
    this.loaderService.showLoader();

    this.projetService.addProjet(projetDto).subscribe({
      next: projResp => {
        const clientDto: ClientCreate = {
          firstName: client.firstName,
          lastName: client.lastName,
          email: client.email,
          pays: +client.pays,
          numTelephone: this.selectedCountry!.codeTel + ' ' + client.numTelephone,
          role: 'Client',
          societeId: projet.societeId,
          projetId: projResp.id
        };
        this.accountService.register(clientDto).subscribe({
          next: () => {
            this.toastr.success('Projet et client ajoutés avec succès');
            this.isCompleted = true;
            this.loaderService.hideLoader();
            this.router.navigate(['/home/Projets']);
          },
          error: () => {
            this.toastr.error('Erreur création client');
            this.loaderService.hideLoader();
          }
        });
      },
      error: () => {
        this.toastr.error('Erreur création projet');
        this.loaderService.hideLoader();
      }
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  beforeUnloadHandler(event: BeforeUnloadEvent): void {
    if (!this.isCompleted && this.step > 1) {
      event.preventDefault();
      event.returnValue = 'Vous allez perdre les données saisies. Voulez-vous vraiment quitter ?';
    }
  }
}
