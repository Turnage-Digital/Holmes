import React, {
  ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import { ApiError } from "@holmes/ui-core";
import {
  Alert,
  Box,
  Button,
  Container,
  Divider,
  LinearProgress,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import { alpha } from "@mui/material/styles";
import { useMutation } from "@tanstack/react-query";

import {
  AddressHistoryForm,
  EducationHistoryForm,
  EmploymentHistoryForm,
  PhoneForm,
  ReferenceForm,
} from "@/components/forms";
import {
  IntakeSectionId,
  useSectionVisibility,
} from "@/hooks/useSectionVisibility";
import { fromBase64, hashString, toBase64 } from "@/lib/crypto";
import {
  captureAuthorizationArtifact,
  getIntakeBootstrap,
  recordDisclosureViewed,
  saveIntakeProgress,
  startIntakeSession,
  submitIntake,
  verifyIntakeOtp,
} from "@/services/intake";
import {
  CaptureAuthorizationResponse,
  IntakeBootstrapResponse,
  IntakeSectionConfig,
  SaveIntakeProgressRequest,
} from "@/types/api";
import {
  createEmptyAddress,
  IntakeAddress,
  IntakeEducation,
  IntakeEmployment,
  IntakePhone,
  IntakeReference,
} from "@/types/intake";

type IntakeStepId =
  | "verify"
  | "otp"
  | "disclosure"
  | "authorization"
  | "personal"
  | "phone"
  | "addresses"
  | "employment"
  | "education"
  | "references"
  | "review"
  | "success";

interface StepDefinition {
  id: IntakeStepId;
  title: string;
  subtitle: string;
  render: () => ReactNode;
  primaryCtaLabel?: string;
  isTerminal?: boolean;
  onPrimary?: () => Promise<boolean | void> | boolean | void;
  primaryButtonDisabled?: boolean;
  /** If set, this step is only shown when the section is visible */
  sectionId?: IntakeSectionId;
}

interface IntakeFormState {
  firstName: string;
  lastName: string;
  middleName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  ssn: string;
  authorizationAccepted: boolean;
  addresses: IntakeAddress[];
  employments: IntakeEmployment[];
  educations: IntakeEducation[];
  references: IntakeReference[];
  phones: IntakePhone[];
}

const formSchemaVersion = "intake-extended-v2";

const initialFormState: IntakeFormState = {
  firstName: "",
  lastName: "",
  middleName: "",
  dateOfBirth: "",
  email: "",
  phone: "",
  ssn: "",
  authorizationAccepted: false,
  addresses: [createEmptyAddress()],
  employments: [],
  educations: [],
  references: [],
  phones: [],
};

const resolveMimeType = (format: string) => {
  switch (format) {
    case "html":
      return "text/html";
    case "pdf":
      return "application/pdf";
    default:
      return "text/plain";
  }
};

const StepCopy = ({
  heading,
  body,
  helper,
}: {
  heading: string;
  body: string;
  helper?: string;
}) => {
  const helperContent = helper ? (
    <Typography color="text.secondary" fontSize="0.85rem">
      {helper}
    </Typography>
  ) : null;

  return (
    <Stack spacing={1.5}>
      <Typography variant="h5">{heading}</Typography>
      <Typography color="text.secondary">{body}</Typography>
      {helperContent}
    </Stack>
  );
};

const maskValue = (value: string) =>
  value.length > 8 ? `${value.slice(0, 4)}…${value.slice(-4)}` : value;

const parseInviteParams = () => {
  if (typeof window === "undefined") {
    return { sessionId: "", resumeToken: "" };
  }

  const params = new URLSearchParams(window.location.search);
  const sessionParam =
    params.get("session") ??
    params.get("sessionId") ??
    import.meta.env.VITE_INTAKE_SESSION_ID ??
    "";
  const resumeParam =
    params.get("token") ??
    params.get("resumeToken") ??
    import.meta.env.VITE_INTAKE_RESUME_TOKEN ??
    "";

  return {
    sessionId: sessionParam.trim(),
    resumeToken: resumeParam.trim(),
  };
};

const extractErrorMessage = (error: unknown) => {
  if (error instanceof ApiError) {
    const payload = error.payload as { message?: string } | undefined;
    return payload?.message ?? error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Something went wrong. Try again.";
};

const IntakeFlow = () => {
  const [activeIndex, setActiveIndex] = useState(0);
  const inviteDefaults = useMemo(() => parseInviteParams(), []);
  const [sessionId] = useState(inviteDefaults.sessionId);
  const [resumeToken] = useState(inviteDefaults.resumeToken);
  const inviteParamsMissing = !(sessionId && resumeToken);
  const deviceInfo =
    typeof navigator === "undefined" ? "intake-client" : navigator.userAgent;

  const [inviteError, setInviteError] = useState<string | null>(null);
  const [otpCode, setOtpCode] = useState("");
  const [otpError, setOtpError] = useState<string | null>(null);
  const [otpVerified, setOtpVerified] = useState(false);
  const [formState, setFormState] = useState<IntakeFormState>(initialFormState);
  const [authorizationReceipt, setAuthorizationReceipt] =
    useState<CaptureAuthorizationResponse | null>(null);
  const [authorizationError, setAuthorizationError] = useState<string | null>(
    null,
  );
  const [disclosureSnapshot, setDisclosureSnapshot] =
    useState<IntakeBootstrapResponse["disclosure"] | null>(null);
  const [authorizationCopy, setAuthorizationCopy] =
    useState<IntakeBootstrapResponse["authorizationCopy"] | null>(null);
  const [authorizationMode, setAuthorizationMode] = useState<string | null>(
    null,
  );
  const [disclosureViewed, setDisclosureViewed] = useState(false);
  const [disclosureError, setDisclosureError] = useState<string | null>(null);
  const [progressError, setProgressError] = useState<string | null>(null);
  const [submissionError, setSubmissionError] = useState<string | null>(null);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
  const [sectionConfig, setSectionConfig] = useState<
    IntakeSectionConfig | undefined
  >();

  // Determine which sections should be visible based on ordered services
  const { isVisible } = useSectionVisibility(sectionConfig);
  const envPrefill = useMemo(
    () => ({
      firstName: import.meta.env.VITE_INTAKE_SUBJECT_FIRST ?? "",
      lastName: import.meta.env.VITE_INTAKE_SUBJECT_LAST ?? "",
      email: import.meta.env.VITE_INTAKE_SUBJECT_EMAIL ?? "",
      phone: import.meta.env.VITE_INTAKE_SUBJECT_PHONE ?? "",
      city: import.meta.env.VITE_INTAKE_SUBJECT_CITY ?? "",
      region: import.meta.env.VITE_INTAKE_SUBJECT_STATE ?? "",
    }),
    [],
  );

  const requireSessionId = () => {
    if (!sessionId) {
      throw new Error("Missing intake session id.");
    }

    return sessionId;
  };

  const requireInviteParams = () => {
    if (!sessionId || !resumeToken) {
      throw new Error("Invite link is incomplete.");
    }

    return { sessionId, resumeToken };
  };

  useEffect(() => {
    if (!otpVerified) {
      return;
    }

    setFormState((current) => {
      const next = { ...current };
      (["firstName", "lastName", "email", "phone"] as const)
        .filter((field) => !current[field] && envPrefill[field])
        .forEach((field) => {
          next[field] = envPrefill[field];
        });
      return next;
    });
  }, [envPrefill, otpVerified]);

  const { mutateAsync: verifyOtpMutateAsync, isPending: isVerifyingOtp } =
    useMutation({
      mutationFn: (code: string) =>
        verifyIntakeOtp(requireSessionId(), { code }),
    });

  const { mutateAsync: startSessionMutateAsync, isPending: isStartingSession } =
    useMutation({
      mutationFn: () => {
        const { sessionId: safeSessionId, resumeToken: safeToken } =
          requireInviteParams();
        return startIntakeSession(safeSessionId, {
          resumeToken: safeToken,
          deviceInfo,
          startedAt: new Date().toISOString(),
        });
      },
    });

  const {
    mutateAsync: captureAuthorizationMutateAsync,
    isPending: isCapturingAuthorization,
  } = useMutation({
    mutationFn: () => {
      if (!authorizationCopy?.authorizationContent) {
        throw new Error("Authorization copy is missing.");
      }

      const format = authorizationCopy.authorizationFormat || "text";
      return captureAuthorizationArtifact(requireSessionId(), {
        mimeType: resolveMimeType(format),
        schemaVersion: authorizationCopy.authorizationVersion,
        payloadBase64: toBase64(authorizationCopy.authorizationContent),
        capturedAt: new Date().toISOString(),
        metadata: {
          source: "intake-ui",
          variant: "phase-2",
          artifactType: "authorization",
        },
      });
    },
  });

  const { mutateAsync: saveProgressMutateAsync, isPending: isSavingProgress } =
    useMutation({
      mutationFn: (payload: SaveIntakeProgressRequest) =>
        saveIntakeProgress(requireSessionId(), payload),
    });

  const {
    mutateAsync: submitIntakeMutateAsync,
    isPending: isSubmittingIntake,
  } = useMutation({
    mutationFn: () =>
      submitIntake(requireSessionId(), {
        submittedAt: new Date().toISOString(),
      }),
  });

  const { mutateAsync: bootstrapMutateAsync, isPending: isBootstrapping } =
    useMutation({
      mutationFn: () => {
        const { sessionId: safeSessionId, resumeToken: safeToken } =
          requireInviteParams();
        return getIntakeBootstrap(safeSessionId, safeToken);
      },
      onError: (error: unknown) => {
        setBootstrapError(extractErrorMessage(error));
      },
    });

  const {
    mutateAsync: recordDisclosureViewedAsync,
    isPending: isRecordingDisclosureViewed,
  } = useMutation({
    mutationFn: () =>
      recordDisclosureViewed(requireSessionId(), {
        viewedAt: new Date().toISOString(),
      }),
  });

  const updateField = useCallback(
    <K extends keyof IntakeFormState>(key: K, value: IntakeFormState[K]) =>
      setFormState((prev) => ({ ...prev, [key]: value })),
    [],
  );

  const validatePersonalInfo = useCallback(() => {
    const missingFields: string[] = [];
    if (!formState.firstName.trim()) missingFields.push("First name");
    if (!formState.lastName.trim()) missingFields.push("Last name");
    if (!formState.dateOfBirth.trim()) missingFields.push("Date of birth");
    if (!formState.email.trim()) missingFields.push("Email");
    if (!formState.phone.trim()) missingFields.push("Phone");
    return missingFields;
  }, [formState]);

  const validateAddresses = useCallback(() => {
    if (formState.addresses.length === 0) {
      return ["At least one address is required"];
    }
    const issues: string[] = [];
    formState.addresses.forEach((addr, i) => {
      const label = addr.isCurrent ? "Current address" : `Address ${i + 1}`;
      if (!addr.street1.trim()) issues.push(`${label}: street address`);
      if (!addr.city.trim()) issues.push(`${label}: city`);
      if (!addr.state.trim()) issues.push(`${label}: state`);
      if (!addr.postalCode.trim()) issues.push(`${label}: ZIP code`);
      if (!addr.fromDate) issues.push(`${label}: from date`);
      if (!addr.isCurrent && !addr.toDate) issues.push(`${label}: to date`);
    });
    return issues;
  }, [formState.addresses]);

  const validateForm = useCallback(() => {
    return [...validatePersonalInfo(), ...validateAddresses()];
  }, [validatePersonalInfo, validateAddresses]);

  const buildAnswersPayload = useCallback(
    () => ({
      middleName: formState.middleName.trim() || null,
      ssn: formState.ssn.trim() || null,
      addresses: formState.addresses.map((a) => ({
        street1: a.street1.trim(),
        street2: a.street2.trim() || null,
        city: a.city.trim(),
        state: a.state.trim(),
        postalCode: a.postalCode.trim(),
        country: a.country || "USA",
        countyFips: a.countyFips.trim() || null,
        fromDate: a.fromDate || null,
        toDate: a.isCurrent ? null : a.toDate || null,
        type: a.type,
      })),
      employments: formState.employments.map((e) => ({
        employerName: e.employerName.trim(),
        employerPhone: e.employerPhone.trim() || null,
        employerAddress: e.employerAddress.trim() || null,
        jobTitle: e.jobTitle.trim() || null,
        supervisorName: e.supervisorName.trim() || null,
        supervisorPhone: e.supervisorPhone.trim() || null,
        startDate: e.startDate || null,
        endDate: e.isCurrent ? null : e.endDate || null,
        reasonForLeaving: e.reasonForLeaving.trim() || null,
        canContact: e.canContact,
      })),
      educations: formState.educations.map((e) => ({
        institutionName: e.institutionName.trim(),
        institutionAddress: e.institutionAddress.trim() || null,
        degree: e.degree.trim() || null,
        major: e.major.trim() || null,
        attendedFrom: e.attendedFrom || null,
        attendedTo: e.attendedTo || null,
        graduationDate: e.graduationDate || null,
        graduated: e.graduated,
      })),
      phones:
        formState.phones.length > 0
          ? formState.phones.map((p) => ({
              phoneNumber: p.phoneNumber.trim(),
              type: p.type,
              isPrimary: p.isPrimary,
            }))
          : [{ phoneNumber: formState.phone.trim(), type: 0, isPrimary: true }],
      references: formState.references.map((r) => ({
        name: r.name.trim(),
        phone: r.phone.trim() || null,
        email: r.email.trim() || null,
        relationship: r.relationship.trim() || null,
        yearsKnown: r.yearsKnown,
        type: r.type,
      })),
      authorization: {
        artifactId: authorizationReceipt?.artifactId ?? null,
        acceptedAt: authorizationReceipt?.createdAt ?? null,
      },
    }),
    [authorizationReceipt, formState],
  );

  const persistProgressSnapshot = useCallback(async () => {
    const answers = buildAnswersPayload();
    const serialized = JSON.stringify(answers);
    const payloadHash = await hashString(serialized);
    const payloadCipherText = toBase64(serialized);

    await saveProgressMutateAsync({
      resumeToken,
      schemaVersion: formSchemaVersion,
      payloadHash,
      payloadCipherText,
      updatedAt: new Date().toISOString(),
    });
  }, [buildAnswersPayload, resumeToken, saveProgressMutateAsync]);

  const applyBootstrap = useCallback((bootstrap: IntakeBootstrapResponse) => {
    setBootstrapError(null);
    setDisclosureError(null);

    // Store section configuration for policy-driven visibility
    if (bootstrap.sectionConfig) {
      setSectionConfig(bootstrap.sectionConfig);
    }

    setDisclosureSnapshot(bootstrap.disclosure ?? null);
    setAuthorizationCopy(bootstrap.authorizationCopy ?? null);
    setAuthorizationMode(bootstrap.authorizationMode ?? "one_time");

    if (!bootstrap.disclosure?.disclosureContent) {
      setDisclosureError(
        "We could not load the disclosure. Please refresh or contact support.",
      );
    }

    if (!bootstrap.authorizationCopy?.authorizationContent) {
      setAuthorizationError(
        "We could not load the authorization copy. Please refresh or contact support.",
      );
    }

    if (bootstrap.authorization) {
      setAuthorizationReceipt({
        artifactId: bootstrap.authorization.artifactId,
        mimeType: bootstrap.authorization.mimeType,
        length: bootstrap.authorization.length,
        hash: bootstrap.authorization.hash,
        hashAlgorithm: bootstrap.authorization.hashAlgorithm,
        schemaVersion: bootstrap.authorization.schemaVersion,
        createdAt: bootstrap.authorization.capturedAt,
      });
    }

    interface DecodedAnswers {
      middleName?: string;
      ssn?: string;
      addresses?: IntakeAddress[];
      employments?: IntakeEmployment[];
      educations?: IntakeEducation[];
      references?: IntakeReference[];
      phones?: IntakePhone[];
    }

    const decodeAnswers = (): DecodedAnswers | null => {
      if (!bootstrap.answers?.payloadCipherText) {
        return null;
      }
      try {
        const json = fromBase64(bootstrap.answers.payloadCipherText);
        return JSON.parse(json) as DecodedAnswers;
      } catch {
        setBootstrapError(
          "We could not load your saved answers. You can re-enter them.",
        );
        return null;
      }
    };

    const decoded = decodeAnswers();
    if (!decoded) {
      setFormState((prev) => ({
        ...prev,
        authorizationAccepted:
          prev.authorizationAccepted || Boolean(bootstrap.authorization),
      }));
      return;
    }

    setFormState((prev) => ({
      ...prev,
      middleName: decoded.middleName ?? prev.middleName,
      ssn: decoded.ssn ?? prev.ssn,
      phone: decoded.phones?.[0]?.phoneNumber ?? prev.phone,
      addresses: decoded.addresses?.length ? decoded.addresses : prev.addresses,
      employments: decoded.employments ?? prev.employments,
      educations: decoded.educations ?? prev.educations,
      references: decoded.references ?? prev.references,
      phones: decoded.phones?.length ? decoded.phones : prev.phones,
      authorizationAccepted:
        prev.authorizationAccepted || Boolean(bootstrap.authorization),
    }));
  }, []);

  const handleOtpVerification = useCallback(async () => {
    if (inviteParamsMissing) {
      setInviteError("This invite link is missing required details.");
      return false;
    }

    const sanitizedOtp = otpCode.trim();
    const otpIsValid = /^\d{6}$/.test(sanitizedOtp);
    if (otpIsValid === false) {
      setOtpError("Enter the 6-digit code we just sent you.");
      return false;
    }

    setInviteError(null);
    setOtpError(null);

    try {
      await verifyOtpMutateAsync(sanitizedOtp);
      await startSessionMutateAsync();
      const bootstrap = await bootstrapMutateAsync();
      applyBootstrap(bootstrap);
      setOtpVerified(true);
      return true;
    } catch (error) {
      const message = extractErrorMessage(error);
      setOtpError(message);
      return false;
    }
  }, [
    applyBootstrap,
    bootstrapMutateAsync,
    inviteParamsMissing,
    otpCode,
    startSessionMutateAsync,
    verifyOtpMutateAsync,
  ]);

  const verifyingOtp = isVerifyingOtp || isStartingSession || isBootstrapping;

  const handleDisclosureViewed = useCallback(async () => {
    if (disclosureViewed) {
      return true;
    }

    if (!disclosureSnapshot?.disclosureContent) {
      setDisclosureError(
        "We could not load the disclosure. Please refresh or contact support.",
      );
      return false;
    }

    setDisclosureError(null);

    try {
      await recordDisclosureViewedAsync();
      setDisclosureViewed(true);
      return true;
    } catch (error) {
      setDisclosureError(extractErrorMessage(error));
      return false;
    }
  }, [disclosureSnapshot, disclosureViewed, recordDisclosureViewedAsync]);

  const handleAuthorizationCapture = useCallback(async () => {
    if (authorizationReceipt) {
      return true;
    }

    if (!authorizationCopy?.authorizationContent) {
      setAuthorizationError(
        "We could not load the authorization copy. Please refresh or contact support.",
      );
      return false;
    }

    if (!formState.authorizationAccepted) {
      setAuthorizationError("Please confirm your authorization to continue.");
      return false;
    }

    setAuthorizationError(null);

    try {
      const receipt = await captureAuthorizationMutateAsync();
      setAuthorizationReceipt(receipt);
      return true;
    } catch (error) {
      setAuthorizationError(extractErrorMessage(error));
      return false;
    }
  }, [
    captureAuthorizationMutateAsync,
    authorizationCopy,
    authorizationReceipt,
    formState.authorizationAccepted,
  ]);

  const handleSubmit = useCallback(async () => {
    setSubmissionError(null);
    setProgressError(null);

    if (!otpVerified) {
      setSubmissionError("Verify the code before submitting.");
      return false;
    }

    const missingFields = validateForm();
    if (missingFields.length > 0) {
      setProgressError(`Add: ${missingFields.join(", ")}`);
      return false;
    }

    if (!authorizationReceipt) {
      const authorizationOk = await handleAuthorizationCapture();
      if (!authorizationOk) {
        return false;
      }
    }

    try {
      await persistProgressSnapshot();
      await submitIntakeMutateAsync();
      return true;
    } catch (error) {
      setSubmissionError(extractErrorMessage(error));
      return false;
    }
  }, [
    authorizationReceipt,
    handleAuthorizationCapture,
    persistProgressSnapshot,
    submitIntakeMutateAsync,
    validateForm,
    otpVerified,
  ]);

  const allSteps: StepDefinition[] = useMemo(
    () => [
      {
        id: "verify",
        title: "Confirm your invite",
        subtitle: "We will check the link and device fingerprint in seconds.",
        render: () => {
          let inviteErrorAlert: ReactNode = null;
          if (inviteError) {
            inviteErrorAlert = <Alert severity="error">{inviteError}</Alert>;
          }

          let inviteDetails: ReactNode;
          if (inviteParamsMissing) {
            inviteDetails = (
              <Alert severity="warning">
                This link is missing the session information required to begin.
                Ask the requester for a new invite.
              </Alert>
            );
          } else {
            inviteDetails = (
              <Paper
                variant="outlined"
                sx={{
                  p: 2,
                  backgroundColor: (theme) =>
                    alpha(theme.palette.primary.main, 0.04),
                }}
              >
                <Typography variant="body2" color="text.secondary">
                  Session reference: <strong>{maskValue(sessionId)}</strong>
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Secure token: <strong>{maskValue(resumeToken)}</strong>
                </Typography>
              </Paper>
            );
          }

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Your invite is unique"
                body="We pair this link with your order so nobody else can view or submit your information."
                helper="If anything looks off, stop and contact the requestor."
              />
              {inviteErrorAlert}
              {inviteDetails}
            </Stack>
          );
        },
        primaryCtaLabel: "Start",
        onPrimary: () => {
          if (inviteParamsMissing) {
            setInviteError("Missing invite parameters. Request a new link.");
            return false;
          }
          setInviteError(null);
          return true;
        },
        primaryButtonDisabled: inviteParamsMissing,
      },
      {
        id: "otp",
        title: "Verify with a code",
        subtitle: "A text message confirms you’re the right person.",
        render: () => {
          let otpErrorAlert: ReactNode = null;
          if (otpError) {
            otpErrorAlert = <Alert severity="error">{otpError}</Alert>;
          }

          let otpSuccessAlert: ReactNode = null;
          if (otpVerified) {
            otpSuccessAlert = (
              <Alert severity="success">
                Code verified. We’re unlocking the rest of your intake.
              </Alert>
            );
          }

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Enter the one-time code"
                body="We sent a 6-digit code to the phone number on file."
                helper="Codes expire after 5 minutes for your security."
              />
              {otpErrorAlert}
              {otpSuccessAlert}
              <TextField
                label="6-digit code"
                value={otpCode}
                onChange={(event) =>
                  setOtpCode(event.target.value.replace(/\D/g, "").slice(0, 6))
                }
                inputProps={{
                  inputMode: "numeric",
                  pattern: "[0-9]*",
                  maxLength: 6,
                }}
              />
            </Stack>
          );
        },
        primaryCtaLabel: verifyingOtp ? "Verifying..." : "Verify",
        onPrimary: handleOtpVerification,
        primaryButtonDisabled: verifyingOtp,
      },
      {
        id: "disclosure",
        title: "Disclosure",
        subtitle: "Review the standalone disclosure before continuing.",
        render: () => {
          const disclosureContent = disclosureSnapshot?.disclosureContent ?? "";
          const disclosureLines = disclosureContent.split("\n");
          const disclosureErrorAlert = disclosureError ? (
            <Alert severity="error">{disclosureError}</Alert>
          ) : null;
          const isHtmlDisclosure =
            disclosureSnapshot?.disclosureFormat === "html";

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Disclosure"
                body="Please read the disclosure below. This page is only the disclosure."
              />
              {disclosureErrorAlert}
              <Paper variant="outlined" sx={{ p: 2 }}>
                {isHtmlDisclosure ? (
                  <Box
                    sx={{ typography: "body2", color: "text.secondary" }}
                    dangerouslySetInnerHTML={{ __html: disclosureContent }}
                  />
                ) : (
                  <Stack spacing={1}>
                    {disclosureLines.map((line) => (
                      <Typography
                        key={line}
                        variant="body2"
                        color="text.secondary"
                      >
                        {line}
                      </Typography>
                    ))}
                  </Stack>
                )}
              </Paper>
            </Stack>
          );
        },
        primaryCtaLabel: isRecordingDisclosureViewed
          ? "Saving..."
          : "Continue",
        onPrimary: handleDisclosureViewed,
        primaryButtonDisabled:
          isRecordingDisclosureViewed || !disclosureSnapshot?.disclosureContent,
      },
      {
        id: "authorization",
        title: "Authorization",
        subtitle: "Provide written authorization for this background check.",
        render: () => {
          let authorizationStatus: ReactNode = null;
          if (authorizationReceipt) {
            authorizationStatus = (
              <Alert severity="success">
                Authorization saved at{" "}
                {new Date(authorizationReceipt.createdAt).toLocaleString()}.
              </Alert>
            );
          }
          const authorizationErrorAlert = authorizationError ? (
            <Alert severity="error">{authorizationError}</Alert>
          ) : null;
          const authorizationHelper =
            authorizationMode === "ongoing"
              ? "This authorization covers ongoing checks during your relationship."
              : "We will attach this authorization to your intake record.";
          const authorizationContent =
            authorizationCopy?.authorizationContent ?? "";
          const authorizationLines = authorizationContent.split("\n");
          const isHtmlAuthorization =
            authorizationCopy?.authorizationFormat === "html";

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Authorize this background check"
                body="Confirm you authorize Holmes to obtain a consumer report."
                helper={authorizationHelper}
              />
              <Paper variant="outlined" sx={{ p: 2 }}>
                {isHtmlAuthorization ? (
                  <Box
                    sx={{ typography: "body2", color: "text.secondary" }}
                    dangerouslySetInnerHTML={{ __html: authorizationContent }}
                  />
                ) : (
                  <Stack spacing={1}>
                    {authorizationLines.map((line) => (
                      <Typography
                        key={line}
                        variant="body2"
                        color="text.secondary"
                      >
                        {line}
                      </Typography>
                    ))}
                  </Stack>
                )}
              </Paper>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formState.authorizationAccepted}
                    onChange={(event) => {
                      updateField(
                        "authorizationAccepted",
                        event.target.checked,
                      );
                      setAuthorizationError(null);
                    }}
                  />
                }
                label="I authorize this background check."
              />
              {authorizationErrorAlert}
              {authorizationStatus}
            </Stack>
          );
        },
        primaryCtaLabel: isCapturingAuthorization
          ? "Saving..."
          : "Authorize & continue",
        onPrimary: handleAuthorizationCapture,
        primaryButtonDisabled:
          isCapturingAuthorization || !formState.authorizationAccepted,
      },
      {
        id: "personal",
        title: "Personal Information",
        subtitle:
          "We need your basic identity details for the background check.",
        render: () => {
          const bootstrapErrorAlert = bootstrapError ? (
            <Alert severity="warning">{bootstrapError}</Alert>
          ) : null;
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Share your identity details"
                body="We'll collect your legal name, date of birth, and contact information."
                helper="This information helps us verify your identity accurately."
              />
              {bootstrapErrorAlert}
              {progressErrorAlert}
              <Stack spacing={1.5}>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    label="First name"
                    value={formState.firstName}
                    onChange={(event) => {
                      updateField("firstName", event.target.value);
                      setProgressError(null);
                    }}
                    fullWidth
                    required
                  />
                  <TextField
                    label="Middle name (optional)"
                    value={formState.middleName}
                    onChange={(event) =>
                      updateField("middleName", event.target.value)
                    }
                    fullWidth
                  />
                  <TextField
                    label="Last name"
                    value={formState.lastName}
                    onChange={(event) => {
                      updateField("lastName", event.target.value);
                      setProgressError(null);
                    }}
                    fullWidth
                    required
                  />
                </Stack>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    type="date"
                    label="Date of birth"
                    value={formState.dateOfBirth}
                    onChange={(event) =>
                      updateField("dateOfBirth", event.target.value)
                    }
                    fullWidth
                    required
                    InputLabelProps={{ shrink: true }}
                  />
                  <TextField
                    label="Social Security Number"
                    value={formState.ssn}
                    onChange={(event) =>
                      updateField(
                        "ssn",
                        event.target.value.replace(/\D/g, "").slice(0, 9),
                      )
                    }
                    inputProps={{ inputMode: "numeric", maxLength: 9 }}
                    fullWidth
                    placeholder="Optional - helps with accuracy"
                    helperText="9 digits, no dashes. Used for identity verification only."
                  />
                </Stack>
                <Divider sx={{ my: 1 }} />
                <Typography variant="body2" color="text.secondary">
                  Contact Information
                </Typography>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    label="Email"
                    type="email"
                    value={formState.email}
                    onChange={(event) =>
                      updateField("email", event.target.value)
                    }
                    fullWidth
                    required
                  />
                  <TextField
                    label="Mobile phone"
                    value={formState.phone}
                    onChange={(event) =>
                      updateField(
                        "phone",
                        event.target.value.replace(/[^\d+]/g, "").slice(0, 20),
                      )
                    }
                    inputProps={{ inputMode: "tel" }}
                    fullWidth
                    required
                  />
                </Stack>
              </Stack>
            </Stack>
          );
        },
        primaryCtaLabel: "Continue",
        onPrimary: () => {
          const missingFields = validatePersonalInfo();
          if (missingFields.length > 0) {
            setProgressError(`Please add: ${missingFields.join(", ")}`);
            return false;
          }
          setProgressError(null);
          return true;
        },
      },
      {
        id: "phone",
        sectionId: "phone",
        title: "Phone Numbers",
        subtitle: "Provide your contact phone numbers.",
        render: () => {
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Your phone numbers"
                body="Add phone numbers where you can be reached."
                helper="Mark one as your primary contact number."
              />
              {progressErrorAlert}
              <PhoneForm
                phones={formState.phones}
                onChange={(phones) => {
                  updateField("phones", phones);
                  setProgressError(null);
                }}
                minPhones={1}
                maxPhones={5}
              />
            </Stack>
          );
        },
        primaryCtaLabel: "Continue",
        onPrimary: () => {
          if (
            formState.phones.length === 0 ||
            !formState.phones.some((p) => p.phoneNumber.trim())
          ) {
            setProgressError("Please add at least one phone number");
            return false;
          }
          setProgressError(null);
          return true;
        },
      },
      {
        id: "addresses",
        sectionId: "addresses",
        title: "Address History",
        subtitle: "We need your residential history for the past 7 years.",
        render: () => {
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              {progressErrorAlert}
              <AddressHistoryForm
                addresses={formState.addresses}
                onChange={(addresses) => {
                  updateField("addresses", addresses);
                  setProgressError(null);
                }}
                yearsRequired={7}
              />
            </Stack>
          );
        },
        primaryCtaLabel: "Continue",
        onPrimary: () => {
          const issues = validateAddresses();
          if (issues.length > 0) {
            setProgressError(
              `Please complete: ${issues.slice(0, 3).join(", ")}${issues.length > 3 ? "..." : ""}`,
            );
            return false;
          }
          setProgressError(null);
          return true;
        },
      },
      {
        id: "employment",
        sectionId: "employment",
        title: "Employment History",
        subtitle: "Tell us about your work experience.",
        render: () => {
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Your work history"
                body="List your employers for the past 7 years, starting with your current or most recent."
                helper="This helps verify your employment claims on your application."
              />
              {progressErrorAlert}
              <EmploymentHistoryForm
                employments={formState.employments}
                onChange={(employments) => {
                  updateField("employments", employments);
                  setProgressError(null);
                }}
                yearsRequired={7}
              />
            </Stack>
          );
        },
        primaryCtaLabel: "Continue",
        onPrimary: () => {
          setProgressError(null);
          return true;
        },
      },
      {
        id: "education",
        sectionId: "education",
        title: "Education History",
        subtitle: "Share your educational background.",
        render: () => {
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Your education"
                body="Add any schools, colleges, or training programs you've attended."
                helper="Only add institutions if education verification was requested."
              />
              {progressErrorAlert}
              <EducationHistoryForm
                educations={formState.educations}
                onChange={(educations) => {
                  updateField("educations", educations);
                  setProgressError(null);
                }}
              />
            </Stack>
          );
        },
        primaryCtaLabel: "Continue",
        onPrimary: () => {
          setProgressError(null);
          return true;
        },
      },
      {
        id: "references",
        sectionId: "references",
        title: "References",
        subtitle: "Provide contacts who can verify your experience.",
        render: () => {
          const progressErrorAlert = progressError ? (
            <Alert severity="error">{progressError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Your references"
                body="Add contacts who can speak to your work history and character."
                helper="Include both professional and personal references if requested."
              />
              {progressErrorAlert}
              <ReferenceForm
                references={formState.references}
                onChange={(references) => {
                  updateField("references", references);
                  setProgressError(null);
                }}
                minReferences={0}
                maxReferences={5}
              />
            </Stack>
          );
        },
        primaryCtaLabel: "Review",
        onPrimary: () => {
          setProgressError(null);
          return true;
        },
      },
      {
        id: "review",
        title: "Review & submit",
        subtitle: "Double-check everything before you send it.",
        render: () => {
          const summaryRow = (label: string, value: string | null) => (
            <Stack
              key={label}
              direction="row"
              justifyContent="space-between"
              spacing={2}
            >
              <Typography color="text.secondary">{label}</Typography>
              <Typography fontWeight={600}>{value || "—"}</Typography>
            </Stack>
          );

          const submissionErrorAlert = submissionError ? (
            <Alert severity="error">{submissionError}</Alert>
          ) : null;

          const fullName = [
            formState.firstName,
            formState.middleName,
            formState.lastName,
          ]
            .filter(Boolean)
            .join(" ");

          const currentAddress =
            formState.addresses.find((a) => a.isCurrent) ??
            (formState.addresses[0] as IntakeAddress | undefined);
          const addressDisplay = currentAddress
            ? `${currentAddress.street1}${currentAddress.street2 ? `, ${currentAddress.street2}` : ""}, ${currentAddress.city}, ${currentAddress.state} ${currentAddress.postalCode}`
            : "—";

          const currentEmployer =
            formState.employments.find((e) => e.isCurrent) ??
            (formState.employments[0] as IntakeEmployment | undefined);

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="One last look"
                body="We summarize your answers so you can fix any typos before submission."
                helper="Submitting hands your order to routing instantly."
              />
              {submissionErrorAlert}
              <Paper variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1.25}>
                  <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                    Personal Information
                  </Typography>
                  {summaryRow("Name", fullName)}
                  {summaryRow("Date of birth", formState.dateOfBirth)}
                  {summaryRow(
                    "SSN",
                    formState.ssn
                      ? `***-**-${formState.ssn.slice(-4)}`
                      : "Not provided",
                  )}
                  {summaryRow("Email", formState.email)}
                  {summaryRow("Phone", formState.phone)}
                  <Divider sx={{ my: 1 }} />
                  <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                    Address History
                  </Typography>
                  {summaryRow("Current/Primary", addressDisplay)}
                  {summaryRow(
                    "Total addresses",
                    `${formState.addresses.length}`,
                  )}
                  <Divider sx={{ my: 1 }} />
                  <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                    Employment & Education
                  </Typography>
                  {summaryRow(
                    "Current employer",
                    currentEmployer?.employerName || "Not provided",
                  )}
                  {summaryRow(
                    "Total employers",
                    `${formState.employments.length}`,
                  )}
                  {summaryRow(
                    "Education entries",
                    `${formState.educations.length}`,
                  )}
                  <Divider sx={{ my: 1 }} />
                  {summaryRow(
                    "Authorization",
                    authorizationReceipt ? "Saved" : "Pending",
                  )}
                </Stack>
              </Paper>
              <Typography variant="body2" color="text.secondary">
                By submitting, you confirm these details are accurate.
              </Typography>
            </Stack>
          );
        },
        primaryCtaLabel:
          isSubmittingIntake || isSavingProgress ? "Submitting..." : "Submit",
        onPrimary: handleSubmit,
        primaryButtonDisabled: isSubmittingIntake || isSavingProgress,
      },
      {
        id: "success",
        title: "All set",
        subtitle: "You may close this window now.",
        render: () => (
          <StepCopy
            heading="Thanks for finishing"
            body="Your background screening moves forward immediately."
            helper="You do not need to keep this tab open."
          />
        ),
        primaryCtaLabel: "Done",
        isTerminal: true,
      },
    ],
    [
      authorizationError,
      authorizationReceipt,
      authorizationCopy,
      authorizationMode,
      disclosureError,
      disclosureSnapshot,
      formState,
      handleDisclosureViewed,
      handleAuthorizationCapture,
      handleOtpVerification,
      handleSubmit,
      inviteError,
      inviteParamsMissing,
      isCapturingAuthorization,
      isRecordingDisclosureViewed,
      isSavingProgress,
      isSubmittingIntake,
      otpCode,
      otpError,
      otpVerified,
      bootstrapError,
      progressError,
      resumeToken,
      sessionId,
      submissionError,
      updateField,
      validateAddresses,
      validatePersonalInfo,
      verifyingOtp,
    ],
  );

  // Filter steps based on section visibility from ordered services
  const steps = useMemo(() => {
    return allSteps.filter((step) => {
      // Steps without sectionId are always visible (verify, otp, disclosure, authorization, personal, review, success)
      if (!step.sectionId) {
        return true;
      }
      // Steps with sectionId are visible only if the section is visible
      return isVisible(step.sectionId);
    });
  }, [allSteps, isVisible]);

  const activeStep = steps[activeIndex];
  const totalSteps = steps.length;

  const progress = useMemo(() => {
    if (activeIndex === 0) {
      return 0;
    }
    if (totalSteps <= 1) {
      return 100;
    }
    const pct = (activeIndex / (totalSteps - 1)) * 100;
    return Number(pct.toFixed(1));
  }, [activeIndex, totalSteps]);

  const canGoBack = activeIndex > 0 && !activeStep.isTerminal;

  const goNext = () => {
    if (activeIndex < totalSteps - 1) {
      setActiveIndex((index) => Math.min(index + 1, totalSteps - 1));
    }
  };

  const goBack = () => {
    if (canGoBack) {
      setActiveIndex((index) => Math.max(index - 1, 0));
    }
  };

  const resetFlow = () => {
    setActiveIndex(0);
    setOtpCode("");
    setOtpError(null);
    setOtpVerified(false);
    setAuthorizationReceipt(null);
    setAuthorizationError(null);
    setDisclosureSnapshot(null);
    setAuthorizationCopy(null);
    setAuthorizationMode(null);
    setDisclosureViewed(false);
    setDisclosureError(null);
    setProgressError(null);
    setSubmissionError(null);
    setFormState(initialFormState);
  };

  const handlePrimaryAction = async () => {
    if (activeStep.isTerminal) {
      resetFlow();
      return;
    }

    if (activeStep.onPrimary) {
      const shouldContinue = await activeStep.onPrimary();

      if (shouldContinue === false) {
        return;
      }
    }

    goNext();
  };

  const actionContent: ReactNode = activeStep.isTerminal ? (
    <Button
      fullWidth
      size="large"
      variant="contained"
      color="primary"
      onClick={resetFlow}
    >
      Restart intake
    </Button>
  ) : (
    <>
      <Button
        onClick={goBack}
        disabled={!canGoBack}
        fullWidth
        size="large"
        variant="outlined"
        sx={{
          color: (theme) => theme.palette.text.secondary,
          borderColor: (theme) => theme.palette.divider,
        }}
      >
        Back
      </Button>
      <Button
        fullWidth
        size="large"
        variant="contained"
        disabled={activeStep.primaryButtonDisabled}
        onClick={handlePrimaryAction}
        sx={{
          backgroundColor: (theme) => theme.palette.primary.main,
          "&:hover": {
            backgroundColor: (theme) => alpha(theme.palette.primary.main, 0.9),
          },
        }}
      >
        {activeStep.primaryCtaLabel ?? "Next"}
      </Button>
    </>
  );

  return (
    <Container maxWidth="sm" sx={{ py: { xs: 4, md: 8 } }}>
      <Paper elevation={0} sx={{ p: { xs: 3, md: 4 } }}>
        <Stack spacing={3}>
          <Box>
            <Typography
              sx={{
                textTransform: "uppercase",
                letterSpacing: 1.5,
                fontWeight: 600,
                fontSize: "0.75rem",
                color: (theme) => theme.palette.text.secondary,
              }}
            >
              Intake progress
            </Typography>
            <LinearProgress
              value={progress}
              variant="determinate"
              sx={{ mt: 1, height: 6, borderRadius: 999 }}
            />
            <Typography mt={1.5} variant="caption" color="text.secondary">
              Step {activeIndex + 1} of {steps.length}
            </Typography>
          </Box>

          <Stack spacing={1}>
            <Typography variant="h4">{activeStep.title}</Typography>
            <Typography color="text.secondary">
              {activeStep.subtitle}
            </Typography>
          </Stack>

          <Divider />

          <Box>{activeStep.render()}</Box>

          <Divider />

          <Stack
            direction={{ xs: "column-reverse", sm: "row" }}
            spacing={2}
            justifyContent="space-between"
          >
            {actionContent}
          </Stack>
        </Stack>
      </Paper>
    </Container>
  );
};

export default IntakeFlow;
