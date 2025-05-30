export interface TicketFilterRequest {
  userId?: number;
  ownerId?: number;
  personnelId?: number;
  projetId?: number; 
  start?: string;       // ISO (ex. "2025-04-29")
  end?: string;
  granularity: 'daily' | 'weekly' | 'monthly' | 'yearly' | 'none';
  
}