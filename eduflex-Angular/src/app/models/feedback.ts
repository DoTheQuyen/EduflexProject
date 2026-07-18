export interface Feedback {
  id: string;
  name: string;
  photoUrl: string;
  courseName: string;
  comment: string;
  createdAt: string;
}

export interface CreateFeedback {
  name: string;
  photoData: string;
  photoContentType: string;
  courseName: string;
  comment: string;
}