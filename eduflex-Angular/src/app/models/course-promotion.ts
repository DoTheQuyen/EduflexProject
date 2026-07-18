export interface CoursePromotion {
  id: string;
  courseName: string;
  universityName: string;
  semester: string;
  scholarshipLabel: string;
  location: string;
  tuition: string;
  opportunities: string;
  expiryDate: string;
  note: string;
  websiteUrl: string;
  isFeatured: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface CreateCoursePromotion {
  courseName: string;
  universityName: string;
  semester: string;
  scholarshipLabel: string;
  location: string;
  tuition: string;
  opportunities: string;
  expiryDate: string;
  note: string;
  websiteUrl: string;
  isFeatured: boolean;
  displayOrder: number;
}
