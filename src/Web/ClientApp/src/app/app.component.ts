import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  title = 'app';
  
  constructor(public router: Router) {}

  isPublicRoute(): boolean {
    return this.router.url.includes('/careers') || this.router.url.includes('/login');
  }
}
