import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { FinishTicketDto } from '../../_models/finish-ticket-dto';
import { LoaderService } from '../../_services/loader.service';

@Component({
  selector: 'app-ticket-completion-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ticket-completion-modal.component.html',
  styleUrls: ['./ticket-completion-modal.component.scss']
})
export class TicketCompletionModalComponent {
  @Input() ticket: any;
  @Output() finished = new EventEmitter<FinishTicketDto>();
  @Output() closed = new EventEmitter<void>();

  finishData: FinishTicketDto = {
    isResolved: true,
    comment: '',
    hoursSpent: 0,
    completionDate: new Date()
  };

  formSubmitted: boolean = false;
  isLoading: boolean = false; // Nouvel attribut pour le loader

  constructor(private loaderService: LoaderService) {
    // Souscrire aux changements d’état de chargement
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
  }
  
  onSubmit(form: NgForm): void {
    this.formSubmitted = true;
    if (form.invalid) {
      return;
    }
    // Mettez à jour la date de fin
    this.finishData.completionDate = new Date();

    // On déclenche le loader
    this.loaderService.showLoader();

    // Émettez l'événement pour déclencher la mise à jour du ticket
    this.finished.emit(this.finishData);
  }

  onClose(): void {
    // Empêcher la fermeture si l'opération est en cours
    if (!this.isLoading) {
      this.closed.emit();
    }
  }

  onIsResolvedChange(): void {
    // Réinitialiser le flag de soumission pour masquer les messages d'erreur
    this.formSubmitted = false;
    // Si le ticket est marqué comme résolu, vider le commentaire
    if (this.finishData.isResolved) {
      this.finishData.comment = '';
    }
  }
}
