import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, _error) => {
        if (failureCount >= 2) {
          return false;
        }
        return true;
      }
    },
    mutations: {
      retry: 0
    }
  }
});
