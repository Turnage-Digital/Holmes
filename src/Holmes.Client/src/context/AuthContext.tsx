import {createContext, useContext} from "react";

import {CurrentUserDto} from "@/types/api";

const AuthContext = createContext<CurrentUserDto | null>(null);

export const AuthProvider = AuthContext.Provider;

export const useAuth = () => {
    const value = useContext(AuthContext);
    if (!value) {
        throw new Error("useAuth must be used within an AuthProvider");
    }

    return value;
};
