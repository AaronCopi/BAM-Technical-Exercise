export interface Person {
  personId: number;
  name: string;
  currentRank: string;
  currentDutyTitle: string;
  careerStartDate: string | null;
  careerEndDate: string | null;
}

export interface AstronautDuty {
  id: number;
  personId: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface PeopleResponse {
  people: Person[];
}

export interface DutiesResponse {
  person: Person;
  astronautDuties: AstronautDuty[];
}

export interface CreateDutyRequest {
  name: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
}