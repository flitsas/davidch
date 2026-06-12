export type TramiteIndexItem = {
  id: string;
  reference: string;
  procedureTypeName: string;
  procedureTypeCode: string;
  otDisplayName: string;
  otDivipolCode: string;
  vehicleQueryValue: string;
  status: string;
  createdAt: string;
  createdBy: string;
};

export type TramitesIndexResponse = {
  items: TramiteIndexItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type TramitesIndexSearchParams = {
  page?: string;
  pageSize?: string;
};

export type VehicleQueryMode = "Plate" | "Vin" | 0 | 1;
export type DocumentKind = "Static" | "Dynamic" | 0 | 1;
export type PersonKind = "Natural" | "Juridica";
export type DocumentIdType = "Cc" | "Ce" | "Passport" | "Nit";

export type ProcedureTypeSummary = {
  id: string;
  name: string;
  code: string;
  vehicleQueryMode: VehicleQueryMode;
  isActive: boolean;
  actorCount: number;
  documentCount: number;
  updatedAt: string;
};

export type ProcedureDefinition = {
  id: string;
  name: string;
  code: string;
  vehicleQueryMode: VehicleQueryMode;
  isActive: boolean;
  actors: { roleLabel: string; sortOrder: number }[];
  documents: { label: string; kind: DocumentKind; sortOrder: number }[];
};

export type TrafficAuthority = {
  divipolCode: string;
  displayName: string;
  otTenantId: string;
};

export type ActorFormState = {
  roleLabel: string;
  sortOrder: number;
  personKind: PersonKind;
  documentType: DocumentIdType;
  documentNumber: string;
  legalRepresentative?: {
    documentType: DocumentIdType;
    documentNumber: string;
  };
};

export type DocumentFileState = {
  label: string;
  file: File | null;
};
