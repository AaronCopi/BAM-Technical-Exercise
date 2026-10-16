import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ActsApiService } from './acts-api.service';
import { AppComponent } from './app.component';
import { AstronautDuty, Person } from './models';

const astronaut: Person = {
  personId: 2,
  name: 'Samantha Carter',
  currentRank: 'Colonel',
  currentDutyTitle: 'Mission Specialist',
  careerStartDate: '2010-01-01',
  careerEndDate: null
};

const civilian: Person = {
  personId: 1,
  name: 'Daniel Jackson',
  currentRank: '',
  currentDutyTitle: '',
  careerStartDate: null,
  careerEndDate: null
};

const duty: AstronautDuty = {
  id: 9,
  personId: astronaut.personId,
  rank: astronaut.currentRank,
  dutyTitle: astronaut.currentDutyTitle,
  dutyStartDate: '2010-01-01',
  dutyEndDate: null
};

function createApiMock() {
  return {
    getPeople: vi.fn(() => of([astronaut, civilian])),
    getDuties: vi.fn(() => of({ person: astronaut, duties: [duty] })),
    createPerson: vi.fn(() => of({ id: 3 })),
    renamePerson: vi.fn(() => of({ id: civilian.personId })),
    createDuty: vi.fn(() => of({ id: 10 }))
  };
}

describe('AppComponent', () => {
  let api: ReturnType<typeof createApiMock>;
  let component: AppComponent;

  beforeEach(async () => {
    api = createApiMock();
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [{ provide: ActsApiService, useValue: api }]
    }).compileComponents();

    component = TestBed.createComponent(AppComponent).componentInstance;
  });

  it('sorts the directory and calculates astronaut and retirement totals', () => {
    expect(component.people.map(person => person.name)).toEqual(['Daniel Jackson', 'Samantha Carter']);
    expect(component.astronautCount).toBe(1);
    expect(component.retiredCount).toBe(0);
  });

  it('filters names without regard to case', () => {
    component.searchTerm = 'sAmAnThA';

    expect(component.filteredPeople).toEqual([astronaut]);
  });

  it('loads duty history when an astronaut is selected', () => {
    component.selectPerson(astronaut);

    expect(api.getDuties).toHaveBeenCalledWith(astronaut.name);
    expect(component.duties).toEqual([duty]);
  });

  it('trims and creates a person, then refreshes the directory', () => {
    component.openDialog('create');
    component.personName = '  Adria  ';

    component.submitPerson();

    expect(api.createPerson).toHaveBeenCalledWith('Adria');
    expect(component.notice).toBe('Adria added to the directory.');
    expect(component.dialogMode).toBeNull();
    expect(api.getPeople).toHaveBeenCalledTimes(2);
  });

  it('renames the selected person and preserves the new selection', () => {
    const renamedPerson = { ...civilian, name: 'Dr. Jackson' };
    component.selectPerson(civilian);
    component.openDialog('rename');
    component.newName = '  Dr. Jackson  ';
    api.getPeople.mockReturnValue(of([astronaut, renamedPerson]));

    component.submitRename();

    expect(api.renamePerson).toHaveBeenCalledWith(civilian.name, 'Dr. Jackson');
    expect(component.notice).toBe('Record renamed to Dr. Jackson.');
    expect(component.selectedPerson?.name).toBe('Dr. Jackson');
    expect(api.getPeople).toHaveBeenCalledTimes(2);
  });

  it('submits a trimmed duty with its date-only value', () => {
    component.selectPerson(astronaut);
    component.openDialog('duty');
    component.dutyRank = '  Brigadier General ';
    component.dutyTitle = '  Base Commander ';
    component.dutyStartDate = '2025-04-03';

    component.submitDuty();

    expect(api.createDuty).toHaveBeenCalledWith({
      name: astronaut.name,
      rank: 'Brigadier General',
      dutyTitle: 'Base Commander',
      dutyStartDate: '2025-04-03'
    });
    expect(component.notice).toBe(`Duty record added for ${astronaut.name}.`);
  });
});