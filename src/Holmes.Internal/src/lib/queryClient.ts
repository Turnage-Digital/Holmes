import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, _error) => {
        return failureCount < 2;
      },
    },
    mutations: {
      retry: 0,
    },
  },
});
