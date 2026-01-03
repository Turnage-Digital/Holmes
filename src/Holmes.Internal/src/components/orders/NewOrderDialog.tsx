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
  Typography
} from "@mui/material";

import type { CustomerListItemDto } from "@/types/api";

import { useCreateOrderWithIntake, useCustomers } from "@/hooks/api";
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
  subjectPhone: ""
};

const NewOrderDialog = ({ open, onClose }: NewOrderDialogProps) => {
  const [formState, setFormState] = useState<FormState>(initialFormState);
  const [error, setError] = useState<string | null>(null);

  // Fetch customers for dropdown
  const { data: customersData, isLoading: customersLoading } = useCustomers(
    1,
    100
  );
  const customers = customersData?.items ?? [];

  // Single mutation for creating order with intake
  const createOrderWithIntakeMutation = useCreateOrderWithIntake();
  const isSubmitting = createOrderWithIntakeMutation.isPending;

  const handleChange =
    (field: keyof FormState) =>
      (
        event:
          | React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
          | { target: { value: string } }
      ) => {
        setFormState((prev) => ({
          ...prev,
          [field]: event.target.value
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

    setError(null);

    try {
      // Get the selected customer to access policySnapshotId
      const selectedCustomer = customers.find(
        (c) => c.id === formState.customerId
      );
      if (!selectedCustomer) {
        throw new Error("Selected customer not found.");
      }

      // Single API call creates subject (or reuses existing), order, and intake session
      await createOrderWithIntakeMutation.mutateAsync({
        subjectEmail: formState.subjectEmail.trim(),
        subjectPhone: formState.subjectPhone.trim() || undefined,
        customerId: formState.customerId,
        policySnapshotId: selectedCustomer.policySnapshotId
      });

      // Reset and close
      setFormState(initialFormState);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err));
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
                  target: { value: e.target.value }
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
