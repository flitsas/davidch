export type VehicleQueryMode = "Plate" | "Vin";

export type ProcedureTypeIndexSearchParams = {
  page?: string;
  pageSize?: string;
  sort?: string;
  name?: string;
  isActive?: string;
};

export type ProcedureTypeIndexItem = {
  id: string;
  name: string;
  code: string;
  vehicleQueryMode: VehicleQueryMode | 0 | 1;
  isActive: boolean;
  actorCount: number;
  documentCount: number;
  updatedAt: string;
};

export type ProcedureTypeIndexResponse = {
  items: ProcedureTypeIndexItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type DocumentKind = "Static" | "Dynamic";

export type ProcedureTypeDetail = {
  id: string;
  name: string;
  code: string;
  vehicleQueryMode: VehicleQueryMode | 0 | 1;
  isActive: boolean;
  actors: { roleLabel: string; sortOrder: number }[];
  documents: { label: string; kind: DocumentKind | 0 | 1; sortOrder: number }[];
  createdAt: string;
  updatedAt: string;
};

export type SaveProcedureTypePayload = {
  name: string;
  vehicleQueryMode: VehicleQueryMode;
  actors: { roleLabel: string }[];
  documents: { label: string; kind: DocumentKind }[];
};
