import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Detalhes } from './detalhes';

describe('Detalhes', () => {
  let component: Detalhes;
  let fixture: ComponentFixture<Detalhes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Detalhes],
    }).compileComponents();

    fixture = TestBed.createComponent(Detalhes);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
