import { apiFetch } from "@/lib/api";
import {
  StartIntakeSessionRequest,
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
