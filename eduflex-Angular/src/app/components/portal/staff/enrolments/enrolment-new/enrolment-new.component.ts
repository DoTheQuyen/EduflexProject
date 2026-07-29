import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Client,
  EducationPartnerDto,
  CourseDto,
  StudentAccountDto,
  StudentFilterDto,
  CreateStudentDto,
  UpdateStudentDto,
  CheckDuplicateStudentDto,
  DuplicateCheckResultDto,
  AddressDto
} from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { EnrolmentService } from '@services/enrolment.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '../../../../../shared/utils/http-error.util';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';
import { CreateEnrolmentRequest } from '../../../../../models/enrolment';
import { StudentDetailsFormComponent } from '../../../share-component/student-details-form/student-details-form.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { ModalComponent } from '@generic/modal/modal.component';

@Component({
  selector: 'app-enrolment-new',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StudentDetailsFormComponent, NotificationComponent, DataTableComponent, ModalComponent],
  templateUrl: './enrolment-new.component.html',
  styleUrls: ['./enrolment-new.component.css']
})
export class EnrolmentNewComponent implements OnInit {
  step: 1 | 2 = 1;

  enquiryId: string | null = null;
  enquiryLabel = '';

  canSearchStudents = false;
  canCreateStudents = false;
  canEditStudents = false;

  // ----- Step 1: find or create the student -----
  selectedStudent: StudentAccountDto | null = null;

  searchResults: StudentAccountDto[] = [];
  isSearching = false;
  pager = new TablePagerState();

  searchColumns: DataTableColumn<StudentAccountDto>[] = [
    { field: 'firstName', title: 'First Name' },
    { field: 'lastName', title: 'Last Name' },
    { field: 'email', title: 'Email' },
    { field: 'mobile', title: 'Mobile' },
    { field: 'isActive', title: 'Active', formatter: (value) => value ? 'Yes' : 'No' },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];
  searchRowActions: DataTableRowAction<StudentAccountDto>[] = [
    { action: 'select', label: 'Select', icon: 'fa-check', cssClass: 'btn btn-sm btn-primary' }
  ];

  showCreateNew = false;
  studentForm: FormGroup;
  isCheckingStudent = false;
  studentErrorMessage = '';
  duplicateWarning: DuplicateCheckResultDto | null = null;

  isEditStudentModalOpen = false;
  editStudentForm: FormGroup;
  isSavingStudentEdit = false;
  editStudentErrorMessage = '';

  // ----- Step 2: enrolment details -----
  form: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  partners: EducationPartnerDto[] = [];
  courses: CourseDto[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private enrolmentService: EnrolmentService,
    private notificationService: NotificationService
  ) {
    this.studentForm = StudentDetailsFormComponent.buildFormGroup(this.fb);
    this.editStudentForm = StudentDetailsFormComponent.buildFormGroup(this.fb);

    this.form = this.fb.group({
      middleName: [''],
      gender: [''],

      sameAsHometown: [true],
      currentStreet: [''],
      currentSuburb: [''],
      currentCity: [''],
      currentState: [''],
      currentPostcode: [''],
      currentCountry: [''],

      emergencyName: [''],
      emergencyRelationship: [''],
      emergencyPhone: [''],
      emergencyEmail: [''],

      educationPartnerId: ['', Validators.required],
      courseId: ['', Validators.required],
      intake: [''],
      studyMode: [''],
      campus: [''],
      commencementDate: [''],
      expectedCompletionDate: [''],
      notes: ['']
    });

    if (!this.authHelper.hasEnrolmentsPermission().add) {
      this.notificationService.error('You do not have permission to create enrolments.');
      this.router.navigate(['/staff-portal/enrolments']);
      return;
    }

    this.canSearchStudents = this.authHelper.hasStudentsPermission().view;
    this.canCreateStudents = this.authHelper.hasStudentsPermission().add;
    this.canEditStudents = this.authHelper.hasStudentsPermission().edit;
  }

  ngOnInit(): void {
    this.loadPartners();

    this.enquiryId = this.route.snapshot.queryParamMap.get('enquiryId');
    if (this.enquiryId) {
      this.prefillFromEnquiry(this.enquiryId);
    } else if (this.canSearchStudents) {
      this.loadStudentResults();
    }
  }

  // ----- Step 1: search -----

  loadStudentResults(): void {
    if (!this.canSearchStudents) return;

    this.isSearching = true;
    const filter = new StudentFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined
    });

    this.apiClient.searchStudents(filter).subscribe({
      next: (result) => {
        this.searchResults = result.items ?? [];
        this.pager.totalCount = result.totalCount ?? 0;
        this.isSearching = false;
      },
      error: () => {
        this.isSearching = false;
      }
    });
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadStudentResults();
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadStudentResults();
  }

  onSearchTableAction(event: DataTableAction<StudentAccountDto>): void {
    if (event.action === 'select') {
      this.selectStudent(event.row);
    }
  }

  selectStudent(student: StudentAccountDto): void {
    if (student.isActive === false) {
      this.notificationService.error('This student is inactive. Reactivate them from Student Management before enrolling.');
      return;
    }
    this.selectedStudent = student;
    this.goToStep2();
  }

  changeStudent(): void {
    this.selectedStudent = null;
    this.step = 1;
  }

  // ----- Edit the selected student's own details, without leaving the wizard -----

  openEditStudentModal(): void {
    if (!this.selectedStudent) return;
    const s = this.selectedStudent;
    this.editStudentForm.reset({
      email: s.email,
      mobile: s.mobile,
      firstName: s.firstName,
      lastName: s.lastName,
      nationality: s.nationality,
      passportNumber: s.passportNumber,
      dateOfBirth: this.toDateInputValue(s.dateOfBirth),
      street: s.address?.street,
      suburb: s.address?.suburb,
      city: s.address?.city,
      state: s.address?.state,
      country: s.address?.country,
      postalCode: s.address?.postalCode
    });
    this.editStudentErrorMessage = '';
    this.isEditStudentModalOpen = true;
  }

  closeEditStudentModal(): void {
    this.isEditStudentModalOpen = false;
  }

  private toDateInputValue(date?: Date): string {
    if (!date) return '';
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  onSubmitEditStudent(): void {
    if (!this.selectedStudent?.id) return;

    this.editStudentForm.markAllAsTouched();
    this.editStudentErrorMessage = '';
    if (this.editStudentForm.invalid) return;

    this.isSavingStudentEdit = true;
    const v = this.editStudentForm.value;
    const updateDto = new UpdateStudentDto({
      email: v.email,
      mobile: v.mobile,
      firstName: v.firstName,
      lastName: v.lastName,
      nationality: v.nationality,
      passportNumber: v.passportNumber,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      address: new AddressDto({
        street: v.street,
        suburb: v.suburb || undefined,
        city: v.city,
        state: v.state || undefined,
        country: v.country,
        postalCode: v.postalCode
      })
    });

    this.apiClient.studentsPUT(this.selectedStudent.id, updateDto).subscribe({
      next: (updated) => {
        this.isSavingStudentEdit = false;
        this.selectedStudent = updated;
        this.toggleSameAsHometown();
        this.closeEditStudentModal();
        this.notificationService.success('Student details updated.');
      },
      error: (err) => {
        this.isSavingStudentEdit = false;
        this.editStudentErrorMessage = extractApiErrorMessage(err, 'Something went wrong updating this student. Please try again.');
      }
    });
  }

  // ----- Step 1: create new student inline -----

  toggleCreateNew(): void {
    this.showCreateNew = !this.showCreateNew;
    this.studentErrorMessage = '';
    this.duplicateWarning = null;
  }

  private buildStudentAddress(): AddressDto {
    const v = this.studentForm.value;
    return new AddressDto({
      street: v.street,
      suburb: v.suburb || undefined,
      city: v.city,
      state: v.state || undefined,
      country: v.country,
      postalCode: v.postalCode
    });
  }

  onCreateStudentSubmit(): void {
    this.studentForm.markAllAsTouched();
    this.studentErrorMessage = '';
    this.duplicateWarning = null;

    if (this.studentForm.invalid) return;

    this.checkDuplicateThenCreateStudent();
  }

  private checkDuplicateThenCreateStudent(): void {
    this.isCheckingStudent = true;
    const v = this.studentForm.value;
    const checkDto = new CheckDuplicateStudentDto({
      email: v.email,
      mobile: v.mobile,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      passportNumber: v.passportNumber
    });

    this.apiClient.checkDuplicate(checkDto).subscribe({
      next: (result) => {
        this.isCheckingStudent = false;
        if (result.isDuplicate) {
          this.duplicateWarning = result;
        } else {
          this.submitCreateStudent();
        }
      },
      error: (err) => {
        this.isCheckingStudent = false;
        this.studentErrorMessage = extractApiErrorMessage(err, 'Could not check for duplicates. Please try again.');
      }
    });
  }

  private submitCreateStudent(): void {
    this.isCheckingStudent = true;
    const v = this.studentForm.value;
    const createDto = new CreateStudentDto({
      email: v.email,
      mobile: v.mobile,
      firstName: v.firstName,
      lastName: v.lastName,
      nationality: v.nationality,
      passportNumber: v.passportNumber,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      address: this.buildStudentAddress()
    });

    this.apiClient.studentsPOST(createDto).subscribe({
      next: (created) => {
        this.isCheckingStudent = false;
        this.notificationService.success('Student created. They have been emailed their login details.');
        this.selectStudent(created);
      },
      error: (err) => {
        this.isCheckingStudent = false;
        this.studentErrorMessage = extractApiErrorMessage(err, 'Something went wrong creating this student. Please try again.');
      }
    });
  }

  useExistingFromDuplicate(): void {
    if (!this.duplicateWarning?.existingStudentId) return;

    this.isCheckingStudent = true;
    this.apiClient.studentsGET(this.duplicateWarning.existingStudentId).subscribe({
      next: (student) => {
        this.isCheckingStudent = false;
        this.selectStudent(student);
      },
      error: (err) => {
        this.isCheckingStudent = false;
        this.studentErrorMessage = extractApiErrorMessage(err, 'Could not load the existing student.');
      }
    });
  }

  reactivateFromDuplicate(): void {
    if (!this.duplicateWarning?.existingStudentId) return;
    const id = this.duplicateWarning.existingStudentId;

    this.isCheckingStudent = true;
    this.apiClient.reactivate(id).subscribe({
      next: () => {
        this.notificationService.success('Existing student reactivated.');
        this.apiClient.studentsGET(id).subscribe({
          next: (student) => {
            this.isCheckingStudent = false;
            this.selectStudent(student);
          },
          error: (err) => {
            this.isCheckingStudent = false;
            this.studentErrorMessage = extractApiErrorMessage(err, 'Reactivated, but could not load the student record.');
          }
        });
      },
      error: (err) => {
        this.isCheckingStudent = false;
        this.studentErrorMessage = extractApiErrorMessage(err, 'Could not reactivate this student. Please try again.');
      }
    });
  }

  dismissDuplicateWarning(): void {
    this.duplicateWarning = null;
  }

  // ----- Step transitions -----

  private goToStep2(): void {
    this.showCreateNew = false;
    this.duplicateWarning = null;
    this.step = 2;
    this.toggleSameAsHometown();
  }

  backToStep1(): void {
    this.step = 1;
  }

  // ----- Step 2: enrolment details -----

  private loadPartners(): void {
    this.apiClient.educationPartnersAll().subscribe({
      next: (partners) => { this.partners = partners; },
      error: () => {}
    });
  }

  onPartnerChange(): void {
    const partnerId = this.form.value.educationPartnerId;
    this.form.patchValue({ courseId: '' });
    this.courses = [];
    this.onCourseChange();
    if (!partnerId) return;

    this.apiClient.byPartner(partnerId).subscribe({
      next: (courses) => { this.courses = courses; },
      error: () => {}
    });
  }

  get selectedCourse(): CourseDto | undefined {
    return this.courses.find(c => c.id === this.form.value.courseId);
  }

  onCourseChange(): void {
    this.form.patchValue({ intake: '', studyMode: '', campus: '' });
  }

  private prefillFromEnquiry(enquiryId: string): void {
    this.apiClient.enquiriesGET(enquiryId).subscribe({
      next: (enquiry) => {
        this.enquiryLabel = `${enquiry.firstName} ${enquiry.lastName} (#${enquiry.id})`;
        this.studentForm.patchValue({
          firstName: enquiry.firstName,
          lastName: enquiry.lastName,
          email: enquiry.email,
          mobile: enquiry.mobile
        });

        if (this.canSearchStudents) {
          this.pager.search(enquiry.email ?? '');
          this.loadStudentResults();
        }
      },
      error: () => {
        this.notificationService.error('Could not load the source enquiry.');
      }
    });
  }

  toggleSameAsHometown(): void {
    if (!this.form.value.sameAsHometown || !this.selectedStudent?.address) return;
    const a = this.selectedStudent.address;
    this.form.patchValue({
      currentStreet: a.street,
      currentSuburb: a.suburb,
      currentCity: a.city,
      currentState: a.state,
      currentPostcode: a.postalCode,
      currentCountry: a.country
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!control && control.invalid && control.touched;
  }

  private buildRequest(): CreateEnrolmentRequest {
    const student = this.selectedStudent!;
    const v = this.form.value;
    return {
      studentId: student.id,
      firstName: student.firstName ?? '',
      middleName: v.middleName || undefined,
      lastName: student.lastName ?? '',
      dateOfBirth: student.dateOfBirth ? new Date(student.dateOfBirth).toISOString() : undefined,
      gender: v.gender || undefined,
      email: student.email ?? '',
      mobile: student.mobile ?? '',
      nationality: student.nationality || undefined,
      passportNumber: student.passportNumber || undefined,
      hometownAddress: {
        street: student.address?.street, suburb: student.address?.suburb, city: student.address?.city,
        state: student.address?.state, postalCode: student.address?.postalCode, country: student.address?.country
      },
      currentAddress: {
        street: v.currentStreet, suburb: v.currentSuburb, city: v.currentCity,
        state: v.currentState, postalCode: v.currentPostcode, country: v.currentCountry
      },
      emergencyContact: {
        name: v.emergencyName, relationship: v.emergencyRelationship,
        phone: v.emergencyPhone, email: v.emergencyEmail
      },
      educationPartnerId: v.educationPartnerId,
      courseId: v.courseId,
      intake: v.intake || undefined,
      studyMode: v.studyMode || undefined,
      campus: v.campus || undefined,
      commencementDate: v.commencementDate || undefined,
      expectedCompletionDate: v.expectedCompletionDate || undefined,
      notes: v.notes || undefined
    };
  }

  onSubmit(): void {
    if (!this.selectedStudent) return;

    this.form.markAllAsTouched();
    this.errorMessage = '';
    if (this.form.invalid) return;

    this.isSubmitting = true;
    const request = this.buildRequest();

    const create$ = this.enquiryId
      ? this.enrolmentService.createFromEnquiry(this.enquiryId, request)
      : this.enrolmentService.createIndependent(request);

    create$.subscribe({
      next: (enrolment) => {
        this.isSubmitting = false;
        this.notificationService.success('Enrolment created.');
        this.router.navigate(['/staff-portal/enrolments', enrolment.id]);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractHttpErrorMessage(err, 'Something went wrong creating this enrolment. Please try again.');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/enrolments']);
  }
}
