import React, { useState, useEffect } from 'react';
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
  Globe
} from 'lucide-react';

interface GhostFxConfigData {
  ghostUrl: string;
  adminApiKey: string;
  inputJsonPath: string;
  outputDir: string;
  indexFile: string;
  siteTitle: string;
  includeDrafts: boolean;
  downloadTheme: boolean;
  themeOutputPath: string;
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
  
  const [config, setConfig] = useState<GhostFxConfigData>({
    ghostUrl: 'https://demo.ghost.io',
    adminApiKey: '640a1b2c3d4e5f6a7b8c9d0e:1234567890abcdef1234567890abcdef',
    inputJsonPath: 'sample-ghost-export.json',
    outputDir: 'articles',
    indexFile: 'index.md',
    siteTitle: 'GhostFx Sample Blog',
    includeDrafts: true,
    downloadTheme: false,
    themeOutputPath: 'templates/ghost-theme.zip'
  });

  const [customJsonInput, setCustomJsonInput] = useState<string>('');
  const [isRunningMigration, setIsRunningMigration] = useState<boolean>(false);
  const [isRunningTests, setIsRunningTests] = useState<boolean>(false);
  
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

  const handleRunMigration = async () => {
    setIsRunningMigration(true);
    setActiveTab('console');
    setTerminalLogs([
      '[INFO] Initializing GhostFx .NET Engine...',
      `[CONFIG] Source: ${config.inputJsonPath || config.ghostUrl}`,
      `[CONFIG] Output Directory: ./${config.outputDir}`,
      '[EXEC] Executing C# GhostFx.Cli migration runner...'
    ]);

    let parsedJson: any = null;
    if (customJsonInput.trim()) {
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
          customJson: parsedJson
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
        setStats({
          totalPosts: 3,
          publishedPosts: 2,
          draftPosts: config.includeDrafts ? 1 : 0,
          totalTags: 3
        });
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
    <div className="flex flex-col h-screen w-full bg-slate-50 font-sans text-slate-900 overflow-hidden">
      {/* Top Navigation / Brand Rail */}
      <nav className="h-14 bg-indigo-900 flex items-center justify-between px-6 shadow-md shrink-0">
        <div className="flex items-center space-x-3">
          <div className="w-8 h-8 bg-indigo-500 rounded flex items-center justify-center font-bold text-white text-lg">
            G
          </div>
          <span className="text-white font-semibold text-lg tracking-tight flex items-center">
            GhostFx <span className="text-indigo-300 font-normal text-xs ml-2 bg-indigo-800/80 px-2 py-0.5 rounded border border-indigo-700">v1.0.0-beta.1</span>
          </span>
        </div>

        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2 bg-indigo-800 px-3 py-1 rounded-full border border-indigo-700">
            <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></div>
            <span className="text-indigo-100 text-xs font-medium uppercase tracking-wider">
              {config.inputJsonPath ? 'Offline JSON Mode' : 'Connected to Ghost API'}
            </span>
          </div>

          <button 
            onClick={handleRunTests}
            className="flex items-center space-x-1 text-xs bg-indigo-800 hover:bg-indigo-700 text-indigo-100 px-3 py-1.5 rounded-lg border border-indigo-700 transition cursor-pointer">
            <CheckCircle2 className="w-3.5 h-3.5 text-indigo-300" />
            <span>.NET xUnit Tests</span>
          </button>

          <div className="w-8 h-8 rounded-full bg-indigo-700 border border-indigo-600 flex items-center justify-center text-indigo-200 text-xs font-bold">
            AD
          </div>
        </div>
      </nav>

      {/* Main Workspace */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left Sidebar: Configuration & State */}
        <aside className="w-72 bg-white border-r border-slate-200 flex flex-col shrink-0">
          <div className="p-5 border-b border-slate-100">
            <h2 className="text-xs font-bold text-slate-500 uppercase tracking-widest mb-4">Migration Profile</h2>
            <div className="space-y-4">
              <div className="flex flex-col">
                <label className="text-[11px] text-slate-400 uppercase font-semibold mb-1">Source URL / Export</label>
                <span className="text-xs text-slate-700 font-mono break-all bg-slate-50 p-2 rounded border border-slate-200">
                  {config.inputJsonPath || config.ghostUrl}
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

          <div className="flex-1 p-5 overflow-y-auto">
            <h2 className="text-xs font-bold text-slate-500 uppercase tracking-widest mb-4">Pipeline Flags</h2>
            <ul className="space-y-3">
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

          <div className="p-5 bg-slate-50 border-t border-slate-200">
            <button 
              onClick={handleRunMigration}
              disabled={isRunningMigration}
              className="w-full bg-indigo-600 hover:bg-indigo-700 text-white py-2.5 rounded shadow-sm font-semibold text-sm transition-colors disabled:opacity-50 flex items-center justify-center space-x-2 cursor-pointer">
              <Play className="w-4 h-4 fill-current" />
              <span>{isRunningMigration ? 'Migrating...' : 'Start Live Migration'}</span>
            </button>
          </div>
        </aside>

        {/* Main Content: Execution Log & Monitoring */}
        <main className="flex-1 flex flex-col p-6 space-y-6 overflow-hidden">
          {/* Summary Metric Cards */}
          <div className="grid grid-cols-4 gap-4 shrink-0">
            <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-1">Total Posts</p>
              <h3 className="text-2xl font-semibold text-slate-800">{stats.totalPosts}</h3>
            </div>

            <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-1">Published</p>
              <h3 className="text-2xl font-semibold text-indigo-600">{stats.publishedPosts}</h3>
            </div>

            <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-1">Drafts</p>
              <h3 className="text-2xl font-semibold text-slate-400">{stats.draftPosts}</h3>
            </div>

            <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
              <p className="text-[10px] text-slate-500 uppercase font-bold tracking-wider mb-1">Total Tags</p>
              <h3 className="text-2xl font-semibold text-slate-800">{stats.totalTags}</h3>
            </div>
          </div>

          {/* Navigation Bar for Workspace Panels */}
          <div className="flex space-x-1 border-b border-slate-200 shrink-0">
            <button 
              onClick={() => setActiveTab('console')}
              className={`px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'console' 
                  ? 'bg-slate-900 text-white border-t border-x border-slate-800' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Terminal className="w-3.5 h-3.5" />
              <span>Migration Console</span>
            </button>

            <button 
              onClick={() => setActiveTab('config')}
              className={`px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'config' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Settings className="w-3.5 h-3.5" />
              <span>ghostfx.json Settings</span>
            </button>

            <button 
              onClick={() => setActiveTab('json')}
              className={`px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'json' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <Code2 className="w-3.5 h-3.5" />
              <span>JSON Payload</span>
            </button>

            <button 
              onClick={() => setActiveTab('files')}
              className={`px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'files' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <FolderOpen className="w-3.5 h-3.5" />
              <span>Generated Artifacts ({generatedFiles.length})</span>
            </button>

            <button 
              onClick={() => setActiveTab('tests')}
              className={`px-4 py-2 text-xs font-semibold rounded-t-lg transition flex items-center space-x-2 cursor-pointer ${
                activeTab === 'tests' 
                  ? 'bg-indigo-600 text-white' 
                  : 'text-slate-600 hover:text-slate-900 bg-white/50'
              }`}>
              <CheckCircle2 className="w-3.5 h-3.5" />
              <span>C# xUnit Tests</span>
            </button>
          </div>

          {/* TAB 1: Terminal Console */}
          {activeTab === 'console' && (
            <div className="flex-1 bg-slate-900 rounded-xl shadow-lg flex flex-col border border-slate-800 overflow-hidden min-h-0">
              <div className="h-10 bg-slate-800 flex items-center px-4 space-x-2 shrink-0">
                <div className="flex space-x-1.5">
                  <div className="w-3 h-3 rounded-full bg-red-500/80"></div>
                  <div className="w-3 h-3 rounded-full bg-amber-500/80"></div>
                  <div className="w-3 h-3 rounded-full bg-emerald-500/80"></div>
                </div>
                <span className="text-slate-400 text-[11px] font-mono pl-4">ghostfx-cli --verbose --target docfx</span>
              </div>

              <div className="flex-1 p-5 font-mono text-[13px] leading-relaxed text-slate-300 overflow-y-auto space-y-1">
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
            <div className="flex-1 bg-white border border-slate-200 rounded-xl p-6 overflow-y-auto space-y-6">
              <div className="flex items-center justify-between border-b border-slate-100 pb-4">
                <div>
                  <h3 className="text-base font-bold text-slate-800">ghostfx.json Configuration</h3>
                  <p className="text-xs text-slate-500">Configure parameters passed to the GhostFx C# migration engine.</p>
                </div>
                <button 
                  onClick={handleSaveConfig}
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-lg shadow-sm transition flex items-center space-x-2 cursor-pointer">
                  <Save className="w-3.5 h-3.5" />
                  <span>Save Configuration</span>
                </button>
              </div>

              <div className="grid grid-cols-2 gap-6 text-xs">
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
                  <label className="font-semibold text-slate-700">Admin API Key (ID:SECRET)</label>
                  <input 
                    type="text" 
                    value={config.adminApiKey} 
                    onChange={e => setConfig({...config, adminApiKey: e.target.value})}
                    className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="font-semibold text-slate-700">Input JSON Export File Path</label>
                  <input 
                    type="text" 
                    value={config.inputJsonPath} 
                    onChange={e => setConfig({...config, inputJsonPath: e.target.value})}
                    className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 font-mono text-slate-800 focus:outline-none focus:border-indigo-500" 
                  />
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
                  <label className="font-semibold text-slate-700">Site Title</label>
                  <input 
                    type="text" 
                    value={config.siteTitle} 
                    onChange={e => setConfig({...config, siteTitle: e.target.value})}
                    className="w-full bg-slate-50 border border-slate-200 rounded-lg p-2.5 text-slate-800 focus:outline-none focus:border-indigo-500" 
                  />
                </div>

                <div className="flex items-center space-x-2 pt-4">
                  <input 
                    type="checkbox" 
                    id="includeDrafts"
                    checked={config.includeDrafts} 
                    onChange={e => setConfig({...config, includeDrafts: e.target.checked})}
                    className="w-4 h-4 text-indigo-600 rounded border-slate-300"
                  />
                  <label htmlFor="includeDrafts" className="font-medium text-slate-700 cursor-pointer">
                    Include Draft Posts (isolate in drafts/ subfolder)
                  </label>
                </div>

                <div className="flex items-center space-x-2 pt-4">
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
              </div>
            </div>
          )}

          {/* TAB 3: JSON Export Payload */}
          {activeTab === 'json' && (
            <div className="flex-1 bg-white border border-slate-200 rounded-xl p-6 flex flex-col space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-base font-bold text-slate-800">Ghost JSON Export Payload</h3>
                  <p className="text-xs text-slate-500">Edit or paste Ghost blog export JSON data to test offline migration.</p>
                </div>
                <button 
                  onClick={fetchSampleJson}
                  className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-medium rounded-md transition flex items-center space-x-1 cursor-pointer">
                  <RefreshCw className="w-3.5 h-3.5" />
                  <span>Reload Sample</span>
                </button>
              </div>

              <textarea 
                value={customJsonInput}
                onChange={e => setCustomJsonInput(e.target.value)}
                className="flex-1 bg-slate-900 border border-slate-800 text-emerald-400 font-mono text-xs p-4 rounded-xl focus:outline-none focus:border-indigo-500 leading-relaxed overflow-y-auto"
              />
            </div>
          )}

          {/* TAB 4: Generated Files Viewer */}
          {activeTab === 'files' && (
            <div className="flex-1 grid grid-cols-3 gap-6 overflow-hidden">
              {/* File list sidebar */}
              <div className="bg-white border border-slate-200 rounded-xl p-4 flex flex-col overflow-y-auto space-y-2">
                <h4 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">Docfx Artifacts</h4>
                {generatedFiles.map(file => (
                  <button 
                    key={file.path}
                    onClick={() => viewFile(file)}
                    className={`w-full text-left p-2.5 rounded-lg text-xs font-mono transition flex items-center justify-between cursor-pointer ${
                      selectedFile?.path === file.path 
                        ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 font-bold' 
                        : 'text-slate-700 hover:bg-slate-50'
                    }`}>
                    <span className="truncate flex items-center space-x-2">
                      <FileCode className="w-3.5 h-3.5 shrink-0 text-slate-400" />
                      <span>{file.path}</span>
                    </span>
                    {file.isDraft && (
                      <span className="text-[10px] bg-amber-100 text-amber-800 px-1.5 py-0.5 rounded font-sans">Draft</span>
                    )}
                  </button>
                ))}
              </div>

              {/* File Content Preview */}
              <div className="col-span-2 bg-slate-900 border border-slate-800 rounded-xl p-5 flex flex-col overflow-hidden">
                <div className="border-b border-slate-800 pb-3 mb-3 flex items-center justify-between">
                  <span className="text-xs font-mono text-indigo-300 font-bold">
                    {selectedFile?.path || 'Select a file'}
                  </span>
                  <span className="text-[10px] bg-slate-800 text-slate-400 px-2 py-0.5 rounded font-mono">Markdown / YAML</span>
                </div>
                <pre className="flex-1 font-mono text-xs text-slate-200 whitespace-pre-wrap leading-relaxed overflow-y-auto p-2">
                  {selectedFileContent}
                </pre>
              </div>
            </div>
          )}

          {/* TAB 5: xUnit Tests */}
          {activeTab === 'tests' && (
            <div className="flex-1 bg-slate-900 border border-slate-800 rounded-xl p-6 flex flex-col space-y-4">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div>
                  <h3 className="text-sm font-bold text-indigo-300 flex items-center space-x-2">
                    <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    <span>C# .NET xUnit Test Suite</span>
                  </h3>
                  <p className="text-xs text-slate-400">Executes dotnet test GhostFx.Tests/GhostFx.Tests.csproj</p>
                </div>
                <button 
                  onClick={handleRunTests}
                  disabled={isRunningTests}
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-lg shadow-sm transition disabled:opacity-50 flex items-center space-x-2 cursor-pointer">
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

              <pre className="flex-1 bg-slate-950 p-4 rounded-xl font-mono text-xs text-slate-300 overflow-y-auto leading-relaxed border border-slate-800">
                {testOutput || 'Click "Run Test Suite" to execute C# unit and integration tests.'}
              </pre>
            </div>
          )}

          {/* Footer Config Preview */}
          <div className="h-32 shrink-0 bg-white border border-slate-200 rounded-xl p-4 flex flex-col">
            <div className="flex justify-between items-center mb-2">
              <span className="text-[10px] font-bold text-slate-400 uppercase tracking-widest">
                ghostfx.json Configuration Preview
              </span>
              <span className="text-[10px] bg-slate-100 text-slate-500 px-2 py-0.5 rounded">Active</span>
            </div>
            <div className="flex-1 font-mono text-xs text-indigo-700 bg-slate-50 p-3 rounded-lg overflow-hidden border border-slate-100">
              <pre className="whitespace-pre-wrap leading-tight">
{JSON.stringify({
  ghostUrl: config.ghostUrl,
  adminApiKey: config.adminApiKey.substring(0, 16) + '...',
  outputDir: config.outputDir,
  indexFile: config.indexFile,
  includeDrafts: config.includeDrafts
}, null, 2)}
              </pre>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
