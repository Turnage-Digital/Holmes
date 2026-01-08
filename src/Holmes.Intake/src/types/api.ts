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

export interface CaptureAuthorizationRequest {
  mimeType: string;
  schemaVersion: string;
  payloadBase64: string;
  capturedAt?: string;
  metadata?: Record<string, string>;
}

export interface CaptureAuthorizationResponse {
  artifactId: string;
  mimeType: string;
  length: number;
  hash: string;
  hashAlgorithm: string;
  schemaVersion: string;
  createdAt: string;
}

export interface SaveIntakeProgressRequest {
  resumeToken: string;
  schemaVersion: string;
  payloadHash: string;
  payloadCipherText: string;
  updatedAt?: string;
}

export interface SubmitIntakeRequest {
  submittedAt?: string;
}

export interface RecordDisclosureViewedRequest {
  viewedAt?: string;
}

/**
 * Configuration for which intake form sections should be displayed,
 * based on the services ordered for this background check.
 */
export interface IntakeSectionConfig {
  requiredSections: string[];
  enabledServiceCodes: string[];
}

export interface IntakeBootstrapResponse {
  sessionId: string;
  orderId: string;
  subjectId: string;
  customerId: string;
  policySnapshotId: string;
  policySnapshotSchemaVersion: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  lastTouchedAt: string;
  submittedAt?: string;
  acceptedAt?: string;
  cancellationReason?: string;
  supersededBySessionId?: string;
  disclosure?: {
    disclosureId: string;
    disclosureVersion: string;
    disclosureHash: string;
    disclosureFormat: string;
    disclosureContent?: string;
    disclosureFetchUrl?: string;
  };
  authorizationCopy?: {
    authorizationId: string;
    authorizationVersion: string;
    authorizationHash: string;
    authorizationFormat: string;
    authorizationContent?: string;
  };
  authorizationMode: string;
  authorization?: {
    artifactId: string;
    mimeType: string;
    length: number;
    hash: string;
    hashAlgorithm: string;
    schemaVersion: string;
    capturedAt: string;
  };
  answers?: {
    schemaVersion: string;
    payloadHash: string;
    payloadCipherText: string;
    updatedAt: string;
  };
  sectionConfig?: IntakeSectionConfig;
}
