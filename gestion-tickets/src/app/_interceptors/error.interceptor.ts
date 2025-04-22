import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError(error => {
      if (error) {
        switch (error.status) {
          case 400:
            if (error.error.errors) {
              const modalStateErrors = [];
              for (const key in error.error.errors) {
                if (error.error.errors[key]) {
                  modalStateErrors.push(error.error.errors[key]);
                }
              }
              const flatErrors = modalStateErrors.flat();
              // Afficher chaque message d'erreur via toastr
              flatErrors.forEach((err: string) => {
                toastr.error(err, 'Erreur');
              });
              return throwError(() => flatErrors);
            } else {
              return throwError(() => error.error);
            }
          case 401:
            
            accountService.logout();
            return throwError(() => error);
          case 404:
            toastr.error(`L'URL ${req.url} n'a pas été trouvée.`, 'Erreur 404');
            return throwError(() => error);
          case 409:
            toastr.error(error.error, 'Erreur 409');
            return throwError(() => error);
          case 500:
            toastr.error(error.error?.message || 'Erreur serveur', 'Erreur 500');
            return throwError(() => error);
          default:
            return throwError(() => ({ message: 'Un problème inattendu est survenu.' }));
        }
      }
      return throwError(() => error);
    })
  );
};
