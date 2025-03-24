import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Ticket } from '../../_models/ticket';
import { User } from '../../_models/user';
import { TicketService } from '../../_services/ticket.service';
import { TicketValidationDto } from '../../_models/ticket-validation.dto';
import { AccountService } from '../../_services/account.service';
import { forkJoin } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { LoaderService } from '../../_services/loader.service';

@Component({
  selector: 'app-ticket-validation-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './ticket-validation-modal.component.html',
  styleUrls: ['./ticket-validation-modal.component.scss']
})
export class TicketValidationModalComponent implements OnInit {
  @Input() ticket: Ticket | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() validated = new EventEmitter<void>();

  private toastr = inject(ToastrService);
  
  validationForm!: FormGroup;
  developers: User[] = [];
  isLoading: boolean = false; // Propriété de chargement

  constructor(
    private fb: FormBuilder,
    private ticketService: TicketService,
    private userService: AccountService,
    private loaderService: LoaderService
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
  }

  ngOnInit(): void {
    // Création du formulaire
    this.validationForm = this.fb.group({
      action: ['', Validators.required],
      reason: [''],
      responsibleId: [null]
    });

    // Lors du changement de l'option, appliquer ou supprimer les validateurs et réinitialiser le contrôle concerné
    this.validationForm.get('action')?.valueChanges.subscribe(val => {
      if (val === 'reject') {
        this.validationForm.get('reason')?.setValidators([Validators.required]);
        this.validationForm.get('reason')?.reset();
        this.validationForm.get('responsibleId')?.clearValidators();
        this.validationForm.get('responsibleId')?.reset();
      } else if (val === 'accept') {
        this.validationForm.get('responsibleId')?.setValidators([Validators.required]);
        this.validationForm.get('responsibleId')?.reset();
        this.validationForm.get('reason')?.clearValidators();
        this.validationForm.get('reason')?.reset();
      } else {
        this.validationForm.get('reason')?.clearValidators();
        this.validationForm.get('responsibleId')?.clearValidators();
      }
      this.validationForm.get('reason')?.updateValueAndValidity();
      this.validationForm.get('responsibleId')?.updateValueAndValidity();
    });

    this.loadDevelopers();
  }

  loadDevelopers(): void {
    forkJoin([
      this.userService.getUsersByRole('collaborateur'),
      this.userService.getUsersByRole('chef de projet')
    ]).subscribe({
      next: ([collaborateurs, chefs]) => {
        this.developers = collaborateurs.concat(chefs);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des développeurs et chefs de projets', err);
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  validateTicket(): void {
    if (this.validationForm.invalid) {
      this.validationForm.markAllAsTouched();
      return;
    }
    const formValue = this.validationForm.value;
    const validationData: TicketValidationDto = {
      isAccepted: formValue.action === 'accept',
      reason: formValue.reason,
      responsibleId: formValue.action === 'accept' ? formValue.responsibleId : null
    };

    // Active le loader
    this.loaderService.showLoader();

    this.ticketService.validateTicket(this.ticket!.id, validationData).subscribe({
      next: () => {
        this.toastr.success('Ticket validé avec succéss');
        this.loaderService.hideLoader();
        this.validated.emit();
        this.close();
      },
      error: (err) => {
        console.error('Erreur lors de la validation du ticket', err);
        this.toastr.error('Erreur lors de la validation du ticket');
        this.loaderService.hideLoader();
      }
    });
  }
}
