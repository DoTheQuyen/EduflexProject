import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthHelperService } from '@services/auth-helper.service';
import { Client, UserDto, UpdateUserProfileDto, ChangePasswordDto } from '@services/api.services';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  // styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  userInfo: any;
  profileData: any = {};
  isEditing = false;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' = 'success';

  // For change password functionality
  showChangePassword = false;
  passwordData = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  constructor(
    private authHelper: AuthHelperService,
    private client: Client,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserProfileFromAPI();
  }

  loadUserProfileFromAPI(): void {
    this.isLoading = true;
    this.client.profileGET().subscribe({
      next: (userProfile: UserDto) => {
        this.userInfo = userProfile;
        this.profileData = {
          firstName: userProfile.firstName || '',
          lastName: userProfile.lastName || '',
          email: userProfile.email || '',
          role: userProfile.role || 'Student'
        };
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading user profile:', error);
        // Fallback to local storage if API fails
        this.loadUserProfileFromLocalStorage();
        this.isLoading = false;
      }
    });
  }

  loadUserProfileFromLocalStorage(): void {
    this.userInfo = this.authHelper.getCurrentUser();
    if (this.userInfo) {
      this.profileData = {
        firstName: this.userInfo.firstName || '',
        lastName: this.userInfo.lastName || '',
        email: this.userInfo.email || '',
        role: this.userInfo.role || 'Student'
      };
    }
  }

  // Helper methods for template
  getCreatedAt(): string {
    if (!this.userInfo?.createdAt) return 'Not available';
    return new Date(this.userInfo.createdAt).toLocaleString();
  }

  getLastLogin(): string {
    if (!this.userInfo?.lastLogin) return 'Never';
    return new Date(this.userInfo.lastLogin).toLocaleString();
  }

  toggleEdit(): void {
    this.isEditing = !this.isEditing;
    if (!this.isEditing) {
      this.loadUserProfileFromAPI(); // Reset changes if canceling edit
    }
  }

  updateProfile(): void {
    this.isLoading = true;
    this.message = '';

    // Create UpdateUserProfileDto instance properly
    const updateData = new UpdateUserProfileDto();
    updateData.firstName = this.profileData.firstName;
    updateData.lastName = this.profileData.lastName;
    updateData.email = this.profileData.email;

    this.client.profilePUT(updateData).subscribe({
      next: (updatedProfile: UserDto) => {
        // Update local storage with new data
        const updatedUser = {
          ...this.userInfo,
          firstName: updatedProfile.firstName,
          lastName: updatedProfile.lastName,
          email: updatedProfile.email
        };
        localStorage.setItem('userData', JSON.stringify(updatedUser));
        
        this.userInfo = updatedProfile;
        this.isEditing = false;
        this.showMessage('Profile updated successfully!', 'success');
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.showMessage('Error updating profile. Please try again.', 'error');
        this.isLoading = false;
      }
    });
  }

  toggleChangePassword(): void {
    this.showChangePassword = !this.showChangePassword;
    this.passwordData = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
  }

  changePassword(): void {
    if (this.passwordData.newPassword !== this.passwordData.confirmPassword) {
      this.showMessage('New password and confirmation do not match.', 'error');
      return;
    }

    if (this.passwordData.newPassword.length < 6) {
      this.showMessage('New password must be at least 6 characters long.', 'error');
      return;
    }

    this.isLoading = true;

    // Create ChangePasswordDto instance properly
    const passwordData = new ChangePasswordDto();
    passwordData.currentPassword = this.passwordData.currentPassword;
    passwordData.newPassword = this.passwordData.newPassword;
    passwordData.confirmPassword = this.passwordData.confirmPassword;

    this.client.changePassword(passwordData).subscribe({
      next: () => {
        this.showChangePassword = false;
        this.passwordData = {
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        };
        this.showMessage('Password changed successfully!', 'success');
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.showMessage('Error changing password. Please check your current password.', 'error');
        this.isLoading = false;
      }
    });
  }

  private showMessage(text: string, type: 'success' | 'error'): void {
    this.message = text;
    this.messageType = type;
    setTimeout(() => {
      this.message = '';
    }, 5000);
  }

  getFullName(): string {
    return `${this.profileData.firstName} ${this.profileData.lastName}`.trim();
  }
}