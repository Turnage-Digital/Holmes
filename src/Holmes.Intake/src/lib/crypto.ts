const toHex = (buffer: ArrayBuffer) =>
  Array.from(new Uint8Array(buffer))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");

export const hashString = async (value: string): Promise<string> => {
  const subtle = (globalThis as { crypto?: Crypto }).crypto?.subtle;
  if (subtle) {
    const encoded = new TextEncoder().encode(value);
    const hashBuffer = await subtle.digest("SHA-256", encoded);
    return toHex(hashBuffer);
  }

  // Non-crypto fallback keeps the contract non-empty if subtle crypto is unavailable.
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash).toString(16);
};

export const toBase64 = (value: string): string => {
  if (typeof btoa === "function") {
    return btoa(unescape(encodeURIComponent(value)));
  }

  const bufferCtor = (globalThis as { Buffer?: any }).Buffer;
  if (bufferCtor) {
    return bufferCtor.from(value, "utf8").toString("base64");
  }

  return value;
};

export const fromBase64 = (value: string): string => {
  if (!value) return value;

  if (typeof atob === "function") {
    return decodeURIComponent(escape(atob(value)));
  }

  const bufferCtor = (globalThis as { Buffer?: any }).Buffer;
  if (bufferCtor) {
    return bufferCtor.from(value, "base64").toString("utf8");
  }

  return value;
};
