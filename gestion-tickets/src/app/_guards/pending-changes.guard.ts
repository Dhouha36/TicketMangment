import { Injectable } from '@angular/core';
import {
  CanDeactivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot
} from '@angular/router';
import { Observable } from 'rxjs';
import { OverlayModalService } from '../_services/overlay-modal.service';
import { ConfirmModalComponent } from '../confirm-modal/confirm-modal.component';

// Interface commune pour les wizards
export interface PendingChangesComponent {
  step: number;
  isCompleted?: boolean;
  cleanupOnExit(): void;
}

@Injectable({ providedIn: 'root' })
export class PendingChangesGuard implements CanDeactivate<PendingChangesComponent> {
  constructor(private overlay: OverlayModalService) {}

  canDeactivate(
    component: PendingChangesComponent,
    currentRoute: ActivatedRouteSnapshot,
    currentState: RouterStateSnapshot,
    nextState?: RouterStateSnapshot
  ): Observable<boolean> | boolean {
    // Si on est à l'étape initiale ou le wizard est terminé, pas de confirmation
    if (component.step === 1 || component.isCompleted) {
      return true;
    }

    // Sinon, affichage de la modale de confirmation
    return new Observable<boolean>(observer => {
      const modal = this.overlay.open(ConfirmModalComponent);
      modal.message = 'Vous allez perdre les données saisies. Confirmer la sortie ?';

      modal.confirmed.subscribe(() => {
        this.overlay.close();
        component.cleanupOnExit();
        observer.next(true);
        observer.complete();
      });

      modal.cancelled.subscribe(() => {
        this.overlay.close();
        observer.next(false);
        observer.complete();
      });
    });
  }
}