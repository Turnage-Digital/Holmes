export type QueryParamValue =
    | string
    | number
    | boolean
    | null
    | undefined
    | (string | number | boolean)[];

export type QueryParams = Record<string, QueryParamValue>;

const ensureLeadingSlash = (path: string) =>
    path.startsWith("/") ? path : `/${path}`;

const stripTrailingSlash = (value: string) =>
    value === "/" ? value : value.replace(/\/$/, "");

const headersInitToRecord = (
    init?: HeadersInit,
): Record<string, string> | undefined => {
    if (!init) {
        return undefined;
    }

    if (init instanceof Headers) {
        const result: Record<string, string> = {};
        init.forEach((value, key) => {
            result[key] = value;
        });
        return result;
    }

    if (Array.isArray(init)) {
        return init.reduce<Record<string, string>>((acc, [key, value]) => {
            acc[key] = value;
            return acc;
        }, {});
    }

    return {...init};
};

const API_BASE_URL = stripTrailingSlash(
    import.meta.env.VITE_API_BASE_URL ?? "/api",
);

export const toQueryString = (params?: QueryParams): string => {
    if (!params) {
        return "";
    }

    const search = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
        if (value === undefined || value === null) {
            return;
        }

        const appendValue = (v: string | number | boolean) => {
            search.append(key, String(v));
        };

        if (Array.isArray(value)) {
            value.forEach(appendValue);
            return;
        }

        appendValue(value);
    });

    const serialized = search.toString();
    return serialized ? `?${serialized}` : "";
};

export class ApiError extends Error {
    public readonly status: number;

    public readonly payload?: unknown;

    public readonly requestId?: string | null;

    constructor(
        message: string,
        status: number,
        payload?: unknown,
        requestId?: string | null,
    ) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.payload = payload;
        this.requestId = requestId ?? undefined;
    }
}

type SerializableBody = BodyInit | object | null | undefined;

export type ApiRequestOptions = Omit<RequestInit, "body"> & {
    body?: SerializableBody;
};

export const apiFetch = async <TResponse>(
    path: string,
    options: ApiRequestOptions = {},
): Promise<TResponse> => {
    const target = `${API_BASE_URL}${ensureLeadingSlash(path)}`;
    const {body, headers: headersInit, ...rest} = options;

    const headers = new Headers({
        Accept: "application/json",
        ...(headersInitToRecord(headersInit) ?? {}),
    });

    const init: RequestInit = {
        credentials: "include",
        ...rest,
        headers,
    };

    if (body !== undefined && body !== null) {
        if (body instanceof FormData || body instanceof Blob) {
            init.body = body;
        } else if (typeof body === "string") {
            init.body = body;
            headers.set("Content-Type", "application/json");
        } else if (body instanceof ArrayBuffer || ArrayBuffer.isView(body)) {
            init.body = body as BodyInit;
        } else {
            init.body = JSON.stringify(body);
            headers.set("Content-Type", "application/json");
        }
    } else if (body === null) {
        init.body = null;
    }

    const response = await fetch(target, init);
    const requestId = response.headers.get("x-request-id");

    if (!response.ok) {
        let payload: unknown;
        try {
            const text = await response.text();
            payload = text ? JSON.parse(text) : undefined;
        } catch {
            try {
                payload = await response.text();
            } catch {
                payload = undefined;
            }
        }
        throw new ApiError(
            `Request to ${target} failed with status ${response.status}`,
            response.status,
            payload,
            requestId,
        );
    }

    if (response.status === 204) {
        return undefined as TResponse;
    }

    const contentType = response.headers.get("content-type") ?? "";

    if (contentType.includes("application/json")) {
        return response.json() as Promise<TResponse>;
    }

    if (contentType.startsWith("text/")) {
        return (await response.text()) as TResponse;
    }

    return (await response.arrayBuffer()) as unknown as TResponse;
};

export const createEventSource = (path: string): EventSource => {
    const target = `${API_BASE_URL}${ensureLeadingSlash(path)}`;
    return new EventSource(target, {withCredentials: true});
};
