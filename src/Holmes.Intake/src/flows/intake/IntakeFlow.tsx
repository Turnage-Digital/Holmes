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

import { fromBase64, hashString, toBase64 } from "@/lib/crypto";
import {
  captureConsentArtifact,
  getIntakeBootstrap,
  saveIntakeProgress,
  startIntakeSession,
  submitIntake,
  verifyIntakeOtp,
} from "@/services/intake";
import {
  CaptureConsentResponse,
  IntakeBootstrapResponse,
  SaveIntakeProgressRequest,
} from "@/types/api";

type IntakeStepId =
  | "verify"
  | "otp"
  | "consent"
  | "data"
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
}

interface IntakeFormState {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
  ssnLast4: string;
  consentAccepted: boolean;
}

const formSchemaVersion = "intake-basic-v1";
const consentSchemaVersion = "consent-basic-v1";

const initialFormState: IntakeFormState = {
  firstName: "",
  lastName: "",
  dateOfBirth: "",
  email: "",
  phone: "",
  addressLine1: "",
  addressLine2: "",
  city: "",
  region: "",
  postalCode: "",
  ssnLast4: "",
  consentAccepted: false,
};

const CONSENT_TEXT = `I authorize Holmes to obtain and share consumer reports for employment purposes.
I acknowledge I have received and reviewed the disclosure describing this process.
This authorization remains valid for this background check request and may be revoked in writing.`;

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
  const [consentReceipt, setConsentReceipt] =
    useState<CaptureConsentResponse | null>(null);
  const [consentError, setConsentError] = useState<string | null>(null);
  const [progressError, setProgressError] = useState<string | null>(null);
  const [submissionError, setSubmissionError] = useState<string | null>(null);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
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
      (["firstName", "lastName", "email", "phone", "city", "region"] as const)
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
    mutateAsync: captureConsentMutateAsync,
    isPending: isCapturingConsent,
  } = useMutation({
    mutationFn: () =>
      captureConsentArtifact(requireSessionId(), {
        mimeType: "text/plain",
        schemaVersion: consentSchemaVersion,
        payloadBase64: toBase64(CONSENT_TEXT),
        capturedAt: new Date().toISOString(),
        metadata: {
          source: "intake-ui",
          variant: "phase-2",
        },
      }),
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

  const updateField = useCallback(
    <K extends keyof IntakeFormState>(key: K, value: IntakeFormState[K]) =>
      setFormState((prev) => ({ ...prev, [key]: value })),
    [],
  );

  const validateForm = useCallback(() => {
    const missingFields: string[] = [];
    if (!formState.firstName.trim()) missingFields.push("First name");
    if (!formState.lastName.trim()) missingFields.push("Last name");
    if (!formState.dateOfBirth.trim()) missingFields.push("Date of birth");
    if (!formState.email.trim()) missingFields.push("Email");
    if (!formState.phone.trim()) missingFields.push("Phone");
    if (!formState.addressLine1.trim()) missingFields.push("Street");
    if (!formState.city.trim()) missingFields.push("City");
    if (!formState.region.trim()) missingFields.push("State");
    if (!formState.postalCode.trim()) missingFields.push("Postal code");

    return missingFields;
  }, [formState]);

  const buildAnswersPayload = useCallback(
    () => ({
      subject: {
        firstName: formState.firstName.trim(),
        lastName: formState.lastName.trim(),
        dateOfBirth: formState.dateOfBirth.trim(),
        ssnLast4: formState.ssnLast4.trim(),
      },
      contact: {
        email: formState.email.trim(),
        phone: formState.phone.trim(),
        addressLine1: formState.addressLine1.trim(),
        addressLine2: formState.addressLine2.trim(),
        city: formState.city.trim(),
        region: formState.region.trim(),
        postalCode: formState.postalCode.trim(),
      },
      consent: {
        artifactId: consentReceipt?.artifactId ?? null,
        acceptedAt: consentReceipt?.createdAt ?? null,
      },
    }),
    [consentReceipt, formState],
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

    if (bootstrap.consent) {
      setConsentReceipt({
        artifactId: bootstrap.consent.artifactId,
        mimeType: bootstrap.consent.mimeType,
        length: bootstrap.consent.length,
        hash: bootstrap.consent.hash,
        hashAlgorithm: bootstrap.consent.hashAlgorithm,
        schemaVersion: bootstrap.consent.schemaVersion,
        createdAt: bootstrap.consent.capturedAt,
      });
    }

    const decodeAnswers = () => {
      if (!bootstrap.answers?.payloadCipherText) {
        return null;
      }
      try {
        const json = fromBase64(bootstrap.answers.payloadCipherText);
        return JSON.parse(json) as {
          subject?: Partial<IntakeFormState>;
          contact?: Partial<IntakeFormState>;
        };
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
        consentAccepted: prev.consentAccepted || Boolean(bootstrap.consent),
      }));
      return;
    }

    setFormState((prev) => ({
      ...prev,
      firstName: decoded.subject?.firstName ?? prev.firstName,
      lastName: decoded.subject?.lastName ?? prev.lastName,
      dateOfBirth: decoded.subject?.dateOfBirth ?? prev.dateOfBirth,
      ssnLast4: decoded.subject?.ssnLast4 ?? prev.ssnLast4,
      email: decoded.contact?.email ?? prev.email,
      phone: decoded.contact?.phone ?? prev.phone,
      addressLine1: decoded.contact?.addressLine1 ?? prev.addressLine1,
      addressLine2: decoded.contact?.addressLine2 ?? prev.addressLine2,
      city: decoded.contact?.city ?? prev.city,
      region: decoded.contact?.region ?? prev.region,
      postalCode: decoded.contact?.postalCode ?? prev.postalCode,
      consentAccepted: prev.consentAccepted || Boolean(bootstrap.consent),
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

  const handleConsentCapture = useCallback(async () => {
    if (consentReceipt) {
      return true;
    }

    if (!formState.consentAccepted) {
      setConsentError("Please confirm you have read and agree to continue.");
      return false;
    }

    setConsentError(null);

    try {
      const receipt = await captureConsentMutateAsync();
      setConsentReceipt(receipt);
      return true;
    } catch (error) {
      setConsentError(extractErrorMessage(error));
      return false;
    }
  }, [captureConsentMutateAsync, consentReceipt, formState.consentAccepted]);

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

    if (!consentReceipt) {
      const consentOk = await handleConsentCapture();
      if (!consentOk) {
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
    consentReceipt,
    handleConsentCapture,
    persistProgressSnapshot,
    submitIntakeMutateAsync,
    validateForm,
    otpVerified,
  ]);

  const steps: StepDefinition[] = useMemo(
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
        id: "consent",
        title: "Consent & Disclosures",
        subtitle: "Review standard disclosures and acknowledge consent.",
        render: () => {
          let consentStatus: ReactNode = null;
          if (consentReceipt) {
            consentStatus = (
              <Alert severity="success">
                Consent saved at{" "}
                {new Date(consentReceipt.createdAt).toLocaleString()}.
              </Alert>
            );
          }
          const consentErrorAlert = consentError ? (
            <Alert severity="error">{consentError}</Alert>
          ) : null;

          return (
            <Stack spacing={2}>
              <StepCopy
                heading="Disclosures in plain language"
                body="Review the summary below and confirm you agree to continue."
                helper="We will attach this acceptance to your intake record."
              />
              <Paper variant="outlined" sx={{ p: 2 }}>
                <Stack spacing={1}>
                  {CONSENT_TEXT.split("\n").map((line) => (
                    <Typography
                      key={line}
                      variant="body2"
                      color="text.secondary"
                    >
                      {line}
                    </Typography>
                  ))}
                </Stack>
              </Paper>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formState.consentAccepted}
                    onChange={(event) => {
                      updateField("consentAccepted", event.target.checked);
                      setConsentError(null);
                    }}
                  />
                }
                label="I have read the disclosure and authorize this background check."
              />
              {consentErrorAlert}
              {consentStatus}
            </Stack>
          );
        },
        primaryCtaLabel: isCapturingConsent ? "Saving..." : "Agree & continue",
        onPrimary: handleConsentCapture,
        primaryButtonDisabled: isCapturingConsent,
      },
      {
        id: "data",
        title: "Provide required info",
        subtitle: "We only ask for what the check needs—nothing more.",
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
                heading="Share the basics"
                body="We’ll collect your legal name, date of birth, and current city."
                helper="This form stays lightweight and should take less than a minute."
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
                  />
                  <TextField
                    label="Last name"
                    value={formState.lastName}
                    onChange={(event) => {
                      updateField("lastName", event.target.value);
                      setProgressError(null);
                    }}
                    fullWidth
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
                    InputLabelProps={{ shrink: true }}
                  />
                  <TextField
                    label="SSN (last 4, optional)"
                    value={formState.ssnLast4}
                    onChange={(event) =>
                      updateField(
                        "ssnLast4",
                        event.target.value.replace(/\D/g, "").slice(0, 4),
                      )
                    }
                    inputProps={{ inputMode: "numeric", maxLength: 4 }}
                    fullWidth
                  />
                </Stack>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    label="Email"
                    type="email"
                    value={formState.email}
                    onChange={(event) =>
                      updateField("email", event.target.value)
                    }
                    fullWidth
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
                  />
                </Stack>
                <TextField
                  label="Street address"
                  value={formState.addressLine1}
                  onChange={(event) =>
                    updateField("addressLine1", event.target.value)
                  }
                  fullWidth
                />
                <TextField
                  label="Apartment, suite (optional)"
                  value={formState.addressLine2}
                  onChange={(event) =>
                    updateField("addressLine2", event.target.value)
                  }
                  fullWidth
                />
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    label="City"
                    value={formState.city}
                    onChange={(event) =>
                      updateField("city", event.target.value)
                    }
                    fullWidth
                  />
                  <TextField
                    label="State"
                    value={formState.region}
                    onChange={(event) =>
                      updateField("region", event.target.value)
                    }
                    fullWidth
                  />
                  <TextField
                    label="Postal code"
                    value={formState.postalCode}
                    onChange={(event) =>
                      updateField(
                        "postalCode",
                        event.target.value
                          .replace(/[^\dA-Za-z- ]/g, "")
                          .slice(0, 10),
                      )
                    }
                    fullWidth
                  />
                </Stack>
              </Stack>
            </Stack>
          );
        },
        primaryCtaLabel: "Review",
        onPrimary: () => {
          const missingFields = validateForm();
          if (missingFields.length > 0) {
            setProgressError(`Add: ${missingFields.join(", ")}`);
            return false;
          }
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

          const answers = buildAnswersPayload();
          const submissionErrorAlert = submissionError ? (
            <Alert severity="error">{submissionError}</Alert>
          ) : null;

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
                  {summaryRow(
                    "Name",
                    `${answers.subject.firstName} ${answers.subject.lastName}`.trim(),
                  )}
                  {summaryRow("Date of birth", answers.subject.dateOfBirth)}
                  {summaryRow("Email", answers.contact.email)}
                  {summaryRow("Mobile", answers.contact.phone)}
                  {summaryRow(
                    "Address",
                    `${answers.contact.addressLine1}${
                      answers.contact.addressLine2
                        ? `, ${answers.contact.addressLine2}`
                        : ""
                    }, ${answers.contact.city}, ${answers.contact.region} ${answers.contact.postalCode}`,
                  )}
                  {summaryRow("Consent", consentReceipt ? "Saved" : "Pending")}
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
      consentError,
      consentReceipt,
      formState,
      handleConsentCapture,
      handleOtpVerification,
      handleSubmit,
      inviteError,
      inviteParamsMissing,
      isCapturingConsent,
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
      buildAnswersPayload,
      validateForm,
      verifyingOtp,
    ],
  );

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
    setConsentReceipt(null);
    setConsentError(null);
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
