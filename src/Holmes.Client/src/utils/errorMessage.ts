import {ApiError} from "@/lib/api";

export const getErrorMessage = (
    error: unknown,
    fallback = "Something went wrong",
) => {
    if (!error) {
        return fallback;
    }

    if (error instanceof ApiError) {
        if (typeof error.payload === "string") {
            return error.payload;
        }

        if (
            error.payload &&
            typeof error.payload === "object" &&
            "message" in error.payload &&
            typeof (error.payload as { message?: unknown }).message === "string"
        ) {
            return (error.payload as { message?: string }).message ?? fallback;
        }

        return `${error.message} (status ${error.status})`;
    }

    if (error instanceof Error) {
        return error.message;
    }

    if (typeof error === "string") {
        return error;
    }

    return fallback;
};
