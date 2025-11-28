import {createContext, useContext} from "react";

export interface AuthState {
    systemType: string | null;
    name: string | null;
    exp: number | null;
    logoutUrl: string | null;
}

const AuthContext = createContext<AuthState | null>(null);

export const useAuth = () => {
    const value = useContext(AuthContext);
    if (!value) {
        throw new Error("useAuth must be used within an AuthContext provider");
    }
    return value;
};

export default AuthContext;
