export interface DishSummaryResponse {
  id: string;
  name: string;
  category: string | null;
  cuisineType: string | null;
  aliases: string[];
  status: string;
}

export interface DishSearchResponse {
  items: DishSummaryResponse[];
  limit: number;
}

export interface DishDetailResponse {
  id: string;
  name: string;
  normalizedName: string;
  description: string | null;
  category: string | null;
  cuisineType: string | null;
  aliases: string[];
  ingredients: string[];
  dietaryFlags: string[];
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  deprecatedAtUtc: string | null;
}

export interface DishSearchRequest {
  query?: string | null;
  category?: string | null;
  cuisineType?: string | null;
  includeDeprecated?: boolean;
  limit?: number | null;
}
