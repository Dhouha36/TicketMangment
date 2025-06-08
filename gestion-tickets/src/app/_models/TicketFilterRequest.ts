export interface TicketFilterRequest {
  userId?: number;
  ownerId?: number;
  personnelId?: number;
  projetId?: number; 
  start?: string;    
  end?: string;
  granularity: 'daily' | 'weekly' | 'monthly' | 'yearly' | 'none';
}