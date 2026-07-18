export interface CreateEnquiry {
  firstName: string;
  middleName?: string;
  lastName: string;
  email: string;
  mobile: string;
  enquiry: string;
  recaptchaToken: string;
}

export interface Enquiry {
  id: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  email: string;
  mobile: string;
  enquiry: string;
  status: string;
  createdAt: string;
}
