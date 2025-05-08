import { Component, LOCALE_ID, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Ticket } from '../../_models/ticket';
import { TicketService } from '../../_services/ticket.service';
import { AccountService } from '../../_services/account.service';
import { User } from '../../_models/user';
import { Comment as TicketComment } from '../../_models/comment';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { TicketValidationModalComponent } from '../ticket-validation-modal/ticket-validation-modal.component';
import { forkJoin } from 'rxjs';
import { TicketCompletionModalComponent } from '../ticket-completion-modal/ticket-completion-modal.component';
import { FinishTicketDto } from '../../_models/finish-ticket-dto';
import { ToastrService } from 'ngx-toastr';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { CommentService } from '../../_services/comment.service';
import { LoaderService } from '../../_services/loader.service';
import { GlobalLoaderService } from '../../_services/global-loader.service';
registerLocaleData(localeFr);

@Component({
  selector: 'app-ticket-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  providers: [
    { provide: LOCALE_ID, useValue: 'fr-FR' }
  ],
  templateUrl: './ticket-details.component.html',
  styleUrls: ['./ticket-details.component.scss']
})
export class TicketDetailsComponent implements OnInit {
  

  ticket: Ticket | null = null;
  currentUser: User | null = null;
  ticketId!: number;
  developers: User[] = [];

  // Pour la gestion des commentaires
  comments: TicketComment[] = [];
  newComment: string = '';

  // Propriété pour stocker le responsable sélectionné
  selectedResponsibleId: number | null = null;

  isLoading: boolean = false;

  constructor(
    private route: ActivatedRoute,
    private ticketService: TicketService,
    private accountService: AccountService,
    private overlayModalService: OverlayModalService,
    private toastr: ToastrService,
    private commentService: CommentService,
    private loaderService: LoaderService,
    private globalLoaderService: GlobalLoaderService,
  ) {
    this.loaderService.isLoading$.subscribe(loading => {
      this.isLoading = loading;
    });
  }
  ngOnInit(): void {
    // Souscrire aux changements de paramètres
    this.route.paramMap.subscribe(paramMap => {
      // Récupère l'ID depuis la route à chaque changement
      this.ticketId = +paramMap.get('id')!;
      // Recharge les données associées au ticket
      this.loadTicket();
      this.loadComments();
    });
  
    // Charge une seule fois la liste des développeurs (si elle ne change pas en fonction de l'ID)
    this.loadDevelopers();
    // Récupère l'utilisateur courant
    this.currentUser = this.accountService.currentUser();
  }
  

  loadTicket(): void {
    this.globalLoaderService.showGlobalLoader();
    this.ticketService.getTicket(this.ticketId).subscribe({
      next: (ticket) => {
        this.ticket = ticket;
        // Initialiser le responsable sélectionné avec la valeur actuelle du ticket
        this.selectedResponsibleId = ticket.responsibleId || null;
      },
      error: (err) => {
        console.error('Erreur lors de la récupération du ticket', err);
        const message = err.error || 'Erreur lors de la récupération du ticket';
        this.toastr.error(message, 'Erreur');
      },
      complete: () => {
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }  

  loadDevelopers(): void {
    forkJoin([
      this.accountService.getUsersByRole('collaborateur'),
      this.accountService.getUsersByRole('chef de projet')
    ]).subscribe({
      next: ([collaborateurs, chefs]) => {
        this.developers = collaborateurs.concat(chefs);
      },
      error: (err) => {
        console.error('Erreur lors du chargement des développeurs et chefs de projets', err);
        const message = err.error || 'Erreur lors du chargement des développeurs';
        this.toastr.error(message, 'Erreur');
      }
    });
  }

  loadComments(): void {
    this.commentService.getCommentsByTicket(this.ticketId).subscribe({
      next: (comments) => {
        this.comments = comments;
      },
      error: (err) => {
        console.error('Erreur lors du chargement des commentaires', err);
      }
    });
  }



  onAddComment(): void {
    if (!this.newComment || this.newComment.trim() === '') return;
    this.loaderService.showLoader();
    this.commentService.addComment({ contenu: this.newComment, ticketId: this.ticketId }).subscribe({
      next: (comment) => {
        this.newComment = '';
        this.comments.push(comment);
        this.loadComments();
        this.loaderService.hideLoader();
      },
      error: (err) => {
        console.error('Erreur lors de l\'ajout du commentaire', err);
        const message = err.error || 'Erreur lors de l\'ajout du commentaire';
        this.toastr.error(message, 'Erreur');
        this.loaderService.hideLoader();
      }
    });
  }

  // Logique pour afficher le bouton de validation
  canValidateTicket(): boolean {
    if (!this.ticket || !this.currentUser) return false;
    const userRole = this.currentUser.role?.toLowerCase() || '';
    const statusIsDefault = (this.ticket.statut?.name === "—");
    return statusIsDefault && (userRole === 'chef de projet' || userRole === 'super admin');
  }

  // Méthode pour déterminer si l'utilisateur peut terminer ou modifier le responsable
  canFinishTicket(): boolean {
    if (!this.ticket || !this.currentUser) return false;

    // Les clients ne peuvent pas terminer ni modifier le responsable
    if (this.currentUser.role.toLowerCase() === 'client') return false;

    // La mise à jour du responsable ne doit être possible que si le ticket a été validé (approvedAt renseigné)
    if (!this.ticket.approvedAt) return false;

    // Vérifier le statut du ticket : si le ticket est déjà dans un statut final (résolu, non résolu, refusé ou non validé) on bloque
    const statusName = this.ticket.statut?.name?.toLowerCase();
    const invalidStatuses = ['—', 'résolu', 'non résolu', 'refusé'];
    if (statusName && invalidStatuses.includes(statusName)) {
      return false;
    }

    const userRole = this.currentUser.role.toLowerCase();
    return userRole === 'chef de projet' ||
      userRole === 'super admin' ||
      (this.ticket.responsibleId === this.currentUser.id);
  }

  // Pour garder la même condition pour la mise à jour du responsable
  canUpdateResponsible(): boolean {
    return this.canFinishTicket();
  }


  // Ouvre le modal de validation
  openValidationModal(): void {
    const modalInstance = this.overlayModalService.open(TicketValidationModalComponent);
    modalInstance.ticket = this.ticket;
    modalInstance.validated.subscribe(() => {
      this.handleValidationDone();
      this.overlayModalService.close();
    });
    modalInstance.closed.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  handleValidationDone(): void {
    this.loadTicket();
  }

  // Ouvre le modal de clôture du ticket
  openCompletionModal(): void {
    const modalInstance = this.overlayModalService.open(TicketCompletionModalComponent);
    modalInstance.ticket = this.ticket;
    modalInstance.finished.subscribe((finishData: FinishTicketDto) => {
      // Appel à la méthode qui gère la validation et les mises à jour
      this.updateTicketCompletion(finishData);
      this.isLoading =!this.isLoading;
    });
    modalInstance.closed.subscribe(() => {
      this.overlayModalService.close();
      this.isLoading =!this.isLoading;
    });
  }
  
  

  updateTicketCompletion(finishData: any): void {
    this.ticketService.finishTicket(this.ticket!.id, finishData).subscribe({
      next: () => {
        this.toastr.success('Ticket clôturé avec succès');
        this.loadTicket();
        this.loadComments();
        this.overlayModalService.close(); 
      },
      error: err => {
        console.error('Erreur lors de la clôture du ticket', err);
        const message = err.error || 'Erreur lors de la clôture du ticket';
        this.toastr.error(message, 'Erreur');
      }
    });
  }
  

  // Méthode pour mettre à jour le responsable
  updateResponsible(): void {
    if (!this.ticket || !this.selectedResponsibleId) {
      console.error("Ticket ou responsable non défini");
      this.toastr.error("Ticket ou responsable non défini", 'Erreur');
      return;
    }
    this.loaderService.showLoader();
    this.ticketService.updateResponsible(this.ticket.id, { responsibleId: this.selectedResponsibleId }).subscribe({
      next: () => {
        this.loadTicket();
        this.toastr.success('Responsable mis à jour avec succès');
        this.loaderService.hideLoader();
      },
      error: err => {
        console.error('Erreur lors de la mise à jour du responsable', err);
        const message = err.error || 'Erreur lors de la mise à jour du responsable';
        this.toastr.error(message, 'Erreur');
        this.loaderService.hideLoader();
      }
    });
  }

  getInitials(firstName?: string, lastName?: string): string {
    // Si aucun prénom/nom, on renvoie juste une chaîne vide
    if (!firstName && !lastName) return '';
  
    let initials = '';
    if (firstName && firstName.length > 0) {
      initials += firstName.charAt(0).toUpperCase();
    }
    if (lastName && lastName.length > 0) {
      initials += lastName.charAt(0).toUpperCase();
    }
    return initials;
  }
  


}
