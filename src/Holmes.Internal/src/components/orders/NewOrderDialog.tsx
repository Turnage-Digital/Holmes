import React, { useState } from "react";

import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";

import type { CustomerListItemDto } from "@/types/api";

import {
  useCreateOrder,
  useCustomers,
  useIssueIntakeInvite,
  useRegisterSubject,
} from "@/hooks/api";
import { getErrorMessage } from "@/utils/errorMessage";

interface NewOrderDialogProps {
  open: boolean;
  onClose: () => void;
}

interface FormState {
  customerId: string;
  subjectEmail: string;
  subjectPhone: string;
}

const initialFormState: FormState = {
  customerId: "",
  subjectEmail: "",
  subjectPhone: "",
};

const NewOrderDialog = ({ open, onClose }: NewOrderDialogProps) => {
  const queryClient = useQueryClient();

  const [formState, setFormState] = useState<FormState>(initialFormState);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Fetch customers for dropdown
  const { data: customersData, isLoading: customersLoading } = useCustomers(
    1,
    100,
  );
  const customers = customersData?.items ?? [];

  // Mutations
  const registerSubjectMutation = useRegisterSubject();
  const createOrderMutation = useCreateOrder();
  const issueInviteMutation = useIssueIntakeInvite();

  const handleChange =
    (field: keyof FormState) =>
    (
      event:
        | React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
        | { target: { value: string } },
    ) => {
      setFormState((prev) => ({
        ...prev,
        [field]: event.target.value,
      }));
      setError(null);
    };

  const validateForm = (): string | null => {
    if (!formState.customerId) {
      return "Please select a customer.";
    }
    if (!formState.subjectEmail.trim()) {
      return "Subject email is required.";
    }
    if (!formState.subjectEmail.includes("@")) {
      return "Please enter a valid email address.";
    }
    if (!formState.subjectPhone.trim()) {
      return "Subject phone is required for OTP verification.";
    }
    return null;
  };

  const handleSubmit = async () => {
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      // Get the selected customer to access policySnapshotId
      const selectedCustomer = customers.find(
        (c) => c.id === formState.customerId,
      );
      if (!selectedCustomer) {
        throw new Error("Selected customer not found.");
      }

      // Step 1: Register the subject (minimal info - intake will collect the rest)
      // givenName and familyName will be populated during intake
      const subject = await registerSubjectMutation.mutateAsync({
        givenName: "",
        familyName: "",
        email: formState.subjectEmail.trim(),
      });

      // Step 2: Create the order
      const order = await createOrderMutation.mutateAsync({
        customerId: formState.customerId,
        subjectId: subject.subjectId,
        policySnapshotId: selectedCustomer.policySnapshotId,
      });

      // Step 3: Issue the intake invite
      await issueInviteMutation.mutateAsync({
        orderId: order.orderId,
        subjectId: subject.subjectId,
        customerId: formState.customerId,
        policySnapshotId: selectedCustomer.policySnapshotId,
      });

      // Invalidate queries to refresh data
      await queryClient.invalidateQueries({ queryKey: ["orders"] });

      // Reset and close
      setFormState(initialFormState);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setFormState(initialFormState);
      setError(null);
      onClose();
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>New Order</DialogTitle>
      <DialogContent>
        <Stack spacing={3} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <FormControl fullWidth>
            <InputLabel id="customer-select-label">Customer</InputLabel>
            <Select
              labelId="customer-select-label"
              value={formState.customerId}
              label="Customer"
              onChange={(e) =>
                handleChange("customerId")({
                  target: { value: e.target.value },
                })
              }
              disabled={customersLoading || isSubmitting}
            >
              {customers.map((customer: CustomerListItemDto) => (
                <MenuItem key={customer.id} value={customer.id}>
                  {customer.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label="Subject Email"
            type="email"
            value={formState.subjectEmail}
            onChange={handleChange("subjectEmail")}
            disabled={isSubmitting}
            fullWidth
            required
            helperText="The subject will receive an invite at this address."
          />

          <TextField
            label="Subject Phone"
            type="tel"
            value={formState.subjectPhone}
            onChange={handleChange("subjectPhone")}
            disabled={isSubmitting}
            fullWidth
            required
            helperText="Used for OTP verification during intake."
          />

          <Typography variant="body2" color="text.secondary">
            The subject will receive an invite to complete their intake, provide
            consent, and verify their information.
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={handleClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={isSubmitting || customersLoading}
        >
          {isSubmitting && <>Creating…</>}
          {!isSubmitting && <>Create &amp; Send Invite</>}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default NewOrderDialog;
