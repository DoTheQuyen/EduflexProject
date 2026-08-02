import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FormAnswer, FormQuestion, RICH_TEXT_MAX_LENGTH } from '@app/models/dynamic-form';

// Editable counterpart to app-form-print-preview — reused by the staff Forms tab's
// "Edit response" mode and the student portal's fill page. Mutates the `answers`
// array it's given in place (same reference the parent holds), matching the
// stepFieldsDraft two-way-local-mutation pattern already used in the VISA Process tab.
@Component({
  selector: 'app-form-answer-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './form-answer-editor.component.html',
  styleUrls: ['./form-answer-editor.component.css']
})
export class FormAnswerEditorComponent {
  @Input({ required: true }) questions: FormQuestion[] = [];
  @Input({ required: true }) answers: FormAnswer[] = [];
  @Input() disabled = false;

  get sortedQuestions(): FormQuestion[] {
    return [...this.questions].sort((a, b) => a.order - b.order);
  }

  maxLengthFor(question: FormQuestion): number {
    return question.maxLength ?? RICH_TEXT_MAX_LENGTH;
  }

  charCount(questionId: string): number {
    return this.textValue(questionId).length;
  }

  private getOrCreateAnswer(questionId: string): FormAnswer {
    let answer = this.answers.find(a => a.questionId === questionId);
    if (!answer) {
      answer = { questionId, textValue: '', selectedOptions: [] };
      this.answers.push(answer);
    }
    return answer;
  }

  textValue(questionId: string): string {
    return this.answers.find(a => a.questionId === questionId)?.textValue ?? '';
  }

  setText(questionId: string, value: string): void {
    this.getOrCreateAnswer(questionId).textValue = value;
  }

  isYes(questionId: string): boolean {
    return this.answers.find(a => a.questionId === questionId)?.textValue === 'Yes';
  }

  isNo(questionId: string): boolean {
    return this.answers.find(a => a.questionId === questionId)?.textValue === 'No';
  }

  setYesNo(questionId: string, value: 'Yes' | 'No'): void {
    if (this.disabled) return;
    this.getOrCreateAnswer(questionId).textValue = value;
  }

  isSelected(questionId: string, option: string): boolean {
    return this.answers.find(a => a.questionId === questionId)?.selectedOptions.includes(option) ?? false;
  }

  setSingleOption(questionId: string, option: string): void {
    if (this.disabled) return;
    this.getOrCreateAnswer(questionId).selectedOptions = [option];
  }

  toggleMultiOption(questionId: string, option: string): void {
    if (this.disabled) return;
    const answer = this.getOrCreateAnswer(questionId);
    const index = answer.selectedOptions.indexOf(option);
    if (index >= 0) {
      answer.selectedOptions.splice(index, 1);
    } else {
      answer.selectedOptions.push(option);
    }
  }
}
