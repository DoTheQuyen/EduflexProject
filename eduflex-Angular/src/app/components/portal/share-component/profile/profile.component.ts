import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthHelperService } from '../../../../services/auth-helper.service';
import { Client } from '../../../../services/api.services';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  userInfo: any;
  profileData: any = {};
  isEditing = false;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' = 'success';

  constructor(
    private authHelper: AuthHelperService,
    private client: Client,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserProfile();
  }

  loadUserProfile(): void {
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
      this.loadUserProfile(); // Reset changes if canceling edit
    }
  }

  updateProfile(): void {
    this.isLoading = true;
    this.message = '';

    // In a real implementation, you would call your user service here
    // For now, we'll simulate the update and update local storage
    setTimeout(() => {
      try {
        const updatedUser = {
          ...this.userInfo,
          firstName: this.profileData.firstName,
          lastName: this.profileData.lastName,
          email: this.profileData.email
        };

        // Update localStorage directly since setCurrentUser doesn't exist
        localStorage.setItem('userData', JSON.stringify(updatedUser));
        
        // Reload the user info from updated storage
        this.userInfo = updatedUser;
        
        this.isEditing = false;
        this.showMessage('Profile updated successfully!', 'success');
      } catch (error) {
        this.showMessage('Error updating profile', 'error');
      } finally {
        this.isLoading = false;
      }
    }, 1000);
  }

  changePassword(): void {
    // Navigate to change password page or show modal
    this.showMessage('Password change functionality would be implemented here', 'success');
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