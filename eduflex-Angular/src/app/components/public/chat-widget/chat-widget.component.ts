import {
  Component,
  ElementRef,
  ViewChild,
  AfterViewChecked,
  Inject,
  PLATFORM_ID,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client, AskQuestionDto } from '@services/content.services';
import { extractApiErrorMessage } from '../../../shared/utils/api-error.util';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-widget.component.html',
  styleUrls: ['./chat-widget.component.css'],
})
export class ChatWidgetComponent implements AfterViewChecked {
  @ViewChild('messagesEnd') messagesEnd?: ElementRef<HTMLDivElement>;

  isOpen = false;
  isSending = false;
  currentQuestion = '';
  errorMessage = '';
  messages: ChatMessage[] = [];
  private readonly dailyLimit = 10;
  private readonly usageStorageKey = 'eduflex_chat_usage';
  private isBrowser: boolean;
  questionsAskedToday = 0;
  private shouldScrollToBottom = false;

  faqQuestions: string[] = [
    'What is the difference between a 189 and 190 visa?',
    'How long does a 485 Temporary Graduate visa last?',
    'What is a 491 visa?',
    'Do I need an IELTS score for a skilled visa?',
  ];

  constructor(
    private apiClient: Client,
    @Inject(PLATFORM_ID) platformId: Object,
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    this.questionsAskedToday = this.loadUsageCount();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  toggleOpen(): void {
    this.isOpen = !this.isOpen;
  }

  askFaq(question: string): void {
    this.currentQuestion = question;
    this.sendQuestion();
  }

  sendQuestion(): void {
    const question = this.currentQuestion.trim();
    if (!question || this.isSending) {
      return;
    }

    if (this.isLimitReached) {
      this.errorMessage = `You've reached today's limit of ${this.dailyLimit} questions. Please try again tomorrow.`;
      return;
    }

    this.messages.push({ role: 'user', text: question });
    this.currentQuestion = '';
    this.errorMessage = '';
    this.isSending = true;
    this.shouldScrollToBottom = true;
    this.recordUsage();

    const payload = new AskQuestionDto({ question });

    this.apiClient.ask(payload).subscribe({
      next: (result) => {
        this.messages.push({ role: 'assistant', text: result.answer ?? '' });
        this.isSending = false;
        this.shouldScrollToBottom = true;
      },
      error: (err) => {
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong. Please try again.');
        this.isSending = false;
        this.shouldScrollToBottom = true;
      },
    });
  }

  private todayKey(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private loadUsageCount(): number {
    if (!this.isBrowser) {
      return 0;
    }
    const raw = localStorage.getItem(this.usageStorageKey);
    if (!raw) {
      return 0;
    }
    try {
      const data = JSON.parse(raw);
      return data.date === this.todayKey() ? data.count : 0;
    } catch {
      return 0;
    }
  }

  private recordUsage(): void {
    this.questionsAskedToday += 1;
    if (this.isBrowser) {
      localStorage.setItem(
        this.usageStorageKey,
        JSON.stringify({ date: this.todayKey(), count: this.questionsAskedToday }),
      );
    }
  }

  get isLimitReached(): boolean {
    return this.questionsAskedToday >= this.dailyLimit;
  }

  private scrollToBottom(): void {
    try {
      this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    } catch {
      // element not present yet — nothing to scroll to
    }
  }
}
