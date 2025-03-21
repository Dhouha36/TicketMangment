import { Component, inject } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { SidenavService } from '../_services/sideNavService.service';

@Component({
    selector: 'app-header',
    imports: [
        MatToolbarModule,
        MatSidenavModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatCardModule,
        MatSelectModule,
        MatSlideToggleModule,
        MatIconModule,
    ],
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  accountService = inject(AccountService);
  private router = inject(Router);
  
  constructor(private sidenavService: SidenavService) {}

  toggleSidenav() {
    this.sidenavService.toggleSidenav();  // Appeler la méthode pour toggler le sidenav
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
