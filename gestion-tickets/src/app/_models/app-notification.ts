// _models/app-notification.ts
export interface AppNotification {
  id: number;
  message: string;
  dateEnvoi: string;
  estLu: boolean;
  utilisateurId: number;
}
