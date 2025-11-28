import {apiFetch} from "@/lib/api";
import {
    CaptureConsentRequest,
    CaptureConsentResponse,
    IntakeBootstrapResponse,
    SaveIntakeProgressRequest,
    StartIntakeSessionRequest,
    SubmitIntakeRequest,
    VerifyOtpRequest,
    VerifyOtpResponse,
} from "@/types/api";

export const startIntakeSession = (
    sessionId: string,
    payload: StartIntakeSessionRequest,
) =>
    apiFetch<void>(`/intake/sessions/${sessionId}/start`, {
        method: "POST",
        body: payload,
    });

export const verifyIntakeOtp = (
    sessionId: string,
    payload: VerifyOtpRequest,
): Promise<VerifyOtpResponse> =>
    apiFetch<VerifyOtpResponse>(`/intake/sessions/${sessionId}/otp/verify`, {
        method: "POST",
        body: payload,
    });

export const captureConsentArtifact = async (
    sessionId: string,
    payload: CaptureConsentRequest,
): Promise<CaptureConsentResponse> => {
    const response = await apiFetch<{
        ArtifactId: string;
        MimeType: string;
        Length: number;
        Hash: string;
        HashAlgorithm: string;
        SchemaVersion: string;
        CreatedAt: string;
    }>(`/intake/sessions/${sessionId}/consent`, {
        method: "POST",
        body: payload,
    });

    return {
        artifactId: response.ArtifactId,
        mimeType: response.MimeType,
        length: response.Length,
        hash: response.Hash,
        hashAlgorithm: response.HashAlgorithm,
        schemaVersion: response.SchemaVersion,
        createdAt: response.CreatedAt,
    };
};

export const saveIntakeProgress = (
    sessionId: string,
    payload: SaveIntakeProgressRequest,
) =>
    apiFetch<void>(`/intake/sessions/${sessionId}/progress`, {
        method: "POST",
        body: payload,
    });

export const submitIntake = (sessionId: string, payload: SubmitIntakeRequest) =>
    apiFetch<void>(`/intake/sessions/${sessionId}/submit`, {
        method: "POST",
        body: payload,
    });

export const getIntakeBootstrap = (
    sessionId: string,
    resumeToken: string,
): Promise<IntakeBootstrapResponse> =>
    apiFetch<IntakeBootstrapResponse>(
        `/intake/sessions/${sessionId}/bootstrap?resumeToken=${encodeURIComponent(resumeToken)}`,
    );
