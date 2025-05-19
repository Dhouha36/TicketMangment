//user create
export interface ClientCreate {
  firstName:    string;
  lastName:     string;
  email:        string;
  pays:         number;
  numTelephone: string;
  role:         'Super Admin';
  projetId:     number;
  contract: {                // ← nouvelle propriété
    dateDebut: string;      // ISO (ex. new Date().toISOString())
    dateFin?:   string;     // optionnel
    type:      'Projet';    // valeur fixe
    projetId:  number;      // même que ci‑dessus
  };
}
