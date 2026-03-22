import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../contants/constants';
import { Users } from './users.interface';

@Injectable({
  providedIn: 'root',
})
export class UsersService {
  private readonly http = inject(HttpClient);
  private baseUrl = environment.apiUrl

  public getAllUsers(){
   return this.http.get<Users[]>(this.baseUrl+'/user')
  }
}
