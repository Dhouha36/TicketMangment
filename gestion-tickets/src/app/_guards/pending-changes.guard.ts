import { Injectable } from '@angular/core';
import {
  CanDeactivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { Observable } from 'rxjs';
import { OverlayModalService } from '../_services/overlay-modal.service';
import { ConfirmModalComponent } from '../confirm-modal/confirm-modal.component';
import { AjouterSocieteWizardComponent } from '../Societes/ajouter-societe-wizard/ajouter-societe-wizard.component';

@Injectable({ providedIn: 'root' })
export class PendingChangesGuard implements CanDeactivate<AjouterSocieteWizardComponent> {
  constructor(private overlay: OverlayModalService) {}

  canDeactivate(
    component: AjouterSocieteWizardComponent,
    currentRoute: ActivatedRouteSnapshot,
    currentState: RouterStateSnapshot,
    nextState?: RouterStateSnapshot
  ): Observable<boolean> | boolean {
    if (component.step === 1) { return true; }
  
    return new Observable<boolean>(observer => {
      const modal = this.overlay.open(ConfirmModalComponent);
      modal.message = 'Vous allez perdre les données saisies. Confirmer la sortie ?';
  
      modal.confirmed.subscribe(() => {
        this.overlay.close();             // <— on ferme la modal
        component.cleanupOnExit();
        observer.next(true);
        observer.complete();
      });
  
      modal.cancelled.subscribe(() => {
        this.overlay.close();             // <— on ferme aussi au cancel
        observer.next(false);
        observer.complete();
      });
    });
  }
}  
