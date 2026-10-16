import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  LucideCircleAlert,
  LucideCircleCheck,
  LucideChevronRight,
  LucideDynamicIcon,
  LucideHistory,
  LucideInfo,
  LucideOrbit,
  LucidePencil,
  LucidePlus,
  LucideRefreshCw,
  LucideSearch,
  LucideUserRound,
  LucideX
} from '@lucide/angular';
import { ActsApiService } from './acts-api.service';
import { AstronautDuty, Person } from './models';

type DialogMode = 'create' | 'rename' | 'duty' | null;

@Component({
  selector: 'acts-root',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideDynamicIcon],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly api = inject(ActsApiService);

  readonly icons = {
    alert: LucideCircleAlert,
    check: LucideCircleCheck,
    chevronRight: LucideChevronRight,
    history: LucideHistory,
    info: LucideInfo,
    orbit: LucideOrbit,
    pencil: LucidePencil,
    plus: LucidePlus,
    refreshCw: LucideRefreshCw,
    search: LucideSearch,
    userRound: LucideUserRound,
    x: LucideX
  };

  people: Person[] = [];
  duties: AstronautDuty[] = [];
  selectedPerson: Person | null = null;
  searchTerm = '';
  loadingPeople = false;
  loadingDuties = false;
  saving = false;
  dialogMode: DialogMode = null;
  personName = '';
  newName = '';
  dutyRank = '';
  dutyTitle = '';
  dutyStartDate = this.today();
  notice = '';
  error = '';

  constructor() {
    this.refreshPeople();
  }

  get filteredPeople(): Person[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    if (!query) return this.people;
    return this.people.filter(person => person.name.toLocaleLowerCase().includes(query));
  }

  get astronautCount(): number {
    return this.people.filter(person => this.isAstronaut(person)).length;
  }

  get retiredCount(): number {
    return this.people.filter(person => person.careerEndDate !== null).length;
  }

  isAstronaut(person: Person): boolean {
    return Boolean(person.currentDutyTitle || person.currentRank);
  }

  selectPerson(person: Person): void {
    this.selectedPerson = person;
    this.notice = '';
    this.error = '';
    this.loadDuties(person);
  }

  refreshPeople(preferredName?: string): void {
    const nameToKeep = preferredName ?? this.selectedPerson?.name;
    this.loadingPeople = true;
    this.error = '';
    this.api.getPeople().pipe(finalize(() => this.loadingPeople = false)).subscribe({
      next: people => {
        this.people = people.sort((first, second) => first.name.localeCompare(second.name));
        const nextSelection = this.people.find(person => person.name === nameToKeep) ?? this.people[0] ?? null;
        this.selectedPerson = nextSelection;
        if (nextSelection) this.loadDuties(nextSelection);
        else this.duties = [];
      },
      error: error => this.error = this.errorMessage(error, 'Could not load the people directory.')
    });
  }

  openDialog(mode: Exclude<DialogMode, null>): void {
    this.dialogMode = mode;
    this.personName = '';
    this.newName = this.selectedPerson?.name ?? '';
    this.dutyRank = '';
    this.dutyTitle = '';
    this.dutyStartDate = this.today();
    this.error = '';
  }

  closeDialog(): void {
    if (!this.saving) this.dialogMode = null;
  }

  submitPerson(): void {
    const name = this.personName.trim();
    if (!name) return;
    this.saving = true;
    this.api.createPerson(name).pipe(finalize(() => this.saving = false)).subscribe({
      next: () => {
        this.dialogMode = null;
        this.notice = `${name} added to the directory.`;
        this.refreshPeople(name);
      },
      error: error => this.error = this.errorMessage(error, 'Could not create this person.')
    });
  }

  submitRename(): void {
    if (!this.selectedPerson) return;
    const updatedName = this.newName.trim();
    if (!updatedName) return;
    this.saving = true;
    this.api.renamePerson(this.selectedPerson.name, updatedName).pipe(finalize(() => this.saving = false)).subscribe({
      next: () => {
        this.dialogMode = null;
        this.notice = `Record renamed to ${updatedName}.`;
        this.refreshPeople(updatedName);
      },
      error: error => this.error = this.errorMessage(error, 'Could not rename this person.')
    });
  }

  submitDuty(): void {
    if (!this.selectedPerson) return;
    this.saving = true;
    this.api.createDuty({
      name: this.selectedPerson.name,
      rank: this.dutyRank.trim(),
      dutyTitle: this.dutyTitle.trim(),
      dutyStartDate: this.dutyStartDate
    }).pipe(finalize(() => this.saving = false)).subscribe({
      next: () => {
        const personName = this.selectedPerson?.name;
        this.dialogMode = null;
        this.notice = `Duty record added for ${personName}.`;
        if (personName) this.refreshPeople(personName);
      },
      error: error => this.error = this.errorMessage(error, 'Could not add this duty. Check that its start date follows the current record.')
    });
  }

  formatDate(value: string | null): string {
    if (!value) return '—';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('en', {
      day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC'
    }).format(date);
  }

  private loadDuties(person: Person): void {
    if (!this.isAstronaut(person)) {
      this.duties = [];
      return;
    }
    this.loadingDuties = true;
    this.api.getDuties(person.name).pipe(finalize(() => this.loadingDuties = false)).subscribe({
      next: response => {
        this.duties = response.duties;
        this.selectedPerson = response.person;
      },
      error: error => this.error = this.errorMessage(error, 'Could not load this duty history.')
    });
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const body = (error as { error?: { message?: unknown } }).error;
      if (typeof body?.message === 'string' && body.message !== 'Bad Request') return body.message;
    }
    return fallback;
  }

  private today(): string {
    const date = new Date();
    const offset = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 10);
  }
}