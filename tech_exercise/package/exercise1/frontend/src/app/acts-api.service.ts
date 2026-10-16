import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { AstronautDuty, CreateDutyRequest, DutiesResponse, PeopleResponse, Person } from './models';

@Injectable({ providedIn: 'root' })
export class ActsApiService {
  private readonly http = inject(HttpClient);
  private readonly jsonHeaders = new HttpHeaders({ 'Content-Type': 'application/json' });

  getPeople(): Observable<Person[]> {
    return this.http.get<PeopleResponse>('/Person').pipe(map(response => response.people ?? []));
  }

  getDuties(name: string): Observable<{ person: Person; duties: AstronautDuty[] }> {
    return this.http.get<DutiesResponse>(`/AstronautDuty/${encodeURIComponent(name)}`).pipe(
      map(response => ({ person: response.person, duties: response.astronautDuties ?? [] }))
    );
  }

  createPerson(name: string): Observable<unknown> {
    return this.http.post('/Person', JSON.stringify(name), { headers: this.jsonHeaders });
  }

  renamePerson(name: string, newName: string): Observable<unknown> {
    return this.http.put(`/Person/${encodeURIComponent(name)}`, JSON.stringify(newName), { headers: this.jsonHeaders });
  }

  createDuty(request: CreateDutyRequest): Observable<unknown> {
    return this.http.post('/AstronautDuty', request);
  }
}