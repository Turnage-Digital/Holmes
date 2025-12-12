import React, { useState } from "react";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import ContactPhoneIcon from "@mui/icons-material/ContactPhone";
import HomeIcon from "@mui/icons-material/Home";
import PeopleIcon from "@mui/icons-material/People";
import PersonIcon from "@mui/icons-material/Person";
import SchoolIcon from "@mui/icons-material/School";
import WorkIcon from "@mui/icons-material/Work";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  Typography,
} from "@mui/material";
import { format } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";

import type {
  SubjectAddressDto,
  SubjectDetailDto,
  SubjectEducationDto,
  SubjectEmploymentDto,
  SubjectPhoneDto,
  SubjectReferenceDto,
} from "@/types/api";

import { useSubject } from "@/hooks/api";

// ============================================================================
// Tab Panel Component
// ============================================================================

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = ({ children, value, index }: TabPanelProps) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);

// ============================================================================
// Overview Tab
// ============================================================================

interface OverviewTabProps {
  subject: SubjectDetailDto;
}

const OverviewTab = ({ subject }: OverviewTabProps) => {
  const middleNamePart = subject.middleName ? ` ${subject.middleName}` : "";
  const fullName = `${subject.firstName}${middleNamePart} ${subject.lastName}`;
  const birthDateText = subject.birthDate
    ? format(new Date(subject.birthDate), "MMMM d, yyyy")
    : "Not provided";
  const ssnText = subject.ssnLast4
    ? `***-**-${subject.ssnLast4}`
    : "Not provided";
  const statusColor = subject.status === "Active" ? "success" : "default";

  return (
    <Stack spacing={3}>
      <Card variant="outlined">
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Personal Information
          </Typography>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: "160px 1fr",
              gap: 1.5,
            }}
          >
            <Typography variant="body2" color="text.secondary">
              Subject ID
            </Typography>
            <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
              {subject.id}
            </Typography>

            <Typography variant="body2" color="text.secondary">
              Full Name
            </Typography>
            <Typography variant="body2">{fullName}</Typography>

            <Typography variant="body2" color="text.secondary">
              Date of Birth
            </Typography>
            <Typography variant="body2">{birthDateText}</Typography>

            <Typography variant="body2" color="text.secondary">
              Email
            </Typography>
            <Typography variant="body2">
              {subject.email || "Not provided"}
            </Typography>

            <Typography variant="body2" color="text.secondary">
              SSN (last 4)
            </Typography>
            <Typography variant="body2">{ssnText}</Typography>

            <Typography variant="body2" color="text.secondary">
              Status
            </Typography>
            <Chip
              label={subject.status}
              color={statusColor}
              size="small"
              variant="outlined"
            />

            <Typography variant="body2" color="text.secondary">
              Created
            </Typography>
            <Typography variant="body2">
              {format(new Date(subject.createdAt), "MMM d, yyyy 'at' h:mm a")}
            </Typography>
          </Box>
        </CardContent>
      </Card>

      {/* Aliases Card */}
      {subject.aliases.length > 0 && (
        <Card variant="outlined">
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Known Aliases ({subject.aliases.length})
            </Typography>
            <Stack spacing={1}>
              {subject.aliases.map((alias) => {
                const birthDateDisplay = alias.birthDate ? (
                  <Typography variant="caption" color="text.secondary">
                    DOB: {format(new Date(alias.birthDate), "MMM d, yyyy")}
                  </Typography>
                ) : null;
                return (
                  <Box
                    key={alias.id}
                    sx={{
                      p: 1.5,
                      borderRadius: 1,
                      bgcolor: "grey.50",
                    }}
                  >
                    <Typography variant="body2">
                      {alias.firstName} {alias.lastName}
                    </Typography>
                    {birthDateDisplay}
                  </Box>
                );
              })}
            </Stack>
          </CardContent>
        </Card>
      )}

      {/* Summary Cards */}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(150px, 1fr))",
          gap: 2,
        }}
      >
        <Card variant="outlined">
          <CardContent sx={{ textAlign: "center", py: 2 }}>
            <Typography variant="h4" color="primary">
              {subject.addresses.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Addresses
            </Typography>
          </CardContent>
        </Card>
        <Card variant="outlined">
          <CardContent sx={{ textAlign: "center", py: 2 }}>
            <Typography variant="h4" color="primary">
              {subject.employments.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Employments
            </Typography>
          </CardContent>
        </Card>
        <Card variant="outlined">
          <CardContent sx={{ textAlign: "center", py: 2 }}>
            <Typography variant="h4" color="primary">
              {subject.educations.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Education Records
            </Typography>
          </CardContent>
        </Card>
        <Card variant="outlined">
          <CardContent sx={{ textAlign: "center", py: 2 }}>
            <Typography variant="h4" color="primary">
              {subject.references.length}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              References
            </Typography>
          </CardContent>
        </Card>
      </Box>
    </Stack>
  );
};

// ============================================================================
// Addresses Tab
// ============================================================================

const AddressesTab = ({ addresses }: { addresses: SubjectAddressDto[] }) => {
  if (addresses.length === 0) {
    return (
      <Alert severity="info">
        No address history on file for this subject.
      </Alert>
    );
  }

  return (
    <Card variant="outlined">
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Address</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Period</TableCell>
            <TableCell>Status</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {addresses.map((addr) => {
            const addressLine2 = addr.street2 ? `, ${addr.street2}` : "";
            const toPeriod = addr.toDate
              ? format(new Date(addr.toDate), "MMM yyyy")
              : "Present";
            const currentChip = addr.isCurrent ? (
              <Chip label="Current" color="success" size="small" />
            ) : (
              <Chip label="Previous" size="small" variant="outlined" />
            );

            return (
              <TableRow key={addr.id}>
                <TableCell>
                  <Typography variant="body2">
                    {addr.street1}
                    {addressLine2}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {addr.city}, {addr.state} {addr.postalCode}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip label={addr.type} size="small" variant="outlined" />
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {format(new Date(addr.fromDate), "MMM yyyy")} - {toPeriod}
                  </Typography>
                </TableCell>
                <TableCell>{currentChip}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </Card>
  );
};

// ============================================================================
// Employment Tab
// ============================================================================

const EmploymentTab = ({
  employments,
}: {
  employments: SubjectEmploymentDto[];
}) => {
  if (employments.length === 0) {
    return (
      <Alert severity="info">
        No employment history on file for this subject.
      </Alert>
    );
  }

  return (
    <Stack spacing={2}>
      {employments.map((emp) => {
        const currentChip = emp.isCurrent ? (
          <Chip label="Current" color="success" size="small" />
        ) : (
          <Chip label="Previous" size="small" variant="outlined" />
        );
        const endPeriod = emp.endDate
          ? format(new Date(emp.endDate), "MMM yyyy")
          : "Present";
        const contactLabel = emp.canContact
          ? "OK to contact"
          : "Do not contact";
        const contactColor = emp.canContact ? "default" : "warning";
        const supervisorPhone = emp.supervisorPhone
          ? ` (${emp.supervisorPhone})`
          : "";

        return (
          <Card key={emp.id} variant="outlined">
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="flex-start"
              >
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    {emp.employerName}
                  </Typography>
                  {(() => {
                    const jobTitleDisplay = emp.jobTitle ? (
                      <Typography variant="body2" color="text.secondary">
                        {emp.jobTitle}
                      </Typography>
                    ) : null;
                    return jobTitleDisplay;
                  })()}
                </Box>
                {currentChip}
              </Stack>

              <Typography variant="body2" sx={{ mt: 1 }}>
                {format(new Date(emp.startDate), "MMM yyyy")} - {endPeriod}
              </Typography>

              {(() => {
                const supervisorDisplay =
                  emp.supervisorName || emp.supervisorPhone ? (
                    <Box
                      sx={{
                        mt: 1.5,
                        p: 1.5,
                        bgcolor: "grey.50",
                        borderRadius: 1,
                      }}
                    >
                      <Typography variant="caption" color="text.secondary">
                        Supervisor
                      </Typography>
                      <Typography variant="body2">
                        {emp.supervisorName || "—"}
                        {supervisorPhone}
                      </Typography>
                    </Box>
                  ) : null;
                return supervisorDisplay;
              })()}

              {(() => {
                const reasonDisplay = emp.reasonForLeaving ? (
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    <strong>Reason for leaving:</strong> {emp.reasonForLeaving}
                  </Typography>
                ) : null;
                return reasonDisplay;
              })()}

              <Box sx={{ mt: 1 }}>
                <Chip
                  label={contactLabel}
                  size="small"
                  color={contactColor}
                  variant="outlined"
                />
              </Box>
            </CardContent>
          </Card>
        );
      })}
    </Stack>
  );
};

// ============================================================================
// Education Tab
// ============================================================================

const EducationTab = ({
  educations,
}: {
  educations: SubjectEducationDto[];
}) => {
  if (educations.length === 0) {
    return (
      <Alert severity="info">
        No education records on file for this subject.
      </Alert>
    );
  }

  return (
    <Stack spacing={2}>
      {educations.map((edu) => {
        const graduationChip = edu.graduated ? (
          <Chip label="Graduated" color="success" size="small" />
        ) : (
          <Chip label="Attended" size="small" variant="outlined" />
        );
        const majorText = edu.major ? ` in ${edu.major}` : "";

        return (
          <Card key={edu.id} variant="outlined">
            <CardContent>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="flex-start"
              >
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    {edu.institutionName}
                  </Typography>
                  {(() => {
                    const degreeDisplay = edu.degree ? (
                      <Typography variant="body2">
                        {edu.degree}
                        {majorText}
                      </Typography>
                    ) : null;
                    return degreeDisplay;
                  })()}
                </Box>
                {graduationChip}
              </Stack>

              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {edu.attendedFrom &&
                  format(new Date(edu.attendedFrom), "MMM yyyy")}
                {edu.attendedFrom && edu.attendedTo && " - "}
                {edu.attendedTo && format(new Date(edu.attendedTo), "MMM yyyy")}
                {edu.graduationDate && (
                  <>
                    {" "}
                    (Graduated:{" "}
                    {format(new Date(edu.graduationDate), "MMM yyyy")})
                  </>
                )}
              </Typography>

              {(() => {
                const addressDisplay = edu.institutionAddress ? (
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 0.5 }}
                  >
                    {edu.institutionAddress}
                  </Typography>
                ) : null;
                return addressDisplay;
              })()}
            </CardContent>
          </Card>
        );
      })}
    </Stack>
  );
};

// ============================================================================
// References Tab
// ============================================================================

const ReferencesTab = ({
  references,
}: {
  references: SubjectReferenceDto[];
}) => {
  if (references.length === 0) {
    return (
      <Alert severity="info">No references on file for this subject.</Alert>
    );
  }

  return (
    <Card variant="outlined">
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Relationship</TableCell>
            <TableCell>Contact</TableCell>
            <TableCell>Years Known</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {references.map((ref) => {
            const refTypeColor =
              ref.type === "Professional" ? "primary" : "default";
            const emailDisplay = ref.email ? (
              <Typography variant="caption" color="text.secondary">
                {ref.email}
              </Typography>
            ) : null;

            return (
              <TableRow key={ref.id}>
                <TableCell>
                  <Typography variant="body2" fontWeight={500}>
                    {ref.name}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip
                    label={ref.type}
                    size="small"
                    color={refTypeColor}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell>{ref.relationship || "—"}</TableCell>
                <TableCell>
                  <Typography variant="body2">{ref.phone || "—"}</Typography>
                  {emailDisplay}
                </TableCell>
                <TableCell>{ref.yearsKnown ?? "—"}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </Card>
  );
};

// ============================================================================
// Phones Tab
// ============================================================================

const PhonesTab = ({ phones }: { phones: SubjectPhoneDto[] }) => {
  if (phones.length === 0) {
    return (
      <Alert severity="info">No phone numbers on file for this subject.</Alert>
    );
  }

  return (
    <Card variant="outlined">
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Phone Number</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Primary</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {phones.map((phone) => {
            const primaryChip = phone.isPrimary ? (
              <Chip label="Primary" color="primary" size="small" />
            ) : null;
            return (
              <TableRow key={phone.id}>
                <TableCell>
                  <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
                    {phone.phoneNumber}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip label={phone.type} size="small" variant="outlined" />
                </TableCell>
                <TableCell>{primaryChip}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

const SubjectDetailPage = () => {
  const navigate = useNavigate();
  const { subjectId } = useParams<{ subjectId: string }>();
  const [activeTab, setActiveTab] = useState(0);

  const { data: subject, isLoading, error } = useSubject(subjectId!);

  if (isLoading) {
    return (
      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          minHeight: 400,
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (error || !subject) {
    const errorMessage = error
      ? "Failed to load subject. Please try again."
      : "Subject not found.";
    return (
      <Box>
        <IconButton
          onClick={() => navigate("/subjects")}
          sx={{ mb: 2 }}
          size="small"
        >
          <ArrowBackIcon />
        </IconButton>
        <Alert severity="error">{errorMessage}</Alert>
      </Box>
    );
  }

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const middleNamePart = subject.middleName ? ` ${subject.middleName}` : "";
  const fullName = `${subject.firstName}${middleNamePart} ${subject.lastName}`;
  const statusColor = subject.status === "Active" ? "success" : "default";
  const dobText = subject.birthDate
    ? `DOB: ${format(new Date(subject.birthDate), "MMM d, yyyy")}`
    : "DOB not provided";

  return (
    <Box>
      {/* Back button */}
      <IconButton
        onClick={() => navigate("/subjects")}
        sx={{ mb: 2 }}
        size="small"
      >
        <ArrowBackIcon />
      </IconButton>

      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 1 }}>
          <Typography variant="h4" component="h1">
            {fullName}
          </Typography>
          <Chip
            label={subject.status}
            color={statusColor}
            size="small"
            variant="outlined"
          />
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {subject.email || "No email"} | {dobText}
        </Typography>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          variant="scrollable"
          scrollButtons="auto"
        >
          <Tab icon={<PersonIcon />} iconPosition="start" label="Overview" />
          <Tab
            icon={<HomeIcon />}
            iconPosition="start"
            label={`Addresses (${subject.addresses.length})`}
          />
          <Tab
            icon={<WorkIcon />}
            iconPosition="start"
            label={`Employment (${subject.employments.length})`}
          />
          <Tab
            icon={<SchoolIcon />}
            iconPosition="start"
            label={`Education (${subject.educations.length})`}
          />
          <Tab
            icon={<PeopleIcon />}
            iconPosition="start"
            label={`References (${subject.references.length})`}
          />
          <Tab
            icon={<ContactPhoneIcon />}
            iconPosition="start"
            label={`Phones (${subject.phones.length})`}
          />
        </Tabs>
      </Box>

      {/* Tab Content */}
      <TabPanel value={activeTab} index={0}>
        <OverviewTab subject={subject} />
      </TabPanel>
      <TabPanel value={activeTab} index={1}>
        <AddressesTab addresses={subject.addresses} />
      </TabPanel>
      <TabPanel value={activeTab} index={2}>
        <EmploymentTab employments={subject.employments} />
      </TabPanel>
      <TabPanel value={activeTab} index={3}>
        <EducationTab educations={subject.educations} />
      </TabPanel>
      <TabPanel value={activeTab} index={4}>
        <ReferencesTab references={subject.references} />
      </TabPanel>
      <TabPanel value={activeTab} index={5}>
        <PhonesTab phones={subject.phones} />
      </TabPanel>
    </Box>
  );
};

export default SubjectDetailPage;
