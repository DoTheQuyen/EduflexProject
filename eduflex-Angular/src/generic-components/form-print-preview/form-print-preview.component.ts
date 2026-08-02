import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormAnswer, FormQuestion, answerFor } from '@app/models/dynamic-form';

// Renders a form as a print-style page — reused by the admin builder's live preview,
// the VISA step "preview + request" popup, the staff Forms tab, and the student's
// fill/view screens. 'blank' mode shows empty fill-in lines/checkboxes (nothing to
// answer yet); 'filled' mode renders the given answers (or "No answer provided").
@Component({
  selector: 'app-form-print-preview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-print-preview.component.html',
  styleUrls: ['./form-print-preview.component.css']
})
export class FormPrintPreviewComponent {
  @Input({ required: true }) formName = '';
  @Input() description?: string;
  @Input({ required: true }) questions: FormQuestion[] = [];
  @Input() mode: 'blank' | 'filled' = 'blank';
  @Input() answers: FormAnswer[] = [];
  @Input() studentName?: string;
  @Input() studentEmail?: string;
  @Input() studentPhone?: string;
  @Input() submittedAt?: string;
  @Input() compact = false;

  get sortedQuestions(): FormQuestion[] {
    return [...this.questions].sort((a, b) => a.order - b.order);
  }

  answerFor(questionId: string): FormAnswer | undefined {
    return answerFor(this.answers, questionId);
  }

  selectedSet(questionId: string): Set<string> {
    return new Set(this.answerFor(questionId)?.selectedOptions ?? []);
  }

  isYes(questionId: string): boolean {
    return this.answerFor(questionId)?.textValue === 'Yes';
  }

  isNo(questionId: string): boolean {
    return this.answerFor(questionId)?.textValue === 'No';
  }

  textAnswer(questionId: string): string {
    return this.answerFor(questionId)?.textValue ?? '';
  }

  hasAnswer(question: FormQuestion): boolean {
    const answer = this.answerFor(question.id);
    if (!answer) return false;
    if (question.answerType === 'MultiSelect' || question.answerType === 'SingleSelect') {
      return answer.selectedOptions.length > 0;
    }
    return !!answer.textValue?.trim();
  }
}
