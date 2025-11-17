import React, { ReactNode, useMemo, useState } from "react";

import {
  Box,
  Button,
  Container,
  Divider,
  LinearProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";

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
  Component: () => ReactNode;
  primaryCtaLabel?: string;
  isTerminal?: boolean;
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

const steps: StepDefinition[] = [
  {
    id: "verify",
    title: "Confirm your invite",
    subtitle: "We will check the link and device fingerprint in seconds.",
    Component: () => (
      <StepCopy
        heading="Your invite is unique"
        body="We pair this link with your order so nobody else can view or submit your information."
        helper="If anything looks off, stop and contact the requestor."
      />
    ),
    primaryCtaLabel: "Start",
  },
  {
    id: "otp",
    title: "Verify with a code",
    subtitle: "A text message confirms you’re the right person.",
    Component: () => (
      <StepCopy
        heading="Enter the one-time code"
        body="We’ll send a 6-digit code to the phone number on file."
        helper="Codes expire after 5 minutes for your security."
      />
    ),
    primaryCtaLabel: "Verify",
  },
  {
    id: "consent",
    title: "Consent & Disclosures",
    subtitle: "Review standard disclosures and acknowledge consent.",
    Component: () => (
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
    Component: () => (
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
    Component: () => (
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
    Component: () => (
      <StepCopy
        heading="Thanks for finishing"
        body="Your background screening moves forward immediately."
        helper="You do not need to keep this tab open."
      />
    ),
    primaryCtaLabel: "Done",
    isTerminal: true,
  },
];

const IntakeFlow = () => {
  const [activeIndex, setActiveIndex] = useState(0);

  const activeStep = steps[activeIndex];

  const progress = useMemo(() => {
    if (activeIndex === 0) {
      return 0;
    }
    const pct = (activeIndex / (steps.length - 1)) * 100;
    return Number(pct.toFixed(1));
  }, [activeIndex]);

  const canGoBack = activeIndex > 0 && !activeStep.isTerminal;

  const goNext = () => {
    if (activeIndex < steps.length - 1) {
      setActiveIndex((index) => Math.min(index + 1, steps.length - 1));
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
        onClick={goNext}
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

          <Box>
            <activeStep.Component />
          </Box>

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
