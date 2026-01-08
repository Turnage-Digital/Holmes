// Types for extended intake form data

export interface IntakeAddress {
  id: string;
  street1: string;
  street2: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  countyFips: string;
  fromDate: string;
  toDate: string;
  type: AddressType;
  isCurrent: boolean;
}

export enum AddressType {
  Residential = 0,
  Mailing = 1,
  Business = 2,
}

export interface IntakeEmployment {
  id: string;
  employerName: string;
  employerPhone: string;
  employerAddress: string;
  jobTitle: string;
  supervisorName: string;
  supervisorPhone: string;
  startDate: string;
  endDate: string;
  reasonForLeaving: string;
  canContact: boolean;
  isCurrent: boolean;
}

export interface IntakeEducation {
  id: string;
  institutionName: string;
  institutionAddress: string;
  degree: string;
  major: string;
  attendedFrom: string;
  attendedTo: string;
  graduationDate: string;
  graduated: boolean;
}

export interface IntakeReference {
  id: string;
  name: string;
  phone: string;
  email: string;
  relationship: string;
  yearsKnown: number | null;
  type: ReferenceType;
}

export enum ReferenceType {
  Personal = 0,
  Professional = 1,
}

export interface IntakePhone {
  id: string;
  phoneNumber: string;
  type: PhoneType;
  isPrimary: boolean;
}

export enum PhoneType {
  Mobile = 0,
  Home = 1,
  Work = 2,
}

// Extended form state with all collections
export interface ExtendedIntakeFormState {
  // Basic info (existing)
  firstName: string;
  lastName: string;
  middleName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  ssnFull: string;
  authorizationAccepted: boolean;

  // Collections (new)
  addresses: IntakeAddress[];
  employments: IntakeEmployment[];
  educations: IntakeEducation[];
  references: IntakeReference[];
  phones: IntakePhone[];
}

// Default empty records for initializing new entries
export const createEmptyAddress = (): IntakeAddress => ({
  id: crypto.randomUUID(),
  street1: "",
  street2: "",
  city: "",
  state: "",
  postalCode: "",
  country: "USA",
  countyFips: "",
  fromDate: "",
  toDate: "",
  type: AddressType.Residential,
  isCurrent: false,
});

export const createEmptyEmployment = (): IntakeEmployment => ({
  id: crypto.randomUUID(),
  employerName: "",
  employerPhone: "",
  employerAddress: "",
  jobTitle: "",
  supervisorName: "",
  supervisorPhone: "",
  startDate: "",
  endDate: "",
  reasonForLeaving: "",
  canContact: false,
  isCurrent: false,
});

export const createEmptyEducation = (): IntakeEducation => ({
  id: crypto.randomUUID(),
  institutionName: "",
  institutionAddress: "",
  degree: "",
  major: "",
  attendedFrom: "",
  attendedTo: "",
  graduationDate: "",
  graduated: false,
});

export const createEmptyReference = (): IntakeReference => ({
  id: crypto.randomUUID(),
  name: "",
  phone: "",
  email: "",
  relationship: "",
  yearsKnown: null,
  type: ReferenceType.Personal,
});

export const createEmptyPhone = (): IntakePhone => ({
  id: crypto.randomUUID(),
  phoneNumber: "",
  type: PhoneType.Mobile,
  isPrimary: false,
});

// US State options for dropdowns
export const US_STATES = [
  { value: "AL", label: "Alabama" },
  { value: "AK", label: "Alaska" },
  { value: "AZ", label: "Arizona" },
  { value: "AR", label: "Arkansas" },
  { value: "CA", label: "California" },
  { value: "CO", label: "Colorado" },
  { value: "CT", label: "Connecticut" },
  { value: "DE", label: "Delaware" },
  { value: "FL", label: "Florida" },
  { value: "GA", label: "Georgia" },
  { value: "HI", label: "Hawaii" },
  { value: "ID", label: "Idaho" },
  { value: "IL", label: "Illinois" },
  { value: "IN", label: "Indiana" },
  { value: "IA", label: "Iowa" },
  { value: "KS", label: "Kansas" },
  { value: "KY", label: "Kentucky" },
  { value: "LA", label: "Louisiana" },
  { value: "ME", label: "Maine" },
  { value: "MD", label: "Maryland" },
  { value: "MA", label: "Massachusetts" },
  { value: "MI", label: "Michigan" },
  { value: "MN", label: "Minnesota" },
  { value: "MS", label: "Mississippi" },
  { value: "MO", label: "Missouri" },
  { value: "MT", label: "Montana" },
  { value: "NE", label: "Nebraska" },
  { value: "NV", label: "Nevada" },
  { value: "NH", label: "New Hampshire" },
  { value: "NJ", label: "New Jersey" },
  { value: "NM", label: "New Mexico" },
  { value: "NY", label: "New York" },
  { value: "NC", label: "North Carolina" },
  { value: "ND", label: "North Dakota" },
  { value: "OH", label: "Ohio" },
  { value: "OK", label: "Oklahoma" },
  { value: "OR", label: "Oregon" },
  { value: "PA", label: "Pennsylvania" },
  { value: "RI", label: "Rhode Island" },
  { value: "SC", label: "South Carolina" },
  { value: "SD", label: "South Dakota" },
  { value: "TN", label: "Tennessee" },
  { value: "TX", label: "Texas" },
  { value: "UT", label: "Utah" },
  { value: "VT", label: "Vermont" },
  { value: "VA", label: "Virginia" },
  { value: "WA", label: "Washington" },
  { value: "WV", label: "West Virginia" },
  { value: "WI", label: "Wisconsin" },
  { value: "WY", label: "Wyoming" },
  { value: "DC", label: "District of Columbia" },
];
