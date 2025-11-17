import React, {ReactNode, useCallback, useMemo, useState} from "react";

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
import {alpha} from "@mui/material/styles";
import {useMutation} from "@tanstack/react-query";

import {ApiError} from "@/lib/api";
import {startIntakeSession, verifyIntakeOtp} from "@/services/intake";

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
        return {sessionId: "", resumeToken: ""};
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
    const hasInviteParams = Boolean(sessionId && resumeToken);
    const inviteParamsMissing = hasInviteParams === false;
    const deviceInfo =
        typeof navigator === "undefined" ? "intake-client" : navigator.userAgent;

    const [inviteError, setInviteError] = useState<string | null>(null);
    const [otpCode, setOtpCode] = useState("");
    const [otpError, setOtpError] = useState<string | null>(null);
    const [otpVerified, setOtpVerified] = useState(false);

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

        return {sessionId, resumeToken};
    };

    const {mutateAsync: verifyOtpMutateAsync, isPending: isVerifyingOtp} =
        useMutation({
            mutationFn: (code: string) =>
                verifyIntakeOtp(requireSessionId(), {code}),
        });

    const {mutateAsync: startSessionMutateAsync, isPending: isStartingSession} =
        useMutation({
            mutationFn: () => {
                const {sessionId: safeSessionId, resumeToken: safeToken} =
                    requireInviteParams();
                return startIntakeSession(safeSessionId, {
                    resumeToken: safeToken,
                    deviceInfo,
                    startedAt: new Date().toISOString(),
                });
            },
        });

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
            setOtpVerified(true);
            return true;
        } catch (error) {
            const message = extractErrorMessage(error);
            setOtpError(message);
            return false;
        }
    }, [
        inviteParamsMissing,
        otpCode,
        startSessionMutateAsync,
        verifyOtpMutateAsync,
    ]);

    const verifyingOtp = isVerifyingOtp || isStartingSession;

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
                                body="We’ll send a 6-digit code to the phone number on file."
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
                render: () => (
                    <StepCopy
                        heading="Disclosures in plain language"
                        body="You’ll see the exact documents your employer uses for background checks."
                        helper="You can download a copy after agreeing."
                    />
                ),
                primaryCtaLabel: "Agree & continue",
            },
            {
                id: "data",
                title: "Provide required info",
                subtitle: "We only ask for what the check needs—nothing more.",
                render: () => (
                    <StepCopy
                        heading="Share the basics"
                        body="We’ll collect your legal name, date of birth, and current city."
                        helper="The entire form fits on one screen."
                    />
                ),
                primaryCtaLabel: "Review",
            },
            {
                id: "review",
                title: "Review & submit",
                subtitle: "Double-check everything before you send it.",
                render: () => (
                    <StepCopy
                        heading="One last look"
                        body="We summarize your answers so you can fix any typos before submission."
                        helper="Submitting hands your order to routing instantly."
                    />
                ),
                primaryCtaLabel: "Submit",
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
            handleOtpVerification,
            inviteError,
            inviteParamsMissing,
            otpCode,
            otpError,
            otpVerified,
            resumeToken,
            sessionId,
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
            Restart demo
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
        <Container maxWidth="sm" sx={{py: {xs: 4, md: 8}}}>
            <Paper elevation={0} sx={{p: {xs: 3, md: 4}}}>
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
                            sx={{mt: 1, height: 6, borderRadius: 999}}
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

                    <Divider/>

                    <Box>{activeStep.render()}</Box>

                    <Divider/>

                    <Stack
                        direction={{xs: "column-reverse", sm: "row"}}
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
