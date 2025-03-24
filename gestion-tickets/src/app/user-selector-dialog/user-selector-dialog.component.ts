import { Component, Inject, OnInit, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { User } from '../_models/user';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';

@Component({
    selector: 'app-user-selector-dialog',
    imports: [
        CommonModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatListModule,
        MatButtonModule,
        MatIconModule,
        FormsModule
    ],
    templateUrl: './user-selector-dialog.component.html',
    styleUrls: ['./user-selector-dialog.component.scss']
})
export class UserSelectorDialogComponent implements OnInit {
  users: User[] = [];
  filteredUsers: User[] = [];
  searchTerm = '';
  selectedUser: User | null = null;
  overlayRef: OverlayRef | null = null;

  // Référence au template du dropdown
  @ViewChild('dropdownTemplate') dropdownTemplate!: TemplateRef<any>;
  // Référence à l'élément déclencheur
  @ViewChild('trigger') triggerElement!: any;

  constructor(
    public dialogRef: MatDialogRef<UserSelectorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { availableUsers: User[] },
    private overlay: Overlay,
    private viewContainerRef: ViewContainerRef
  ) {}

  ngOnInit(): void {
    if (this.data?.availableUsers) {
      this.users = this.data.availableUsers;
      this.filteredUsers = [...this.users];
    }
  }

  toggleDropdown(): void {
    if (this.overlayRef && this.overlayRef.hasAttached()) {
      this.closeDropdown();
    } else {
      this.openDropdown();
    }
  }

  openDropdown(): void {
    // Crée la stratégie de positionnement relative à l'élément déclencheur
    const positionStrategy = this.overlay.position()
      .flexibleConnectedTo(this.triggerElement.nativeElement)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
          offsetY: 4
        }
      ]);
    
    // Crée l'overlay s'il n'existe pas encore
    if (!this.overlayRef) {
      this.overlayRef = this.overlay.create({
        positionStrategy,
        scrollStrategy: this.overlay.scrollStrategies.reposition(),
        hasBackdrop: true,
        backdropClass: 'transparent-backdrop'
      });
      // Ferme l'overlay si l'utilisateur clique en dehors
      this.overlayRef.backdropClick().subscribe(() => this.closeDropdown());
    } else {
      // Mettez à jour la stratégie de positionnement lors de l'ouverture
      this.overlayRef.updatePositionStrategy(positionStrategy);
    }
    // Attache le TemplatePortal au dropdown
    const portal = new TemplatePortal(this.dropdownTemplate, this.viewContainerRef);
    this.overlayRef.attach(portal);
  }

  closeDropdown(): void {
    if (this.overlayRef) {
      this.overlayRef.detach();
    }
  }

  filterUsers(): void {
    const term = this.searchTerm.trim().toLowerCase();
    if (term) {
      this.filteredUsers = this.users.filter(user =>
        (`${user.firstName} ${user.lastName}`).toLowerCase().includes(term) ||
        (user.email?.toLowerCase().includes(term)) ||
        (user.role?.toLowerCase().includes(term))
      );
    } else {
      this.filteredUsers = [...this.users];
    }
  }

  selectUser(user: User): void {
    this.selectedUser = user;
    this.closeDropdown();
    // Réinitialise le champ de recherche pour la prochaine ouverture
    this.searchTerm = '';
  }

  confirmSelection(): void {
    // Ferme le dialog en renvoyant l'utilisateur sélectionné
    this.dialogRef.close(this.selectedUser);
  }
}