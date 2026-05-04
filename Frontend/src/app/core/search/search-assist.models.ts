export interface SearchAssistRequest {
  query: string;
  location: string | null;
}

export interface SearchAssistSuggestion {
  type: string;
  value: string;
  reason: string;
}

export interface SearchAssistResponse {
  normalizedQuery: string;
  assistStatus: string;
  fallbackUsed: boolean;
  suggestions: SearchAssistSuggestion[];
  filters: Record<string, string>;
  explanation: string;
}
