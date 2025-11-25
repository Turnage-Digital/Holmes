export interface StartIntakeSessionRequest {
  resumeToken: string;
  deviceInfo?: string;
  startedAt?: string;
}

export interface VerifyOtpRequest {
  code: string;
}

export interface VerifyOtpResponse {
  verified: boolean;
}
