import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';
import { ClientDto } from '../DTOs/ClientDto';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private accountService: AccountService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const allowedRoles = route.data['roles'] as string[];
    const cu = this.accountService.currentUser();

    // Type-guard : s'assure que c'est un User (pas un ClientDto)
    if (this.isUser(cu)) {
      const userRole = cu.role.toLowerCase().trim();
      if (allowedRoles.map(r => r.toLowerCase()).includes(userRole)) {
        return true;
      }
    }

    // Non autorisé ou pas un User -> redirection
    this.router.navigate(['/not-found']);
    return false;
  }

  /** Type-guard pour distinguer User vs ClientDto */
  private isUser(u: User | ClientDto | null): u is User {
    return !!u && (u as User).role !== undefined;
  }
}
