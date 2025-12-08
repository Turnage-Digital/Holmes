import React, { useMemo, useState } from "react";

import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import FilterListIcon from "@mui/icons-material/FilterList";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography
} from "@mui/material";

import type { CatalogServiceItemDto, ServiceCategory, Ulid } from "@/types/api";

import { useUpdateCatalogService } from "@/hooks/api";
import { getErrorMessage } from "@/utils/errorMessage";

// ============================================================================
// Category Chip Component
// ============================================================================

const categoryColors: Record<
  ServiceCategory,
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"
> = {
  Criminal: "error",
  Identity: "primary",
  Employment: "info",
  Education: "success",
  Driving: "warning",
  Credit: "secondary",
  Drug: "error",
  Civil: "default",
  Reference: "info",
  Healthcare: "success",
  Custom: "default"
};

interface CategoryChipProps {
  category: ServiceCategory;
}

const CategoryChip = ({ category }: CategoryChipProps) => (
  <Chip
    label={category}
    size="small"
    color={categoryColors[category]}
    variant="outlined"
  />
);

// ============================================================================
// Service Row Component
// ============================================================================

interface ServiceRowProps {
  service: CatalogServiceItemDto;
  onToggle: (serviceTypeCode: string, enabled: boolean) => void;
  onTierChange: (serviceTypeCode: string, tier: number) => void;
  onVendorChange: (serviceTypeCode: string, vendor: string | undefined) => void;
  isPending: boolean;
}

const ServiceRow = ({
                      service,
                      onToggle,
                      onTierChange,
                      onVendorChange,
                      isPending
                    }: ServiceRowProps) => {
  const [localVendor, setLocalVendor] = useState(service.vendorCode ?? "");

  const handleVendorBlur = () => {
    const newVendor = localVendor.trim() || undefined;
    if (newVendor !== service.vendorCode) {
      onVendorChange(service.serviceTypeCode, newVendor);
    }
  };

  return (
    <TableRow
      sx={{
        opacity: service.isEnabled ? 1 : 0.6,
        "&:hover": { bgcolor: "action.hover" }
      }}
    >
      <TableCell>
        <IconButton
          size="small"
          onClick={() => onToggle(service.serviceTypeCode, !service.isEnabled)}
          disabled={isPending}
          color={service.isEnabled ? "success" : "default"}
        >
          {service.isEnabled ? (
            <CheckCircleIcon />
          ) : (
            <RadioButtonUncheckedIcon />
          )}
        </IconButton>
      </TableCell>
      <TableCell>
        <Stack spacing={0.5}>
          <Typography variant="body2" fontWeight={500}>
            {service.displayName}
          </Typography>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontFamily: "monospace" }}
          >
            {service.serviceTypeCode}
          </Typography>
        </Stack>
      </TableCell>
      <TableCell>
        <CategoryChip category={service.category} />
      </TableCell>
      <TableCell>
        <FormControl size="small" sx={{ minWidth: 80 }}>
          <Select
            value={service.tier}
            onChange={(e) =>
              onTierChange(service.serviceTypeCode, Number(e.target.value))
            }
            disabled={isPending || !service.isEnabled}
          >
            {[1, 2, 3, 4, 5].map((tier) => (
              <MenuItem key={tier} value={tier}>
                Tier {tier}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </TableCell>
      <TableCell>
        <TextField
          size="small"
          value={localVendor}
          onChange={(e) => setLocalVendor(e.target.value)}
          onBlur={handleVendorBlur}
          disabled={isPending || !service.isEnabled}
          placeholder="Auto-select"
          sx={{ width: 140 }}
        />
      </TableCell>
    </TableRow>
  );
};

// ============================================================================
// Service Catalog Editor Component
// ============================================================================

interface ServiceCatalogEditorProps {
  customerId: Ulid;
  services: CatalogServiceItemDto[];
}

const ServiceCatalogEditor = ({
                                customerId,
                                services
                              }: ServiceCatalogEditorProps) => {
  const [categoryFilter, setCategoryFilter] = useState<ServiceCategory | "all">(
    "all"
  );
  const [enabledFilter, setEnabledFilter] = useState<
    "all" | "enabled" | "disabled"
  >("all");
  const [successMessage, setSuccessMessage] = useState<string>();
  const [errorMessage, setErrorMessage] = useState<string>();

  const updateMutation = useUpdateCatalogService();

  // Available categories from service list
  const availableCategories = useMemo(() => {
    const categories = new Set(services.map((s) => s.category));
    return Array.from(categories).sort();
  }, [services]);

  // Filter services
  const filteredServices = useMemo(() => {
    return services.filter((service) => {
      if (categoryFilter !== "all" && service.category !== categoryFilter) {
        return false;
      }
      if (enabledFilter === "enabled" && !service.isEnabled) {
        return false;
      }
      if (enabledFilter === "disabled" && service.isEnabled) {
        return false;
      }
      return true;
    });
  }, [services, categoryFilter, enabledFilter]);

  // Stats
  const enabledCount = services.filter((s) => s.isEnabled).length;

  const handleUpdate = async (
    serviceTypeCode: string,
    updates: Partial<{
      isEnabled: boolean;
      tier: number;
      vendorCode: string | undefined;
    }>
  ) => {
    setErrorMessage(undefined);
    setSuccessMessage(undefined);

    const service = services.find((s) => s.serviceTypeCode === serviceTypeCode);
    if (!service) return;

    try {
      await updateMutation.mutateAsync({
        customerId,
        payload: {
          serviceTypeCode,
          isEnabled: updates.isEnabled ?? service.isEnabled,
          tier: updates.tier ?? service.tier,
          vendorCode: updates.vendorCode ?? service.vendorCode
        }
      });
      setSuccessMessage(`Updated ${service.displayName}`);
      setTimeout(() => setSuccessMessage(undefined), 3000);
    } catch (err) {
      setErrorMessage(getErrorMessage(err));
    }
  };

  return (
    <Stack spacing={3}>
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center"
        }}
      >
        <Box>
          <Typography variant="h6">Service Catalog</Typography>
          <Typography variant="body2" color="text.secondary">
            {enabledCount} of {services.length} services enabled
          </Typography>
        </Box>
      </Box>

      {/* Alerts */}
      {successMessage && (
        <Alert severity="success" onClose={() => setSuccessMessage(undefined)}>
          {successMessage}
        </Alert>
      )}
      {errorMessage && (
        <Alert severity="error" onClose={() => setErrorMessage(undefined)}>
          {errorMessage}
        </Alert>
      )}

      {/* Filters */}
      <Card variant="outlined">
        <CardContent>
          <Stack direction="row" spacing={2} alignItems="center">
            <FilterListIcon color="action" />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Category</InputLabel>
              <Select
                value={categoryFilter}
                label="Category"
                onChange={(e) =>
                  setCategoryFilter(e.target.value as ServiceCategory | "all")
                }
              >
                <MenuItem value="all">All Categories</MenuItem>
                {availableCategories.map((cat) => (
                  <MenuItem key={cat} value={cat}>
                    {cat}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 130 }}>
              <InputLabel>Status</InputLabel>
              <Select
                value={enabledFilter}
                label="Status"
                onChange={(e) =>
                  setEnabledFilter(
                    e.target.value as "all" | "enabled" | "disabled"
                  )
                }
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="enabled">Enabled</MenuItem>
                <MenuItem value="disabled">Disabled</MenuItem>
              </Select>
            </FormControl>
            <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>
              Showing {filteredServices.length} services
            </Typography>
          </Stack>
        </CardContent>
      </Card>

      {/* Services Table */}
      <TableContainer component={Card} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell width={60}>
                <Tooltip title="Toggle enabled">
                  <span>On</span>
                </Tooltip>
              </TableCell>
              <TableCell>Service</TableCell>
              <TableCell width={120}>Category</TableCell>
              <TableCell width={100}>Tier</TableCell>
              <TableCell width={160}>Vendor Override</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredServices.map((service) => (
              <ServiceRow
                key={service.serviceTypeCode}
                service={service}
                onToggle={(code, enabled) =>
                  handleUpdate(code, { isEnabled: enabled })
                }
                onTierChange={(code, tier) => handleUpdate(code, { tier })}
                onVendorChange={(code, vendor) =>
                  handleUpdate(code, { vendorCode: vendor })
                }
                isPending={updateMutation.isPending}
              />
            ))}
            {filteredServices.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} align="center" sx={{ py: 4 }}>
                  <Typography color="text.secondary">
                    No services match the current filters.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
};

export default ServiceCatalogEditor;
