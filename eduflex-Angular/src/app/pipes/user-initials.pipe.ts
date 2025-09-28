import { Pipe, PipeTransform } from '@angular/core';

// Create a union type or use a more generic approach
interface BasicUser {
  firstName?: string;
  lastName?: string;
}

@Pipe({
  name: 'userInitials',
  standalone: true
})
export class UserInitialsPipe implements PipeTransform {
  transform(user: BasicUser | null | undefined): string {
    if (!user) return '?';
    
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || 'U';
  }
}