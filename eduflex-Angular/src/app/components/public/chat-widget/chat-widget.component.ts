import {
  Component,
  ElementRef,
  ViewChild,
  AfterViewChecked,
  Inject,
  PLATFORM_ID,
  Output,
  EventEmitter,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';
import { Client, AskQuestionDto } from '@services/content.services';
import { extractApiErrorMessage } from '../../../shared/utils/api-error.util';
import { finalize, timeout } from 'rxjs/operators';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './chat-widget.component.html',
  styleUrls: ['./chat-widget.component.css'],
})
export class ChatWidgetComponent implements AfterViewChecked {
  @ViewChild('messagesEnd') messagesEnd?: ElementRef<HTMLDivElement>;
  @Output() contactStaff = new EventEmitter<void>();

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

  get faqQuestions(): string[] {
    return [
      this.translate.instant('CHAT.FAQ1'),
      this.translate.instant('CHAT.FAQ2'),
      this.translate.instant('CHAT.FAQ3'),
      this.translate.instant('CHAT.FAQ4'),
    ];
  }

  constructor(
    private apiClient: Client,
    private translate: TranslateService,
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
      this.errorMessage = this.translate.instant('CHAT.LIMIT_REACHED', { limit: this.dailyLimit });
      return;
    }

    this.messages.push({ role: 'user', text: question });
    this.currentQuestion = '';
    this.errorMessage = '';
    this.isSending = true;
    this.shouldScrollToBottom = true;
    this.recordUsage();

    const payload = new AskQuestionDto({ question });

    this.apiClient
      .ask(payload)
      .pipe(
        timeout(30000),
        finalize(() => {
          this.isSending = false;
          this.shouldScrollToBottom = true;
        }),
      )
      .subscribe({
        next: (result) => {
          this.messages.push({ role: 'assistant', text: result.answer ?? '' });
        },
        error: (err) => {
          this.errorMessage = extractApiErrorMessage(
            err,
            this.translate.instant('CHAT.GENERIC_ERROR'),
          );
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
