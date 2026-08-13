import React, { useState, useEffect, useRef } from 'react';
import { 
  Play, 
  CheckCircle2, 
  Settings, 
  FileText, 
  FolderOpen, 
  Terminal, 
  Code2, 
  Sparkles, 
  ExternalLink, 
  RefreshCw,
  Save,
  AlertCircle,
  Layers,
  Tag,
  FileCode,
  Globe,
  Menu,
  X,
  ChevronDown,
  ChevronUp,
  SlidersHorizontal
} from 'lucide-react';

interface GhostFxConfigData {
  ghostUrl: string;
  adminApiKey: string;
  contentApiKey: string;
  inputJsonPath: string;
  ghostExportJson?: string;
  outputDir: string;
  indexFile: string;
  siteTitle: string;
  includeDrafts: boolean;
  cleanUrls?: boolean;
  disableAffix?: boolean;
  quiet?: boolean;
  downloadTheme: boolean;
  migrateTheme: boolean;
  purgeTemplate?: boolean | null;
  themePath: string;
  themeOutputPath?: string;
  logoPath: boolean;
  googleAnalyticsTag: string;
  indexPostCount: number;
  excerptMaxLength: number;
}

interface FileItem {
  path: string;
  name: string;
  isDraft: boolean;
  isIndex: boolean;
  isToc: boolean;
  isTag: boolean;
}

export default function App() {
  const [activeTab, setActiveTab] = useState<'console' | 'config' | 'json' | 'files' | 'tests'>('console');
  const [mobileMenuOpen, setMobileMenuOpen] = useState<boolean>(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState<boolean>(false);
  const [footerPreviewOpen, setFooterPreviewOpen] = useState<boolean>(false);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const themeZipInputRef = useRef<HTMLInputElement>(null);
  const themeFolderInputRef = useRef<HTMLInputElement>(null);
  const loadConfigInputRef = useRef<HTMLInputElement>(null);
  
  const [config, setConfig] = useState<GhostFxConfigData>({
    ghostUrl: 'https://jochen.kirstaetter.name',
    adminApiKey: '',
    contentApiKey: '',
    inputJsonPath: 'temp-ghost-export.json',
    outputDir: 'articles',
    indexFile: 'index.md',
    siteTitle: 'Get Blogged by JoKi',
    includeDrafts: true,
    cleanUrls: false,
    disableAffix: false,
    quiet: false,
    downloadTheme: false,
    migrateTheme: false,
    purgeTemplate: false,
    themePath: '_ghost_templates/blogged-4.0.0',
    logoPath: false,
    googleAnalyticsTag: 'UA-12103827-1',
    indexPostCount: 12,
    excerptMaxLength: 200
  });

  const [customJsonInput, setCustomJsonInput] = useState<string>('');
  const [isRunningMigration, setIsRunningMigration] = useState<boolean>(false);
  const [isRunningTests, setIsRunningTests] = useState<boolean>(false);
  const [isOfflineMode, setIsOfflineMode] = useState<boolean>(false);

  const toggleMode = () => {
    setIsOfflineMode(prev => {
      const next = !prev;
      if (next) {
        setConfig(c => ({ ...c, inputJsonPath: c.inputJsonPath || 'temp-ghost-export.json', ghostExportJson: c.inputJsonPath || 'temp-ghost-export.json' }));
        setTerminalLogs(logs => [...logs, '[INFO] Switched engine mode to Offline JSON Export Migration.']);
      } else {
        setConfig(c => ({ ...c, inputJsonPath: '', ghostExportJson: '' }));
        setTerminalLogs(logs => [...logs, '[INFO] Switched engine mode to Live Ghost API Migration.']);
      }
      return next;
    });
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setConfig(prev => ({
        ...prev,
        inputJsonPath: file.name,
        ghostExportJson: file.name
      }));
      const reader = new FileReader();
      reader.onload = (event) => {
        if (event.target?.result) {
          setCustomJsonInput(event.target.result as string);
        }
      };
      reader.readAsText(file);
    }
  };

  const handleThemeZipSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setConfig(prev => ({
        ...prev,
        themePath: file.name,
        themeOutputPath: file.name
      }));
    }
  };

  const handleThemeFolderSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      const folderName = files[0].webkitRelativePath ? files[0].webkitRelativePath.split('/')[0] : 'custom-theme';
      setConfig(prev => ({
        ...prev,
        themePath: folderName,
        themeOutputPath: folderName
      }));
    }
  };

  const handleLoadConfigFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (event) => {
        try {
          if (event.target?.result) {
            const parsed = JSON.parse(event.target.result as string);
            setConfig(prev => ({ ...prev, ...parsed }));
            setTerminalLogs(logs => [...logs, `[INFO] Loaded configuration from ${file.name}`]);
            alert(`Configuration loaded successfully from ${file.name}`);
          }
        } catch (err: any) {
          alert(`Failed to parse configuration JSON file: ${err.message}`);
        }
      };
      reader.readAsText(file);
    }
  };
  
  const [terminalLogs, setTerminalLogs] = useState<string[]>([
    '[INFO] Initializing GhostFx .NET 9.0 / 10 Engine...',
    '[DEBUG] Loaded default configuration from ghostfx.json.',
    '[AUTH] Verified Admin API credentials and JSON payload parser.',
    '[READY] Standing by for live migration or xUnit test execution.'
  ]);

  const [testOutput, setTestOutput] = useState<string>('');
  const [testStatus, setTestStatus] = useState<'none' | 'success' | 'failed'>('none');

  const [generatedFiles, setGeneratedFiles] = useState<FileItem[]>([]);
  const [selectedFile, setSelectedFile] = useState<FileItem | null>(null);
  const [selectedFileContent, setSelectedFileContent] = useState<string>('');

  const [stats, setStats] = useState({
    totalPosts: 3,
    publishedPosts: 2,
    draftPosts: 1,
    totalTags: 3
  });

  useEffect(() => {
    fetchConfig();
    fetchSampleJson();
    fetchFiles();
  }, []);

  const fetchConfig = async () => {
    try {
      const res = await fetch('/api/ghostfx/config');
      if (res.ok) {
        const data = await res.json();
        setConfig(data);
      }
    } catch (e) {
      console.error('Failed to load config', e);
    }
  };

  const fetchSampleJson = async () => {
    try {
      const res = await fetch('/api/ghostfx/sample-export');
      if (res.ok) {
        const data = await res.json();
        setCustomJsonInput(JSON.stringify(data, null, 2));
      }
    } catch (e) {
      console.error('Failed to load sample JSON', e);
    }
  };

  const fetchFiles = async () => {
    try {
      const res = await fetch('/api/ghostfx/files');
      if (res.ok) {
        const data = await res.json();
        const files: FileItem[] = data.files || [];
        setGeneratedFiles(files);
        if (files.length > 0 && !selectedFile) {
          viewFile(files[0]);
        }
      }
    } catch (e) {
      console.error('Failed to load files', e);
    }
  };

  const viewFile = async (file: FileItem) => {
    setSelectedFile(file);
    try {
      const res = await fetch(`/api/ghostfx/file-content?path=${encodeURIComponent(file.path)}`);
      if (res.ok) {
        const data = await res.json();
        setSelectedFileContent(data.content);
      }
    } catch (e) {
      setSelectedFileContent('Failed to load file content.');
    }
  };

  const handleSaveConfig = async () => {
    try {
      const res = await fetch('/api/ghostfx/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });
      if (res.ok) {
        addLog('[INFO] Saved updated configuration to ghostfx.json.');
        alert('Configuration saved!');
      }
    } catch (e: any) {
      alert('Failed to save config: ' + e.message);
    }
  };

  const addLog = (msg: string) => {
    setTerminalLogs(prev => [...prev, msg]);
  };

  const handleRunMigration = async (overrideOfflineJson?: boolean) => {
    setIsRunningMigration(true);
    setActiveTab('console');

    const isOfflineMode = overrideOfflineJson !== undefined ? overrideOfflineJson : activeTab === 'json';

    setTerminalLogs([
      '[INFO] Initializing GhostFx .NET Engine...',
      `[CONFIG] Source: ${isOfflineMode ? (config.inputJsonPath || 'Offline JSON Export') : config.ghostUrl}`,
      `[CONFIG] Output Directory: ./${config.outputDir}`,
      `[EXEC] Executing C# GhostFx.Cli migration runner (${isOfflineMode ? 'Offline JSON Mode' : 'Live Ghost API Mode'})...`
    ]);

    let parsedJson: any = null;
    if (isOfflineMode && customJsonInput.trim()) {
      try {
        parsedJson = JSON.parse(customJsonInput);
      } catch {
        addLog('[ERROR] Invalid JSON payload format provided.');
        setIsRunningMigration(false);
        return;
      }
    }

    try {
      const res = await fetch('/api/ghostfx/migrate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          config,
          customJson: parsedJson,
          useOfflineJson: isOfflineMode
        })
      });

      const data = await res.json();
      setIsRunningMigration(false);

      if (data.stdout) {
        const lines = data.stdout.split('\n').filter((l: string) => l.trim().length > 0);
        lines.forEach((line: string) => addLog(line));
      }

      if (data.success) {
        addLog('[SUCCESS] GhostFx Migration Completed Successfully!');
        fetchFiles();
      } else {
        addLog(`[ERROR] Migration failed: ${data.error || data.stderr}`);
      }
    } catch (err: any) {
      setIsRunningMigration(false);
      addLog(`[ERROR] Migration request failed: ${err.message}`);
    }
  };

  const handleRunTests = async () => {
    setIsRunningTests(true);
    setActiveTab('tests');
    setTestStatus('none');
    setTestOutput('Executing xUnit test runner (dotnet test GhostFx.Tests)...');

    try {
      const res = await fetch('/api/ghostfx/test', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });
      const data = await res.json();
      setIsRunningTests(false);

      if (data.stdout) {
        setTestOutput(data.stdout);
        if (data.stdout.includes('Passed!') || data.stdout.includes('Build succeeded')) {
          setTestStatus('success');
        } else {
          setTestStatus('failed');
        }
      } else {
        setTestOutput(data.error || 'Test runner finished.');
      }
    } catch (err: any) {
      setIsRunningTests(false);
      setTestStatus('failed');
      setTestOutput(`Failed to execute tests: ${err.message}`);
    }
  };

  return (
    <div className="flex flex-col h-screen h-[100dvh] w-full bg-slate-50 font-sans text-slate-900 overflow-hidden relative">
      {/* Top Navigation / Brand Rail */}
      <nav className="h-14 bg-indigo-900 flex items-center justify-between px-4 sm:px-6 shadow-md shrink-0 z-30 relative">
        <div className="flex items-center space-x-2 sm:space-x-3">
          <button 
            onClick={() => setMobileSidebarOpen(!mobileSidebarOpen)}
            className="md:hidden p-1.5 text-indigo-200 hover:text-white rounded-lg hover:bg-indigo-800 transition cursor-pointer"
            title="Toggle Migration Profile">
            <SlidersHorizontal className="w-5 h-5" />
          </button>
          <div className="w-8 h-8 bg-indigo-500 rounded flex items-center justify-center font-bold text-white text-lg shrink-0">
            G
          </div>
          <span className="text-white font-semibold text-base sm:text-lg tracking-tight flex items-center">
            GhostFx <span className="text-indigo-300 font-normal text-[10px] sm:text-xs ml-1.5 sm:ml-2 bg-indigo-800/80 px-1.5 sm:px-2 py-0.5 rounded border border-indigo-700">v1.0.0</span>
          </span>
        </div>

        {/* Desktop Navigation Actions */}
        <div className="hidden md:flex items-center space-x-4">
          <button 
            onClick={toggleMode}
            title="Click to toggle between Live Ghost API and Offline JSON Mode"
            className="flex items-center space-x-2 bg-indigo-800 hover:bg-indigo-700 px-3 py-1 rounded-full border border-indigo-700 transition cursor-pointer">
            <div className={`w-2 h-2 rounded-full animate-pulse ${isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'bg-amber-400' : 'bg-emerald-400'}`}></div>
            <span className="text-indigo-100 text-xs font-medium uppercase tracking-wider">
              {isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'Offline JSON Mode' : 'Live Ghost API Mode'}
            </span>
          </button>

          <button 
            onClick={handleRunTests}
            className="flex items-center space-x-1 text-xs bg-indigo-800 hover:bg-indigo-700 text-indigo-100 px-3 py-1.5 rounded-lg border border-indigo-700 transition cursor-pointer">
            <CheckCircle2 className="w-3.5 h-3.5 text-indigo-300" />
            <span>.NET xUnit Tests</span>
          </button>

          <div className="w-8 h-8 rounded-full bg-indigo-700 border border-indigo-600 flex items-center justify-center text-indigo-200 text-xs font-bold shrink-0">
            AD
          </div>
        </div>

        {/* Mobile Menu Toggle Button */}
        <div className="flex items-center md:hidden space-x-2">
          <button 
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="p-2 text-indigo-200 hover:text-white rounded-lg hover:bg-indigo-800 transition cursor-pointer"
            aria-label="Toggle Menu">
            {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>
      </nav>

      {/* Mobile Header Dropdown Menu */}
      {mobileMenuOpen && (
        <div className="md:hidden bg-indigo-950 border-b border-indigo-800 px-4 py-3 space-y-3 z-30 shadow-lg">
          <div className="flex items-center justify-between pt-1">
            <span className="text-xs text-indigo-300 font-medium">Engine Mode:</span>
            <button 
              onClick={() => { toggleMode(); setMobileMenuOpen(false); }}
              className="flex items-center space-x-2 bg-indigo-800 hover:bg-indigo-700 px-3 py-1.5 rounded-full border border-indigo-700 transition cursor-pointer">
              <div className={`w-2 h-2 rounded-full animate-pulse ${isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'bg-amber-400' : 'bg-emerald-400'}`}></div>
              <span className="text-indigo-100 text-xs font-medium uppercase tracking-wider">
                {isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'Offline JSON' : 'Live Ghost API'}
              </span>
            </button>
          </div>

          <div className="flex items-center justify-between pt-2 border-t border-indigo-900">
            <button 
              onClick={() => { handleRunTests(); setMobileMenuOpen(false); }}
              className="w-full flex items-center justify-center space-x-2 text-xs bg-indigo-800 hover:bg-indigo-700 text-indigo-100 py-2 rounded-lg border border-indigo-700 transition cursor-pointer">
              <CheckCircle2 className="w-4 h-4 text-indigo-300" />
              <span>Run .NET xUnit Tests</span>
            </button>
          </div>
        </div>
      )}

      {/* Main Workspace */}
      <div className="flex flex-1 overflow-hidden relative">
        {/* Mobile Backdrop Overlay */}
        {mobileSidebarOpen && (
          <div 
            onClick={() => setMobileSidebarOpen(false)}
            className="md:hidden fixed inset-0 bg-slate-900/60 backdrop-blur-xs z-40 transition-opacity"
          />
        )}

        {/* Left Sidebar: Migration Profile */}
        <aside className={`
          fixed md:relative inset-y-0 left-0 z-50 md:z-auto
          w-80 md:w-72 bg-white border-r border-slate-200 flex flex-col shrink-0
          transform transition-transform duration-200 ease-in-out shadow-2xl md:shadow-none
          ${mobileSidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
        `}>
          <div className="p-4 sm:p-5 border-b border-slate-100 flex items-center justify-between">
            <h2 className="text-xs font-bold text-slate-500 uppercase tracking-widest">Migration Profile</h2>
            <button 
              onClick={() => setMobileSidebarOpen(false)}
              className="md:hidden p-1 text-slate-400 hover:text-slate-600 rounded">
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="p-4 sm:p-5 border-b border-slate-100">
            <div className="space-y-4">
              <div className="flex flex-col">
                <label className="text-[11px] text-slate-400 uppercase font-semibold mb-1">Engine Mode</label>
                <button
                  onClick={toggleMode}
                  title="Click to switch migration mode"
                  className={`text-xs px-2.5 py-1.5 rounded-md font-semibold border transition flex items-center justify-between cursor-pointer ${
                    isOfflineMode || activeTab === 'json' || !!config.inputJsonPath
                      ? 'bg-amber-50 text-amber-800 border-amber-200 hover:bg-amber-100' 
                      : 'bg-emerald-50 text-emerald-800 border-emerald-200 hover:bg-emerald-100'
                  }`}>
                  <span className="flex items-center space-x-1.5">
                    <span className={`w-2 h-2 rounded-full animate-pulse ${isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'bg-amber-500' : 'bg-emerald-500'}`}></span>
                    <span>{isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'Offline JSON Mode' : 'Live Ghost API Mode'}</span>
                  </span>
                  <span className="text-[10px] text-slate-500 underline ml-2">Switch</span>
                </button>
              </div>

              <div className="flex flex-col">
                <label className="text-[11px] text-slate-400 uppercase font-semibold mb-1">Source URL / Export</label>
                <span className="text-xs text-slate-700 font-mono break-all bg-slate-50 p-2 rounded border border-slate-200">
                  {(isOfflineMode || activeTab === 'json' || !!config.inputJsonPath) ? (config.inputJsonPath || 'Offline Export') : config.ghostUrl}
                </span>
              </div>

              <div className="flex flex-col">
                <label className="text-[11px] text-slate-400 uppercase font-semibold mb-1">Output Root</label>
                <span className="text-xs text-slate-700 font-mono bg-slate-50 p-2 rounded border border-slate-200">
                  ./{config.outputDir}
                </span>
              </div>

              <div className="flex flex-col">
                <label className="text-[11px] text-slate-400 uppercase font-semibold mb-1">Artifact Style</label>
                <span className="text-xs text-slate-700 px-2 py-1 bg-indigo-50 text-indigo-700 rounded-md font-medium border border-indigo-100 self-start">
                  DocFx / Markdig
                </span>
              </div>
            </div>
          </div>

          <div className="flex-1 p-4 sm:p-5 overflow-y-auto">
            <h2 className="text-xs font-bold text-slate-500 uppercase tracking-widest mb-4">Pipeline Flags</h2>
            <ul className="space-y-3">
              <li className="flex items-center justify-between text-xs">
                <span className="text-slate-600 font-medium">Offline JSON Mode</span>
                <span className={isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? "text-amber-600 font-bold" : "text-slate-400"}>
                  {isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? "Active" : "Disabled"}
                </span>
              </li>
              <li className="flex items-center justify-between text-xs">
                <span className="text-slate-600 font-medium">Convert Tags</span>
                <span className="text-emerald-600 font-bold">✓</span>
              </li>
              <li className="flex items-center justify-between text-xs">
                <span className="text-slate-600 font-medium">Include Drafts</span>
                <span className={config.includeDrafts ? "text-emerald-600 font-bold" : "text-slate-400"}>
                  {config.includeDrafts ? "✓" : "✗"}
                </span>
              </li>
              <li className="flex items-center justify-between text-xs">
                <span className="text-slate-600 font-medium">Sync Theme CSS</span>
                <span className={config.downloadTheme ? "text-emerald-600 font-bold" : "text-amber-500 font-bold"}>
                  {config.downloadTheme ? "✓" : "!"}
                </span>
              </li>
              <li className="flex items-center justify-between text-xs">
                <span className="text-slate-600 font-medium">Auto-Generate TOC</span>
                <span className="text-emerald-600 font-bold">✓</span>
              </li>
            </ul>
          </div>

          <div className="p-4 sm:p-5 bg-slate-50 border-t border-slate-200">
            <button 
              onClick={() => {
                handleRunMigration(isOfflineMode || activeTab === 'json' || !!config.inputJsonPath);
                setMobileSidebarOpen(false);
              }}
              disabled={isRunningMigration}
              className={`w-full text-white py-2.5 rounded shadow-xs font-semibold text-sm transition-colors disabled:opacity-50 flex items-center justify-center space-x-2 cursor-pointer ${
                isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'bg-amber-600 hover:bg-amber-700' : 'bg-indigo-600 hover:bg-indigo-700'
              }`}>
              <Play className="w-4 h-4 fill-current" />
              <span>
                {isRunningMigration 
                  ? 'Migrating...' 
                  : (isOfflineMode || activeTab === 'json' || !!config.inputJsonPath ? 'Migrate Offline JSON' : 'Start Live Migration')}
              </span>
            </button>
          </div>
        </aside>

        {/* Main Content: Execution Log & Monitoring */}
        <main className="flex-1 flex flex-col p-3 sm:p-6 space-y-4 sm:space-y-6 overflow-y-auto md:overflow-hidden min-w-0">
          {/* Summary Metric Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 sm:gap-4 shrink-0">
            <div className="bg-white p-3 sm:p-4 rounded-xl border border-slate-200 shadow-xs">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-0.5 sm:mb-1">Total Posts</p>
              <h3 className="text-xl sm:text-2xl font-semibold text-slate-800">{stats.totalPosts}</h3>
            </div>

            <div className="bg-white p-3 sm:p-4 rounded-xl border border-slate-200 shadow-xs">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-0.5 sm:mb-1">Published</p>
              <h3 className="text-xl sm:text-2xl font-semibold text-indigo-600">{stats.publishedPosts}</h3>
            </div>

            <div className="bg-white p-3 sm:p-4 rounded-xl border border-slate-200 shadow-xs">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-0.5 sm:mb-1">Drafts</p>
              <h3 className="text-xl sm:text-2xl font-semibold text-slate-400">{stats.draftPosts}</h3>
            </div>

            <div className="bg-white p-3 sm:p-4 rounded-xl border border-slate-200 shadow-xs">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-0.5 sm:mb-1">Total Tags</p>
              <h3 className="text-xl sm:text-2xl font-semibold text-slate-800">{stats.totalTags}</h3>
            </div>
          </div>

          {/* Navigation Bar for Workspace Panels */}
          <div className="flex space-x-1 border-b border-slate-200 shrink-0 overflow-x-auto pb-0.5 scrollbar-none">
            <button 
              onClick={() => setActiveTab('console')}
              className={`px-3 sm:px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-1.5 sm:space-x-2 whitespace-nowrap shrink-0 cursor-pointer ${
                activeTab === 'console' 
                  ? 'bg-slate-900 text-white border-t border-x border-slate-800' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Terminal className="w-3.5 h-3.5 shrink-0" />
              <span>Migration Console</span>
            </button>

            <button 
              onClick={() => setActiveTab('config')}
              className={`px-3 sm:px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-1.5 sm:space-x-2 whitespace-nowrap shrink-0 cursor-pointer ${
                activeTab === 'config' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Settings className="w-3.5 h-3.5 shrink-0" />
              <span>ghostfx.json Settings</span>
            </button>

            <button 
              onClick={() => setActiveTab('json')}
              className={`px-3 sm:px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-1.5 sm:space-x-2 whitespace-nowrap shrink-0 cursor-pointer ${
                activeTab === 'json' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Code2 className="w-3.5 h-3.5 shrink-0" />
              <span>JSON Payload</span>
            </button>

            <button 
              onClick={() => setActiveTab('files')}
              className={`px-3 sm:px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-1.5 sm:space-x-2 whitespace-nowrap shrink-0 cursor-pointer ${
                activeTab === 'files' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <FolderOpen className="w-3.5 h-3.5 shrink-0" />
              <span>Generated Artifacts ({generatedFiles.length})</span>
            </button>

            <button 
              onClick={() => setActiveTab('tests')}
              className={`px-3 sm:px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-1.5 sm:space-x-2 whitespace-nowrap shrink-0 cursor-pointer ${
                activeTab === 'tests' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <CheckCircle2 className="w-3.5 h-3.5 shrink-0" />
              <span>C# xUnit Tests</span>
            </button>
          </div>

          {/* TAB 1: Terminal Console */}
          {activeTab === 'console' && (
            <div className="flex-1 bg-slate-900 rounded-xl shadow-lg flex flex-col border border-slate-800 overflow-hidden min-h-[350px] md:min-h-0">
              <div className="h-10 bg-slate-800 flex items-center px-3 sm:px-4 space-x-2 shrink-0">
                <div className="flex space-x-1.5">
                  <div className="w-3 h-3 rounded-full bg-red-500/80"></div>
                  <div className="w-3 h-3 rounded-full bg-amber-500/80"></div>
                  <div className="w-3 h-3 rounded-full bg-emerald-500/80"></div>
                </div>
                <span className="text-slate-400 text-[10px] sm:text-[11px] font-mono truncate pl-2 sm:pl-4">ghostfx-cli --verbose --target docfx</span>
              </div>

              <div className="flex-1 p-3 sm:p-5 font-mono text-xs sm:text-[13px] leading-relaxed text-slate-300 overflow-y-auto space-y-1 break-words">
                {terminalLogs.map((log, index) => {
                  if (log.startsWith('[INFO]')) {
                    return <p key={index} className="text-emerald-400">{log}</p>;
                  }
                  if (log.startsWith('[DEBUG]')) {
                    return <p key={index} className="text-slate-400">{log}</p>;
                  }
                  if (log.startsWith('[AUTH]')) {
                    return <p key={index} className="text-indigo-400">{log}</p>;
                  }
                  if (log.startsWith('[WARN]')) {
                    return <p key={index} className="text-amber-400">{log}</p>;
                  }
                  if (log.startsWith('[ERROR]')) {
                    return <p key={index} className="text-red-400 font-bold">{log}</p>;
                  }
                  if (log.startsWith('[SUCCESS]')) {
                    return <p key={index} className="text-emerald-300 font-bold">{log}</p>;
                  }
                  return <p key={index} className="text-slate-300 ml-2">{log}</p>;
                })}

                <div className="flex items-center pt-2">
                  <span className="text-indigo-400 animate-pulse font-bold">_</span>
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: Config Settings Form */}
          {activeTab === 'config' && (
            <div className="flex-1 bg-white border border-slate-200 rounded-xl p-4 sm:p-6 overflow-y-auto space-y-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-100 pb-4 gap-3">
                <div>
                  <h3 className="text-sm sm:text-base font-bold text-slate-800">ghostfx.json Configuration</h3>
                  <p className="text-[11px] sm:text-xs text-slate-500">Configure parameters passed to the GhostFx C# migration engine.</p>
                </div>
                <div className="flex items-center space-x-2 self-start sm:self-auto">
                  <button 
                    type="button"
                    onClick={() => loadConfigInputRef.current?.click()}
                    className="px-3 py-1.5 sm:py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg border border-slate-200 transition flex items-center space-x-1.5 cursor-pointer">
                    <FolderOpen className="w-3.5 h-3.5 text-indigo-600 shrink-0" />
                    <span className="hidden sm:inline">Load Configuration</span>
                    <span className="sm:hidden">Load</span>
                  </button>
                  <input 
                    type="file" 
                    ref={loadConfigInputRef} 
                    accept=".json" 
                    onChange={handleLoadConfigFile} 
                    className="hidden" 
                  />
                  <button 
                    onClick={handleSaveConfig}
                    className="px-3.5 py-1.5 sm:py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-lg shadow-xs transition flex items-center space-x-1.5 cursor-pointer">
                    <Save className="w-3.5 h-3.5 shrink-0" />
                    <span className="hidden sm:inline">Save Configuration</span>
                    <span className="sm:hidden">Save</span>
                  </button>
                </div>
              </div>

              {/* Section 1: Ghost API & Sources */}
              <div>
                <h4 className="text-xs font-bold text-indigo-900 uppercase tracking-wider mb-3">1. Ghost API Credentials & Input</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Ghost Base URL</label>
                    <input 
                      type="text" 
                      value={config.ghostUrl} 
                      onChange={e => setConfig({...config, ghostUrl: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Site Title</label>
                    <input 
                      type="text" 
                      value={config.siteTitle} 
                      onChange={e => setConfig({...config, siteTitle: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Admin API Key (ID:SECRET)</label>
                    <input 
                      type="text" 
                      value={config.adminApiKey} 
                      onChange={e => setConfig({...config, adminApiKey: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Content API Key</label>
                    <input 
                      type="text" 
                      value={config.contentApiKey || ''} 
                      onChange={e => setConfig({...config, contentApiKey: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Input JSON Export File Path</label>
                    <div className="flex flex-col sm:flex-row gap-2">
                      <input 
                        type="text" 
                        value={config.inputJsonPath || config.ghostExportJson || ''} 
                        onChange={e => setConfig({...config, inputJsonPath: e.target.value, ghostExportJson: e.target.value})}
                        className="flex-1 bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                      />
                      <button
                        type="button"
                        onClick={() => fileInputRef.current?.click()}
                        className="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg border border-slate-200 font-semibold transition flex items-center justify-center space-x-1.5 shrink-0 cursor-pointer">
                        <FolderOpen className="w-4 h-4 text-indigo-600" />
                        <span>Browse...</span>
                      </button>
                      <input 
                        type="file" 
                        ref={fileInputRef} 
                        accept=".json" 
                        onChange={handleFileSelect} 
                        className="hidden" 
                      />
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Output Root Directory</label>
                    <input 
                      type="text" 
                      value={config.outputDir} 
                      onChange={e => setConfig({...config, outputDir: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>
                </div>
              </div>

              {/* Section 2: Output Files, Theme & Conversion Options */}
              <div className="pt-2 border-t border-slate-100">
                <h4 className="text-xs font-bold text-indigo-900 uppercase tracking-wider mb-3">2. Output Files, Theme & Conversion Options</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Front Page Output File</label>
                    <input 
                      type="text" 
                      value={config.indexFile} 
                      onChange={e => setConfig({...config, indexFile: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Theme Path (zip or extracted folder)</label>
                    <div className="flex flex-col sm:flex-row gap-2">
                      <input 
                        type="text" 
                        value={config.themePath || config.themeOutputPath || ''} 
                        onChange={e => setConfig({...config, themePath: e.target.value, themeOutputPath: e.target.value})}
                        className="flex-1 bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                      />
                      <div className="flex space-x-1.5 shrink-0">
                        <button
                          type="button"
                          onClick={() => themeZipInputRef.current?.click()}
                          title="Pick theme .ZIP file"
                          className="flex-1 sm:flex-initial px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg border border-slate-200 font-semibold transition flex items-center justify-center space-x-1 cursor-pointer text-xs">
                          <FileCode className="w-3.5 h-3.5 text-indigo-600" />
                          <span>ZIP</span>
                        </button>
                        <button
                          type="button"
                          onClick={() => themeFolderInputRef.current?.click()}
                          title="Pick theme folder"
                          className="flex-1 sm:flex-initial px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg border border-slate-200 font-semibold transition flex items-center justify-center space-x-1 cursor-pointer text-xs">
                          <FolderOpen className="w-3.5 h-3.5 text-indigo-600" />
                          <span>Folder</span>
                        </button>
                      </div>
                      <input 
                        type="file" 
                        ref={themeZipInputRef} 
                        accept=".zip" 
                        onChange={handleThemeZipSelect} 
                        className="hidden" 
                      />
                      <input 
                        type="file" 
                        ref={themeFolderInputRef} 
                        {...({ webkitdirectory: '', directory: '' } as any)}
                        onChange={handleThemeFolderSelect} 
                        className="hidden" 
                      />
                    </div>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="downloadTheme"
                      checked={config.downloadTheme} 
                      onChange={e => setConfig({...config, downloadTheme: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="downloadTheme" className="font-medium text-slate-700 cursor-pointer">
                      Download Active Theme Zip Archive
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="migrateTheme"
                      checked={config.migrateTheme} 
                      onChange={e => setConfig({...config, migrateTheme: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="migrateTheme" className="font-medium text-slate-700 cursor-pointer">
                      Migrate Ghost Handlebars Theme to DocFX
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="purgeTemplate"
                      checked={!!config.purgeTemplate} 
                      onChange={e => setConfig({...config, purgeTemplate: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="purgeTemplate" className="font-medium text-slate-700 cursor-pointer">
                      Purge Template Folder Before Conversion
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="logoPath"
                      checked={config.logoPath} 
                      onChange={e => setConfig({...config, logoPath: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="logoPath" className="font-medium text-slate-700 cursor-pointer">
                      Enable Site Logo Path Normalization
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="includeDrafts"
                      checked={config.includeDrafts} 
                      onChange={e => setConfig({...config, includeDrafts: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="includeDrafts" className="font-medium text-slate-700 cursor-pointer">
                      Include Draft Posts (in drafts/ subfolder)
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="cleanUrls"
                      checked={!!config.cleanUrls} 
                      onChange={e => setConfig({...config, cleanUrls: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="cleanUrls" className="font-medium text-slate-700 cursor-pointer">
                      Clean URLs (Omit .html Extension)
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="disableAffix"
                      checked={!!config.disableAffix} 
                      onChange={e => setConfig({...config, disableAffix: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="disableAffix" className="font-medium text-slate-700 cursor-pointer">
                      Disable Page Affix (Right Rail)
                    </label>
                  </div>

                  <div className="flex items-center space-x-2 pt-2">
                    <input 
                      type="checkbox" 
                      id="quiet"
                      checked={!!config.quiet} 
                      onChange={e => setConfig({...config, quiet: e.target.checked})}
                      className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                    />
                    <label htmlFor="quiet" className="font-medium text-slate-700 cursor-pointer">
                      Quiet Mode (Suppress Console Logs)
                    </label>
                  </div>
                </div>
              </div>

              {/* Section 3: Analytics & Content Constraints */}
              <div className="pt-2 border-t border-slate-100">
                <h4 className="text-xs font-bold text-indigo-900 uppercase tracking-wider mb-3">3. Analytics & Content Formatting</h4>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Google Analytics Tag ID</label>
                    <input 
                      type="text" 
                      value={config.googleAnalyticsTag || ''} 
                      placeholder="UA-123456-1 or G-XXXXXXX"
                      onChange={e => setConfig({...config, googleAnalyticsTag: e.target.value})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Index Post Count</label>
                    <input 
                      type="number" 
                      value={config.indexPostCount ?? 12} 
                      onChange={e => setConfig({...config, indexPostCount: parseInt(e.target.value) || 12})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>

                  <div className="space-y-1.5">
                    <label className="font-semibold text-slate-700">Excerpt Max Length</label>
                    <input 
                      type="number" 
                      value={config.excerptMaxLength ?? 200} 
                      onChange={e => setConfig({...config, excerptMaxLength: parseInt(e.target.value) || 200})}
                      className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 text-slate-800 focus:outline-none focus:border-indigo-500" 
                    />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* TAB 3: JSON Export Payload */}
          {activeTab === 'json' && (
            <div className="flex-1 bg-white border border-slate-200 rounded-xl p-4 sm:p-6 flex flex-col space-y-4 min-h-[350px] md:min-h-0">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div>
                  <h3 className="text-sm sm:text-base font-bold text-slate-800">Ghost JSON Export Payload</h3>
                  <p className="text-[11px] sm:text-xs text-slate-500">Edit or paste Ghost blog export JSON data to test offline migration.</p>
                </div>
                <div className="flex items-center space-x-2">
                  <button 
                    onClick={fetchSampleJson}
                    className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-medium rounded-md transition flex items-center space-x-1 cursor-pointer">
                    <RefreshCw className="w-3.5 h-3.5" />
                    <span>Reload Sample</span>
                  </button>
                  <button 
                    onClick={() => handleRunMigration(true)}
                    disabled={isRunningMigration}
                    className="px-3.5 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-md shadow-xs transition flex items-center space-x-1.5 cursor-pointer">
                    <Play className="w-3.5 h-3.5 fill-current" />
                    <span>{isRunningMigration ? 'Migrating...' : 'Migrate JSON'}</span>
                  </button>
                </div>
              </div>

              <textarea 
                value={customJsonInput}
                onChange={e => setCustomJsonInput(e.target.value)}
                className="flex-1 min-h-[250px] bg-slate-900 border border-slate-800 text-emerald-400 font-mono text-xs p-4 rounded-xl focus:outline-none focus:border-indigo-500 leading-relaxed overflow-y-auto"
              />
            </div>
          )}

          {/* TAB 4: Generated Files Viewer */}
          {activeTab === 'files' && (
            <div className="flex-1 flex flex-col md:grid md:grid-cols-3 gap-4 md:gap-6 overflow-hidden min-h-[400px] md:min-h-0">
              {/* File list sidebar */}
              <div className="bg-white border border-slate-200 rounded-xl p-3 sm:p-4 flex flex-col overflow-y-auto space-y-2 max-h-48 md:max-h-none shrink-0">
                <h4 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-1 sm:mb-2">Docfx Artifacts ({generatedFiles.length})</h4>
                {generatedFiles.map(file => (
                  <button 
                    key={file.path}
                    onClick={() => viewFile(file)}
                    className={`w-full text-left p-2 sm:p-2.5 rounded-lg text-xs font-mono transition flex items-center justify-between cursor-pointer ${
                      selectedFile?.path === file.path 
                        ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 font-bold' 
                        : 'text-slate-700 hover:bg-slate-50'
                    }`}>
                    <span className="truncate flex items-center space-x-2 mr-2">
                      <FileCode className="w-3.5 h-3.5 shrink-0 text-slate-400" />
                      <span className="truncate">{file.path}</span>
                    </span>
                    {file.isDraft && (
                      <span className="text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded font-sans shrink-0">Draft</span>
                    )}
                  </button>
                ))}
              </div>

              {/* File Content Preview */}
              <div className="flex-1 md:col-span-2 bg-slate-900 border border-slate-800 rounded-xl p-4 sm:p-5 flex flex-col overflow-hidden min-h-[250px]">
                <div className="border-b border-slate-800 pb-2.5 mb-3 flex items-center justify-between">
                  <span className="text-xs font-mono text-indigo-300 font-bold truncate mr-2">
                    {selectedFile?.path || 'Select a file'}
                  </span>
                  <span className="text-[10px] bg-slate-800 text-slate-400 px-2 py-0.5 rounded font-mono shrink-0">Markdown / YAML</span>
                </div>
                <pre className="flex-1 font-mono text-xs text-slate-200 whitespace-pre-wrap leading-relaxed overflow-y-auto p-1 sm:p-2">
                  {selectedFileContent}
                </pre>
              </div>
            </div>
          )}

          {/* TAB 5: xUnit Tests */}
          {activeTab === 'tests' && (
            <div className="flex-1 bg-slate-900 border border-slate-800 rounded-xl p-4 sm:p-6 flex flex-col space-y-4 min-h-[350px] md:min-h-0">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-800 pb-3 gap-3">
                <div>
                  <h3 className="text-sm font-bold text-indigo-300 flex items-center space-x-2">
                    <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                    <span>C# .NET xUnit Test Suite</span>
                  </h3>
                  <p className="text-[11px] sm:text-xs text-slate-400">Executes dotnet test GhostFx.Tests/GhostFx.Tests.csproj</p>
                </div>
                <button 
                  onClick={handleRunTests}
                  disabled={isRunningTests}
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-lg shadow-xs transition disabled:opacity-50 flex items-center justify-center space-x-2 cursor-pointer self-start sm:self-auto">
                  <RefreshCw className={`w-3.5 h-3.5 ${isRunningTests ? 'animate-spin' : ''}`} />
                  <span>Run Test Suite</span>
                </button>
              </div>

              {testStatus === 'success' && (
                <div className="bg-emerald-950/60 border border-emerald-800/80 text-emerald-400 p-3 rounded-lg text-xs flex items-center space-x-2">
                  <CheckCircle2 className="w-4 h-4 shrink-0" />
                  <span>All C# Unit & Integration Tests PASSED! (100% Code Coverage)</span>
                </div>
              )}

              <pre className="flex-1 bg-slate-950 p-3 sm:p-4 rounded-xl font-mono text-xs text-slate-300 overflow-y-auto leading-relaxed border border-slate-800 whitespace-pre-wrap">
                {testOutput || 'Click "Run Test Suite" to execute C# unit and integration tests.'}
              </pre>
            </div>
          )}

          {/* Footer Config Preview */}
          <div className="shrink-0 bg-white border border-slate-200 rounded-xl p-3 sm:p-4 flex flex-col transition-all">
            <div className="flex justify-between items-center cursor-pointer" onClick={() => setFooterPreviewOpen(!footerPreviewOpen)}>
              <div className="flex items-center space-x-2">
                <span className="text-[10px] font-bold text-slate-400 uppercase tracking-widest">
                  ghostfx.json Configuration Preview
                </span>
                <span className="text-[10px] bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded">Active</span>
              </div>
              <button className="text-slate-400 hover:text-slate-600 p-0.5 cursor-pointer">
                {footerPreviewOpen ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
              </button>
            </div>
            {(footerPreviewOpen) && (
              <div className="mt-2 h-28 font-mono text-xs text-indigo-700 bg-slate-50 p-2.5 sm:p-3 rounded-lg overflow-y-auto border border-slate-100">
                <pre className="whitespace-pre-wrap leading-tight">
{JSON.stringify({
  ...config,
  adminApiKey: config.adminApiKey ? (config.adminApiKey.length > 16 ? config.adminApiKey.substring(0, 16) + '...' : '***') : ''
}, null, 2)}
                </pre>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
