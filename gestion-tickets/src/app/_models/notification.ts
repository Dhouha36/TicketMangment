export interface AppNotification {
  id: number;
  message: string;
  dateEnvoi: string; 
  isRead: boolean;
  entityType: string; 
  entityId:   number;
  userId:     number;
}