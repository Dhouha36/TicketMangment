import { Component, ElementRef, LOCALE_ID, OnInit, ViewChild, inject } from '@angular/core';
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
import { environment } from 'src/environments/environment';
import { ImagePreviewModalComponent } from '../image-preview-modal/image-preview-modal.component';
import { ClientDto } from 'src/app/DTOs/ClientDto';
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
  @ViewChild('editor') editor!: ElementRef<HTMLDivElement>;
  public assetsUrl = environment.assetsUrl;

  ticket: Ticket | null = null;
  currentUser: User | ClientDto | null = null;
  ticketId!: number;
  developers: User[] = [];

  // Pour la gestion des commentaires
  comments: TicketComment[] = [];
  newComment: string = '';
  filesToUpload: { file: File; url: string }[] = [];

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


  selectAllText(event: MouseEvent) {
    const input = event.target as HTMLInputElement;
    input.select();
  }
  
  onAddComment() {
    if (!this.newComment.trim() && !this.filesToUpload.length) return;
    this.loaderService.showLoader();
  
    const form = new FormData();
    form.append('contenu', this.newComment);
    form.append('ticketId', this.ticketId.toString());
  
    // Si l'utilisateur courant est un client, on ajoute son clientId
    if (this.isClient()) {
      const client = this.currentUser as ClientDto;
      form.append('clientId', this.currentUser!.id.toString());
    }
  
    // Ajout des fichiers (images ou documents)
    this.filesToUpload.forEach(item =>
      form.append('files', item.file, item.file.name)
    );

  
    this.commentService.addCommentFormData(form).subscribe({
      next: comment => {
        // Réinitialisation de l'éditeur et rafraîchissement de la liste
        this.editor.nativeElement.innerText = '';
        this.newComment = '';
        this.filesToUpload = [];
        this.loaderService.hideLoader();
        this.loadComments();
      },
      error: () => {
        this.loaderService.hideLoader();
      }
    });
  }
  
  

  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files) { return; }
    const files = Array.from(input.files);
    files.forEach(f => {
      const url = URL.createObjectURL(f);
      this.filesToUpload.push({ file: f, url });
    });
    // met à jour la variable newComment si vous en avez besoin
    this.newComment = this.editor.nativeElement?.innerText ?? '';
  }
  
  // Met à jour le texte tapé
  onEditorInput(event: Event) {
    this.newComment = (event.target as HTMLDivElement).innerText;
  }

  // Gère le dragover pour autoriser le drop
  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  // Drop de fichiers
  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      this.addFiles(Array.from(event.dataTransfer.files));
    }
  }

  // Ctrl+V / Cmd+V  
  onPaste(event: ClipboardEvent) {
    if (event.clipboardData?.files.length) {
      event.preventDefault();
      this.addFiles(Array.from(event.clipboardData.files));
    }
  }

  private addFiles(files: File[]) {
    files.forEach(f => {
      const url = URL.createObjectURL(f);
      this.filesToUpload.push({ file: f, url });
    });
    // (facultatif) on garde newComment à jour
    this.newComment = this.editor.nativeElement.innerText;
  }
  


  removeFile(fileToRemove: File) {
    this.filesToUpload = this.filesToUpload.filter(item => item.file !== fileToRemove);
  }  

  openPhoto(url: string) {
    const modal = this.overlayModalService.open(ImagePreviewModalComponent);
    modal.url = url;                    // passe l'URL au composant
    modal.close = () => this.overlayModalService.close();
  }

  // Logique pour afficher le bouton de validation
  canValidateTicket(): boolean {
    if (!this.ticket || !this.isUser(this.currentUser) || !this.ticket.projet) return false;
    
    const role = this.currentUser.role.toLowerCase();
    const isDefaultStatus = this.ticket.statut?.name === '—';
    const isSuperAdmin = role === 'super admin';
    const isProjectChef = this.currentUser.id === this.ticket.projet.chefProjetId;
  
    return isDefaultStatus && (isSuperAdmin || (role === 'chef de projet' && isProjectChef));
  }

  canFinishTicket(): boolean {
    if (!this.ticket || !this.isUser(this.currentUser)) return false;
    
    // Seul le responsable peut terminer
    if (this.ticket.responsibleId !== this.currentUser.id) return false;
  
    // Doit être validé
    if (!this.ticket.approvedAt) return false;
  
    // Statut non final
    const statusName = this.ticket.statut?.name?.toLowerCase();
    const invalidStatuses = ['—', 'résolu', 'non résolu', 'refusé'];
    
    return !(statusName && invalidStatuses.includes(statusName));
  }

  // Pour garder la même condition pour la mise à jour du responsable
  canUpdateResponsible(): boolean {
    if (!this.ticket || !this.isUser(this.currentUser) || !this.ticket.projet) return false;
    
    const role = this.currentUser.role.toLowerCase();
    const isSuperAdmin = role === 'super admin';
    const isProjectChef = this.currentUser.id === this.ticket.projet.chefProjetId;
    const isCurrentResponsible = this.currentUser.id === this.ticket.responsibleId;
  
    // Statut non final
    const statusName = this.ticket.statut?.name?.toLowerCase();
    const invalidStatuses = ['—', 'résolu', 'non résolu', 'refusé'];

    // Autoriser : super admin, chef du projet, OU responsable actuel
    return (isSuperAdmin || isProjectChef || isCurrentResponsible) 
      && !!this.ticket.approvedAt && !(statusName && invalidStatuses.includes(statusName)); // Ticket doit être validé
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
  

  isImage(url: string): boolean {
    return /\.(jpe?g|png|gif|bmp|svg)$/i.test(url);
  }

  private isUser(u: User | ClientDto | null): u is User {
    return !!u && (u as User).role !== undefined;
  }

  /** Type‑guard : vrai client si paysId existe */
  private isClientDto(u: User | ClientDto | null): u is ClientDto {
    return !!u && (u as ClientDto).paysId !== undefined;
  }

  /** Exposé au template : est‑ce un client ? */
  isClient(): boolean {
    return this.isClientDto(this.currentUser);
  }
  

}
