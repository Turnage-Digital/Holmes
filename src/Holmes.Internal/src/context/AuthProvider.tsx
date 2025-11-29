import React, { PropsWithChildren, useEffect, useMemo, useState } from "react";

import AuthContext, { AuthState } from "./auth-context";

interface Claim {
  type: string;
  value: string;
}

const AuthProvider = ({ children }: PropsWithChildren) => {
  const [authState, setAuthState] = useState<AuthState>({
    systemType: null,
    name: null,
    exp: null,
    logoutUrl: null,
  });

  useEffect(() => {
    const controller = new AbortController();

    fetch("/bff/user", {
      headers: {
        "X-CSRF": "1",
      },
      credentials: "include",
      signal: controller.signal,
    })
      .then((response) => {
        if (response.ok) {
          return response.json() as Promise<Claim[]>;
        }

        if (response.status === 401) {
          const returnUrl = encodeURIComponent(
            window.location.pathname + window.location.search,
          );
          window.location.href = `/bff/login?returnUrl=${returnUrl}`;
        }

        return null;
      })
      .then((data) => {
        if (!data) {
          return;
        }

        const getClaimValue = (
          claims: Claim[],
          claimType: string,
        ): string | null => {
          const claim = claims.find((c) => c.type === claimType);
          return claim ? claim.value : null;
        };

        const systemType = getClaimValue(data, "systemType");
        const name = getClaimValue(data, "name");
        const expIn = getClaimValue(data, "bff:session_expires_in");
        const exp = expIn ? Date.now() + parseInt(expIn, 10) * 1000 : null;
        const logoutUrl = getClaimValue(data, "bff:logout_url");

        setAuthState({
          systemType,
          name,
          exp,
          logoutUrl,
        });
      })
      .catch(() => {
        /* swallow network errors; BFF will redirect when available */
      });

    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (authState.exp && authState.logoutUrl) {
      const interval = window.setInterval(() => {
        const now = Date.now();
        if (authState.exp < now) {
          window.location.href = authState.logoutUrl!;
        }
      }, 60000);

      return () => window.clearInterval(interval);
    }
  }, [authState.exp, authState.logoutUrl]);

  const value = useMemo(() => authState, [authState]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export default AuthProvider;
