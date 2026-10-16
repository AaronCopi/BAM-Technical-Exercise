import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ActsApiService } from './acts-api.service';
import { AstronautDuty, Person } from './models';

describe('ActsApiService', () => {
  let service: ActsApiService;
  let httpTesting: HttpTestingController;

  const person: Person = {
    personId: 7,
    name: 'Jane Doe',
    currentRank: 'Commander',
    currentDutyTitle: 'Mission Specialist',
    careerStartDate: '2020-01-01',
    careerEndDate: null
  };

  const duty: AstronautDuty = {
    id: 12,
    personId: person.personId,
    rank: 'Commander',
    dutyTitle: 'Mission Specialist',
    dutyStartDate: '2020-01-01',
    dutyEndDate: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ActsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('retrieves the people collection', () => {
    let result: Person[] | undefined;

    service.getPeople().subscribe(people => result = people);
    const request = httpTesting.expectOne('/Person');
    expect(request.request.method).toBe('GET');
    request.flush({ people: [person] });

    expect(result).toEqual([person]);
  });

  it('retrieves and maps duties for an encoded person name', () => {
    let result: { person: Person; duties: AstronautDuty[] } | undefined;

    service.getDuties('Jane Doe').subscribe(response => result = response);
    const request = httpTesting.expectOne('/AstronautDuty/Jane%20Doe');
    expect(request.request.method).toBe('GET');
    request.flush({ person, astronautDuties: [duty] });

    expect(result).toEqual({ person, duties: [duty] });
  });

  it('creates a person using the API raw JSON string body', () => {
    service.createPerson('Jane Doe').subscribe();
    const request = httpTesting.expectOne('/Person');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBe(JSON.stringify('Jane Doe'));
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ id: person.personId });
  });

  it('renames a person using the API raw JSON string body', () => {
    service.renamePerson('Jane Doe', 'Jane Carter').subscribe();
    const request = httpTesting.expectOne('/Person/Jane%20Doe');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toBe(JSON.stringify('Jane Carter'));
    request.flush({ id: person.personId });
  });

  it('posts a new duty with the supplied date-only value', () => {
    const requestBody = {
      name: person.name,
      rank: duty.rank,
      dutyTitle: duty.dutyTitle,
      dutyStartDate: '2025-04-03'
    };

    service.createDuty(requestBody).subscribe();
    const request = httpTesting.expectOne('/AstronautDuty');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    request.flush({ id: duty.id });
  });
});