export interface PlaceSummaryResponse {
  id: string;
  name: string;
  addressText: string;
  area: string;
  city: string;
  country: string;
  cuisineTypes: string[];
  priceRange: string | null;
  status: string;
}

export interface PlaceSearchResponse {
  items: PlaceSummaryResponse[];
  limit: number;
}

export interface PlaceDetailResponse {
  id: string;
  name: string;
  normalizedName: string;
  description: string | null;
  addressText: string;
  area: string;
  city: string;
  country: string;
  latitude: number | null;
  longitude: number | null;
  cuisineTypes: string[];
  priceRange: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PlaceSearchRequest {
  query?: string | null;
  city?: string | null;
  area?: string | null;
  cuisineType?: string | null;
  priceRange?: string | null;
  includeInactive?: boolean;
  limit?: number | null;
}
