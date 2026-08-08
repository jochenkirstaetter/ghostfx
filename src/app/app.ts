import { ChangeDetectionStrategy, Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

interface FileItem {
  path: string;
  name: string;
  isDraft: boolean;
  isIndex: boolean;
  isToc: boolean;
  isTag: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);

  activeTab = signal<'dashboard' | 'config' | 'json' | 'output' | 'tests'>('dashboard');

  configForm: FormGroup = this.fb.group({
    ghostUrl: ['https://demo.ghost.io'],
    adminApiKey: ['640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef'],
    inputJsonPath: ['sample-ghost-export.json'],
    outputDir: ['articles'],
    indexFile: ['index.md'],
    siteTitle: ['GhostFx Sample Blog'],
    includeDrafts: [true],
    downloadTheme: [false],
    themeOutputPath: ['templates/ghost-theme.zip']
  });

  customJsonInput = signal<string>('');
  isRunningMigration = signal<boolean>(false);
  isRunningTests = signal<boolean>(false);
  migrationOutput = signal<string>('');
  testOutput = signal<string>('');
  testStatus = signal<'none' | 'success' | 'failed'>('none');
  
  generatedFiles = signal<FileItem[]>([]);
  selectedFile = signal<FileItem | null>(null);
  selectedFileContent = signal<string>('');

  statusMessage = signal<string>('Ready');

  ngOnInit() {
    this.loadConfig();
    this.loadSampleJson();
    this.loadFiles();
  }

  loadConfig() {
    this.http.get<any>('/api/ghostfx/config').subscribe({
      next: (data) => {
        if (data) {
          this.configForm.patchValue(data);
        }
      },
      error: (err) => console.error('Failed to load config', err)
    });
  }

  loadSampleJson() {
    this.http.get<any>('/api/ghostfx/sample-export').subscribe({
      next: (data) => {
        this.customJsonInput.set(JSON.stringify(data, null, 2));
      },
      error: (err) => console.error('Failed to load sample JSON', err)
    });
  }

  saveConfig() {
    const configData = this.configForm.value;
    this.http.post('/api/ghostfx/config', configData).subscribe({
      next: () => {
        this.statusMessage.set('Configuration saved to ghostfx.json!');
        setTimeout(() => this.statusMessage.set('Ready'), 3000);
      },
      error: (err) => alert('Failed to save config: ' + err.message)
    });
  }

  runMigration() {
    this.isRunningMigration.set(true);
    this.statusMessage.set('Executing C# GhostFx Migration Engine...');
    
    let jsonPayload: any = null;
    try {
      if (this.customJsonInput().trim()) {
        jsonPayload = JSON.parse(this.customJsonInput());
      }
    } catch {
      alert('Invalid JSON in JSON Payload editor');
      this.isRunningMigration.set(false);
      return;
    }

    const payload = {
      config: this.configForm.value,
      customJson: jsonPayload
    };

    this.http.post<any>('/api/ghostfx/migrate', payload).subscribe({
      next: (res) => {
        this.isRunningMigration.set(false);
        this.migrationOutput.set(res.stdout + '\n' + (res.stderr || ''));
        this.statusMessage.set('Migration completed!');
        this.loadFiles();
        this.activeTab.set('output');
      },
      error: (err) => {
        this.isRunningMigration.set(false);
        this.migrationOutput.set('Error executing migration:\n' + (err.error?.error || err.message) + '\n' + (err.error?.stderr || ''));
        this.statusMessage.set('Migration failed.');
      }
    });
  }

  runTests() {
    this.isRunningTests.set(true);
    this.testStatus.set('none');
    this.statusMessage.set('Executing .NET xUnit Test Suite...');

    this.http.post<any>('/api/ghostfx/test', {}).subscribe({
      next: (res) => {
        this.isRunningTests.set(false);
        this.testOutput.set(res.stdout);
        this.testStatus.set('success');
        this.statusMessage.set('All C# unit and integration tests passed!');
        this.activeTab.set('tests');
      },
      error: (err) => {
        this.isRunningTests.set(false);
        this.testOutput.set(err.error?.stdout || err.error?.error || err.message);
        this.testStatus.set('failed');
        this.statusMessage.set('Tests failed.');
        this.activeTab.set('tests');
      }
    });
  }

  loadFiles() {
    this.http.get<{ files: FileItem[] }>('/api/ghostfx/files').subscribe({
      next: (res) => {
        this.generatedFiles.set(res.files || []);
        if (res.files && res.files.length > 0 && !this.selectedFile()) {
          this.viewFile(res.files[0]);
        }
      },
      error: (err) => console.error('Failed to load generated files', err)
    });
  }

  viewFile(file: FileItem) {
    this.selectedFile.set(file);
    this.http.get<{ path: string; content: string }>(`/api/ghostfx/file-content?path=${encodeURIComponent(file.path)}`).subscribe({
      next: (res) => {
        this.selectedFileContent.set(res.content);
      },
      error: (err) => {
        this.selectedFileContent.set('Failed to read file content.');
      }
    });
  }

  onJsonTextChange(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.customJsonInput.set(value);
  }
}
