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
