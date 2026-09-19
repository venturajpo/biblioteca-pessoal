import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Biblioteca } from './biblioteca';

describe('Biblioteca', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Biblioteca],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Biblioteca);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
