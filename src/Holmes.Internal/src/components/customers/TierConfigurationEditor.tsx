import React, { useState } from "react";

import AutorenewIcon from "@mui/icons-material/Autorenew";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import PauseCircleIcon from "@mui/icons-material/PauseCircle";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Chip,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { DataGrid, GridColDef, GridRenderCellParams } from "@mui/x-data-grid";

import type {
  CatalogServiceItemDto,
  TierConfigurationDto,
  Ulid,
} from "@/types/api";

import { useUpdateTierConfiguration } from "@/hooks/api";
import { getErrorMessage } from "@/utils/errorMessage";

// ============================================================================
// Tier Card Component
// ============================================================================

interface TierCardProps {
  tier: TierConfigurationDto;
  availableServices: CatalogServiceItemDto[];
  customerId: Ulid;
  defaultExpanded?: boolean;
}

const TierCard = ({
  tier,
  availableServices,
  customerId,
  defaultExpanded = false,
}: TierCardProps) => {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const [localName, setLocalName] = useState(tier.name);
  const [localDescription, setLocalDescription] = useState(
    tier.description ?? "",
  );
  const [errorMessage, setErrorMessage] = useState<string>();
  const [successMessage, setSuccessMessage] = useState<string>();

  const updateMutation = useUpdateTierConfiguration();

  // Get service display names by code
  const serviceNameMap = new Map(
    availableServices.map((s) => [s.serviceTypeCode, s.displayName]),
  );

  const handleUpdate = async (
    updates: Partial<{
      name: string;
      description: string;
      autoDispatch: boolean;
      waitForPreviousTier: boolean;
    }>,
  ) => {
    setErrorMessage(undefined);
    setSuccessMessage(undefined);

    try {
      await updateMutation.mutateAsync({
        customerId,
        payload: {
          tier: tier.tier,
          name: updates.name ?? tier.name,
          description: updates.description ?? tier.description,
          autoDispatch: updates.autoDispatch ?? tier.autoDispatch,
          waitForPreviousTier:
            updates.waitForPreviousTier ?? tier.waitForPreviousTier,
          requiredServices: tier.requiredServices,
          optionalServices: tier.optionalServices,
        },
      });
      setSuccessMessage("Configuration saved");
      setTimeout(() => setSuccessMessage(undefined), 3000);
    } catch (err) {
      setErrorMessage(getErrorMessage(err));
    }
  };

  const handleNameBlur = () => {
    if (localName !== tier.name && localName.trim()) {
      handleUpdate({ name: localName.trim() });
    }
  };

  const handleDescriptionBlur = () => {
    if (localDescription !== (tier.description ?? "")) {
      handleUpdate({ description: localDescription.trim() || undefined });
    }
  };

  // Service columns for grid
  const serviceColumns: GridColDef[] = [
    {
      field: "code",
      headerName: "Service Code",
      flex: 1,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
          {params.value}
        </Typography>
      ),
    },
    {
      field: "displayName",
      headerName: "Name",
      flex: 1.5,
    },
    {
      field: "type",
      headerName: "Type",
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          color={params.value === "Required" ? "primary" : "default"}
          variant="outlined"
        />
      ),
    },
  ];

  // Combine required and optional services for the grid
  const serviceRows = [
    ...tier.requiredServices.map((code) => ({
      id: `req-${code}`,
      code,
      displayName: serviceNameMap.get(code) ?? code,
      type: "Required",
    })),
    ...tier.optionalServices.map((code) => ({
      id: `opt-${code}`,
      code,
      displayName: serviceNameMap.get(code) ?? code,
      type: "Optional",
    })),
  ];

  const totalServices =
    tier.requiredServices.length + tier.optionalServices.length;

  return (
    <Accordion
      expanded={expanded}
      onChange={() => setExpanded(!expanded)}
      variant="outlined"
      sx={{ mb: 1 }}
    >
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          sx={{ width: "100%" }}
        >
          <Chip
            label={`Tier ${tier.tier}`}
            color="primary"
            size="small"
            sx={{ fontWeight: 600, minWidth: 70 }}
          />
          <Typography variant="subtitle1" sx={{ flexGrow: 1 }}>
            {tier.name}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Tooltip
              title={
                tier.autoDispatch ? "Auto-dispatch enabled" : "Manual dispatch"
              }
            >
              <Chip
                icon={
                  tier.autoDispatch ? <AutorenewIcon /> : <PlayCircleIcon />
                }
                label={tier.autoDispatch ? "Auto" : "Manual"}
                size="small"
                variant="outlined"
                color={tier.autoDispatch ? "success" : "default"}
              />
            </Tooltip>
            {tier.tier > 1 && (
              <Tooltip
                title={
                  tier.waitForPreviousTier
                    ? "Waits for previous tier"
                    : "Parallel execution"
                }
              >
                <Chip
                  icon={
                    tier.waitForPreviousTier ? (
                      <PauseCircleIcon />
                    ) : (
                      <PlayCircleIcon />
                    )
                  }
                  label={tier.waitForPreviousTier ? "Sequential" : "Parallel"}
                  size="small"
                  variant="outlined"
                />
              </Tooltip>
            )}
            <Chip
              label={`${totalServices} service${totalServices !== 1 ? "s" : ""}`}
              size="small"
              variant="outlined"
            />
          </Stack>
        </Stack>
      </AccordionSummary>
      <AccordionDetails>
        <Stack spacing={3}>
          {/* Alerts */}
          {successMessage && (
            <Alert
              severity="success"
              onClose={() => setSuccessMessage(undefined)}
            >
              {successMessage}
            </Alert>
          )}
          {errorMessage && (
            <Alert severity="error" onClose={() => setErrorMessage(undefined)}>
              {errorMessage}
            </Alert>
          )}

          {/* Settings */}
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" },
              gap: 2,
            }}
          >
            <TextField
              label="Tier Name"
              value={localName}
              onChange={(e) => setLocalName(e.target.value)}
              onBlur={handleNameBlur}
              size="small"
              fullWidth
            />
            <TextField
              label="Description"
              value={localDescription}
              onChange={(e) => setLocalDescription(e.target.value)}
              onBlur={handleDescriptionBlur}
              size="small"
              fullWidth
              placeholder="Optional description"
            />
          </Box>

          {/* Toggles */}
          <Stack direction="row" spacing={4}>
            <FormControlLabel
              control={
                <Switch
                  checked={tier.autoDispatch}
                  onChange={(e) =>
                    handleUpdate({ autoDispatch: e.target.checked })
                  }
                  disabled={updateMutation.isPending}
                />
              }
              label={
                <Box>
                  <Typography variant="body2">Auto-dispatch</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Automatically dispatch services when tier becomes active
                  </Typography>
                </Box>
              }
            />
            {tier.tier > 1 && (
              <FormControlLabel
                control={
                  <Switch
                    checked={tier.waitForPreviousTier}
                    onChange={(e) =>
                      handleUpdate({ waitForPreviousTier: e.target.checked })
                    }
                    disabled={updateMutation.isPending}
                  />
                }
                label={
                  <Box>
                    <Typography variant="body2">
                      Wait for Previous Tier
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Require Tier {tier.tier - 1} to complete before starting
                    </Typography>
                  </Box>
                }
              />
            )}
          </Stack>

          {/* Services Grid */}
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Services in this Tier
            </Typography>
            {serviceRows.length === 0 ? (
              <Typography color="text.secondary" variant="body2">
                No services configured for this tier.
              </Typography>
            ) : (
              <DataGrid
                rows={serviceRows}
                columns={serviceColumns}
                autoHeight
                density="compact"
                disableRowSelectionOnClick
                hideFooter
                sx={{ minHeight: 100 }}
              />
            )}
          </Box>
        </Stack>
      </AccordionDetails>
    </Accordion>
  );
};

// ============================================================================
// Tier Configuration Editor
// ============================================================================

interface TierConfigurationEditorProps {
  customerId: Ulid;
  tiers: TierConfigurationDto[];
  availableServices: CatalogServiceItemDto[];
}

const TierConfigurationEditor = ({
  customerId,
  tiers,
  availableServices,
}: TierConfigurationEditorProps) => {
  // Sort tiers by tier number
  const sortedTiers = [...tiers].sort((a, b) => a.tier - b.tier);

  // Count total services
  const totalConfigured = tiers.reduce(
    (acc, t) => acc + t.requiredServices.length + t.optionalServices.length,
    0,
  );

  return (
    <Stack spacing={3}>
      {/* Header */}
      <Box>
        <Typography variant="h6">Tier Configuration</Typography>
        <Typography variant="body2" color="text.secondary">
          {tiers.length} tiers with {totalConfigured} services configured
        </Typography>
      </Box>

      {/* Info */}
      <Alert severity="info" variant="outlined">
        <Typography variant="body2">
          Tiers control the order in which background check services are
          executed. Lower tiers (1-2) typically contain identity verification
          and criminal searches, while higher tiers (3-4) contain employment
          verification and specialized checks.
        </Typography>
      </Alert>

      {/* Tier Cards */}
      {sortedTiers.length === 0 ? (
        <Alert severity="warning">
          No tiers configured. Contact support to set up the tier structure.
        </Alert>
      ) : (
        sortedTiers.map((tier, index) => (
          <TierCard
            key={tier.tier}
            tier={tier}
            availableServices={availableServices}
            customerId={customerId}
            defaultExpanded={index === 0}
          />
        ))
      )}
    </Stack>
  );
};

export default TierConfigurationEditor;
