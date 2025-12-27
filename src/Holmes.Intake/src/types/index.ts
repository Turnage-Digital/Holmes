// API types
export type {
  StartIntakeSessionRequest,
  VerifyOtpRequest,
  VerifyOtpResponse,
  CaptureConsentRequest,
  CaptureConsentResponse,
  SaveIntakeProgressRequest,
  SubmitIntakeRequest,
  IntakeSectionConfig,
  IntakeBootstrapResponse,
} from "./api";

// Intake domain types
export type {
  IntakeAddress,
  IntakeEmployment,
  IntakeEducation,
  IntakeReference,
  IntakePhone,
  ExtendedIntakeFormState,
} from "./intake";

export {
  AddressType,
  ReferenceType,
  PhoneType,
  createEmptyAddress,
  createEmptyEmployment,
  createEmptyEducation,
  createEmptyReference,
  createEmptyPhone,
  US_STATES,
} from "./intake";
